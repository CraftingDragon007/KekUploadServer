using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Html;
using SharpHash.Base;
using Microsoft.AspNetCore.Mvc;

namespace KekUploadServer.Controllers;

public class UploadController : Controller
{

    [HttpPost]
    [Route("u/{stream}/{hash}")]
    public IActionResult Upload(string stream, string hash)
    {
        var item = Data.UploadStreams.GetByStreamId(stream);
        if (item == null)
        {
            return new JsonResult(new
            {
                generic = "NOT_FOUND",
                field = "STREAM",
                error = "Stream not found"
            }) {ContentType = "application/json", SerializerSettings = null, StatusCode = 404};
        }

        var uploadStream = new MemoryStream();
        HttpContext.Request.Body.CopyToAsync(uploadStream).Wait();
        var data = uploadStream.ToArray();
        if (data.Length > Data.MaxChunkSize)
        {
            return new JsonResult(new {generic = "OVERFLOW", field = "CHUNK", error = "Chunk size exceeded"})
                {ContentType = "application/json", SerializerSettings = null, StatusCode = 400};
        }
        var chunkHash = HashFactory.Crypto.CreateSHA1().ComputeBytes(data).ToString().ToLower().Replace("-", "");
        if (chunkHash != hash)
        {
            return new JsonResult(new {generic = "HASH_MISMATCH", field = "HASH", error = "Hash doesn't match"})
                {ContentType = "application/json", SerializerSettings = null, StatusCode = 400};
        }
        //uploadStream.CopyTo(item.FileStream);
        item.FileStream.Write(data, 0, data.Length);
        item.Hash.TransformBytes(data);
        return new JsonResult(new { success = true }) {ContentType = "application/json", SerializerSettings = null, StatusCode = 200};
    }

    [HttpPost]
    [Route("c/{ext}")]
    public IActionResult Create(string ext)
    {
        var streamId = Data.RandomString(Data.EnsureNotNullConfig().IdSize);
        FileStream fileStream;
        try
        {
            fileStream = System.IO.File.Create(Data.EnsureNotNullConfig().UploadFolder + streamId + ".upload");
        }catch(Exception e)
        {
            return new JsonResult(new { generic = "FS_CREATE", field = "FILE", error = "Error while creating file: " + e.Message })
            {
                ContentType = "application/json",
                SerializerSettings = null,
                StatusCode = 500
            };
        }
        Data.UploadStreams.Add(new UploadItem(streamId, ext, fileStream));
        Console.WriteLine("Created upload stream with Id: " + streamId);
        return new JsonResult(new { stream = streamId });
    }
    
    [HttpPost]
    [Route("c/{ext}/{name}")]
    public IActionResult Create(string ext, string name)
    {
        var streamId = Data.RandomString(Data.EnsureNotNullConfig().IdSize);
        FileStream fileStream;
        try
        {
            fileStream = System.IO.File.Create(Data.EnsureNotNullConfig().UploadFolder + streamId + ".upload");
        }catch(Exception e)
        {
            return new JsonResult(new { generic = "FS_CREATE", field = "FILE", error = "Error while creating file: " + e.Message })
            {
                ContentType = "application/json",
                SerializerSettings = null,
                StatusCode = 500
            };
        }
        Data.UploadStreams.Add(new UploadItem(streamId, ext, name, fileStream));
        return new JsonResult(new { stream = streamId });
    }

    [HttpPost]
    [Route("r/{stream}")]
    public IActionResult Remove(string stream)
    {
        try
        {
            System.IO.File.Delete(Data.EnsureNotNullConfig().UploadFolder + stream + ".upload");
        }
        catch (IOException e)
        {
            return new JsonResult(new { generic = "FS_REMOVE", field = "FILE", error = "Error while deleting file: " + e.Message })
            {
                ContentType = "application/json",
                SerializerSettings = null,
                StatusCode = 500
            };
        }
        return new JsonResult(new { success = true });
    }
    
    [HttpPost]
    [Route("f/{stream}/{hash}")]
    public IActionResult Finish(string stream, string hash)
    {
        var item = Data.UploadStreams.GetByStreamId(stream);
        if (item == null)
        {
            return new JsonResult(new { generic = "NOT_FOUND", field = "STREAM", error = "Stream not found" })
            {
                ContentType = "application/json",
                SerializerSettings = null,
                StatusCode = 404
            };
        }

        var fileHash = item.Hash.TransformFinal().ToString().ToLower();
        if (fileHash != hash)
        {
            return new JsonResult(new { generic = "HASH_MISMATCH", field = "HASH", error = "Hash doesn't match" })
            {
                ContentType = "application/json",
                SerializerSettings = null,
                StatusCode = 400
            };
        }
        item.FileStream.Close();
        Data.UploadStreams.Remove(item);
        var id = Data.RandomString(Data.EnsureNotNullConfig().IdSize);
        Data.InsertUploadedItemIntoDatabase(item, id);
        return new JsonResult(new { id }) {ContentType = "application/json", SerializerSettings = null, StatusCode = 200};
    }

    [HttpGet]
    [Route("d/{id}")]
    public IActionResult Download(string id)
    {
        var notFound = new JsonResult(new { generic = "NOT_FOUND", field = "ID", error = "File with id not found" })
        {
            ContentType = "application/json",
            SerializerSettings = null,
            StatusCode = 404
        };
        var streamId = Data.GetStreamIdByUploadId(id);
        if (streamId == null)
        {
            return notFound;
        }
        var extension = Data.GetExtensionFromId(id);
        var name = Data.GetNameFromId(id);
        extension ??= "";
        if (name == null)
        {
            var hash = Data.GetHashFromId(id);
            name = hash ?? "HASH_NOT_FOUND";
        }

        if (System.IO.File.Exists(Data.EnsureNotNullConfig().UploadFolder + streamId + ".upload"))
        {
            return File(new FileStream(Data.EnsureNotNullConfig().UploadFolder + streamId + ".upload", FileMode.Open), "application/octet-stream", name + (extension.Equals("none") ? "" : "." + extension));
        }
        return notFound;
    }

    [HttpGet]
    [Route("vs/{id}")]
    public Task StreamVideo(string id)
    {
        var notFound = new JsonResult(new { generic = "NOT_FOUND", field = "ID", error = "File with id not found" })
        {
            ContentType = "application/json",
            SerializerSettings = null,
            StatusCode = 404
        };
        var streamId = Data.GetStreamIdByUploadId(id);
        if (streamId == null)
        {
            return notFound.ExecuteResultAsync(ControllerContext);
        }
        var extension = Data.GetExtensionFromId(id);
        var name = Data.GetNameFromId(id);
        extension ??= "";
        if (name == null)
        {
            var hash = Data.GetHashFromId(id);
            name = hash ?? "HASH_NOT_FOUND";
        }

        if (!System.IO.File.Exists(Data.EnsureNotNullConfig().UploadFolder + streamId + ".upload"))
            return notFound.ExecuteResultAsync(ControllerContext);
        
        HttpContext.Response.ContentType = "video/" + extension;
        var buffer = new byte[4096];
        var stream = new FileStream(Data.EnsureNotNullConfig().UploadFolder + streamId + ".upload", FileMode.Open);
        /*return new Task(() =>
        {
            while (true)
            {
                var read = stream.Read(buffer, 0, buffer.Length);
                if (read == 0)
                    break;
                HttpContext.Response.Body.Write(buffer, 0, read);
            }
        });*/
        //return new FileStreamResult(new FileStream("uploads/" + streamId + ".upload", FileMode.Open), "video/" + extension).ExecuteResultAsync(ControllerContext);
        //var video = new VideoStream(name);
        
        HttpContext.Response.Headers.Add("Content-Disposition", "attachment; filename=" + name + (extension.Equals("none") ? "" : "." + extension));
        return new Task(() =>
        {
            while (true)
            {
                var read = stream.Read(buffer, 0, buffer.Length);
                if (read == 0)
                    break;
                HttpContext.Response.Body.Write(buffer, 0, read);
            }
        });
        //return new PushStreamContent(video.WriteToStream, new MediaTypeHeaderValue("video/" + extension)).CopyToAsync(HttpContext.Response.Body);
    }

    [HttpGet]
    [Route("l/{id}")]
    public IActionResult Lenght(string id)
    {
        var notFound = new JsonResult(new { generic = "NOT_FOUND", field = "ID", error = "File with id not found" })
        {
            ContentType = "application/json",
            SerializerSettings = null,
            StatusCode = 404
        };
        var streamId = Data.GetStreamIdByUploadId(id);
        if (streamId == null)
        {
            return notFound;
        }
        var extension = Data.GetExtensionFromId(id);
        var name = Data.GetNameFromId(id);
        extension ??= "";
        if (name == null)
        {
            var hash = Data.GetHashFromId(id);
            name = hash ?? "HASH_NOT_FOUND";
        }

        if (!System.IO.File.Exists(Data.EnsureNotNullConfig().UploadFolder + streamId + ".upload")) return notFound;
        
        return base.Content(new FileInfo(Data.EnsureNotNullConfig().UploadFolder + streamId + ".upload").Length.ToString());
    }

    [HttpGet]
    [Route("d/{id}/{startByte}/{lenght}")]
    public IActionResult DownloadRange(string id, long startByte, int lenght)
    {
        var notFound = new JsonResult(new { generic = "NOT_FOUND", field = "ID", error = "File with id not found" })
        {
            ContentType = "application/json",
            SerializerSettings = null,
            StatusCode = 404
        };
        var streamId = Data.GetStreamIdByUploadId(id);
        if (streamId == null)
        {
            return notFound;
        }
        var extension = Data.GetExtensionFromId(id);
        var name = Data.GetNameFromId(id);
        extension ??= "";
        if (name == null)
        {
            var hash = Data.GetHashFromId(id);
            name = hash ?? "HASH_NOT_FOUND";
        }

        if (!System.IO.File.Exists(Data.EnsureNotNullConfig().UploadFolder + streamId + ".upload")) return notFound;
        
        var stream = new FileStream(Data.EnsureNotNullConfig().UploadFolder + streamId + ".upload", FileMode.Open);
        var range = new byte[lenght];
        try
        {
            stream.Seek(startByte, SeekOrigin.Begin);
            var read = stream.Read(range, 0, lenght);
            stream.Close();
        }catch(Exception)
        {
            return notFound;
        }
        return File(range, "application/octet-stream", name + (extension.Equals("none") ? "" : "." + extension));
    }
    

    [HttpGet]
    [Route("v/{id}")]
    public IActionResult Video(string id)
    {
        var notFound = new JsonResult(new { generic = "NOT_FOUND", field = "ID", error = "File with id not found" })
        {
            ContentType = "application/json",
            SerializerSettings = null,
            StatusCode = 404
        };
        var streamId = Data.GetStreamIdByUploadId(id);
        if (streamId == null)
        {
            return notFound;
        }
        var extension = Data.GetExtensionFromId(id);
        var name = Data.GetNameFromId(id);
        extension ??= "";
        if (name == null)
        {
            var hash = Data.GetHashFromId(id);
            name = hash ?? "HASH_NOT_FOUND";
        }
        var path = Data.EnsureNotNullConfig().UploadFolder + streamId + ".upload";
        if (!System.IO.File.Exists(path)) return notFound;
        var videoExtensions = new List<string> { ".MP4", ".MOV", ".M4V", ".AVI", ".WMV", ".MPG", ".MPEG", ".OGG", ".WEBM", ".MKV" };
        if (!videoExtensions.Contains("." + extension.ToUpper())) return new JsonResult(new { generic = "NOT_VIDEO", field = "ID", error = "File is not a video" })
        {
            ContentType = "application/json",
            SerializerSettings = null,
            StatusCode = 405
        };
        
        var description = Data.EnsureNotNullConfig().VideoEmbedDescription;
        
        //return base.Content($"<!DOCTYPE html><html><head><title>{name}</title><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width, initial-scale=1, shrink-to-fit=no\"><link rel=\"stylesheet\" href=\"https://stackpath.bootstrapcdn.com/bootstrap/4.3.1/css/bootstrap.min.css\" integrity=\"sha384-ggOyR0iXCbMQv3Xipma34MD+dH/1fQ784/j6cY/iJTQUOhcWr7x9JvoRxT2MZw1T\" crossorigin=\"anonymous\"><script src=\"https://code.jquery.com/jquery-3.3.1.slim.min.js\" integrity=\"sha384-q8i/X+965DzO0rT7abK41JStQIAqVgRVzpbzo5smXKp4YfRvH+8abtTE1Pi6jizo\" crossorigin=\"anonymous\"></script><script src=\"https://cdnjs.cloudflare.com/ajax/libs/popper.js/1.14.7/umd/popper.min.js\" integrity=\"sha384-UO2eT0CpHqdSJQ6hJty5KVphtPhzWj9WO1clHTMGa3JDZwrnQq4sF86dIHNDz0W1\" crossorigin=\"anonymous\"></script><script src=\"https://stackpath.bootstrapcdn.com/bootstrap/4.3.1/js/bootstrap.min.js\" integrity=\"sha384-JjSmVgyd0p3pXB1rRibZUAYoIIy6OrQ6VrjIEaFf/nJGzIxFDsf4x0xIM+B07jRM\" crossorigin=\"anonymous\"></script><style>body{{background-color:black;color:white;}}</style></head><body><video controls autoplay><source src=\"/d/{id}\" type=\"video/{extension}\"></video></body></html>", "text/html");

        var config = Data.EnsureNotNullConfig();
        var html = System.IO.File.ReadAllText("VideoPlayer.html");
        html = html.Replace("%id%", id);
        html = html.Replace("%name%", name);
        html = html.Replace("%description%", description);
        html = html.Replace("%extension%", extension);
        html = html.Replace("%downloadUrl%", config.DownloadUrl + id);
        html = html.Replace("%rootUrl%", config.RootUrl);
        html = html.Replace("%thumbnail%", config.RootUrl + "t/" + id);
        html = html.Replace("%videoEmbedColor%", config.VideoEmbedColor);
        return base.Content(html, "text/html");
    }
    
    [HttpGet]
    [Route("m/{id}")]
    public IActionResult Metadata(string id)
    {
        var notFound = new JsonResult(new { generic = "NOT_FOUND", field = "ID", error = "File with id not found" })
        {
            ContentType = "application/json",
            SerializerSettings = null,
            StatusCode = 404
        };
        var streamId = Data.GetStreamIdByUploadId(id);
        if (streamId == null)
        {
            return notFound;
        }
        var extension = Data.GetExtensionFromId(id);
        var name = Data.GetNameFromId(id);
        var videoExtensions = new List<string> { ".MP4", ".MOV", ".M4V", ".AVI", ".WMV", ".MPG", ".MPEG", ".OGG", ".WEBM", ".MKV" };
        if (!videoExtensions.Contains("." + extension.ToUpper())) return new JsonResult(new { generic = "NOT_VIDEO", field = "ID", error = "File is not a video" })
        {
            ContentType = "application/json",
            SerializerSettings = null,
            StatusCode = 405
        };
        var path = Data.EnsureNotNullConfig().UploadFolder + streamId + ".upload";
        if (!System.IO.File.Exists(path)) return notFound;
        var metadata = Data.GetVideoMetadata(path);
        return new JsonResult(metadata.RawMetaData)
        {
            ContentType = "application/json",
            SerializerSettings = null,
            StatusCode = 200
        };
    }
    
    [HttpGet]
    [Route("t/{id}")]
    public IActionResult Thumbnail(string id)
    {
        var notFound = new JsonResult(new { generic = "NOT_FOUND", field = "ID", error = "File with id not found" })
        {
            ContentType = "application/json",
            SerializerSettings = null,
            StatusCode = 404
        };
        var streamId = Data.GetStreamIdByUploadId(id);
        if (streamId == null)
        {
            return notFound;
        }
        var extension = Data.GetExtensionFromId(id);
        var name = Data.GetNameFromId(id);
        extension ??= "";
        if (name == null)
        {
            var hash = Data.GetHashFromId(id);
            name = hash ?? "HASH_NOT_FOUND";
        }
        var path = Data.EnsureNotNullConfig().UploadFolder + streamId + ".upload";
        if (!System.IO.File.Exists(path)) return notFound;
        var videoExtensions = new List<string> { ".MP4", ".MOV", ".M4V", ".AVI", ".WMV", ".MPG", ".MPEG", ".OGG", ".WEBM", ".MKV" };
        if (!videoExtensions.Contains("." + extension.ToUpper())) return new JsonResult(new { generic = "NOT_VIDEO", field = "ID", error = "File is not a video" })
        {
            ContentType = "application/json",
            SerializerSettings = null,
            StatusCode = 405
        };
        var thumbnail = Data.GetVideoThumbnail(path, id);
        var thumbnailStream = new FileStream(Data.EnsureNotNullConfig().ThumbnailsFolder + thumbnail, FileMode.Open, FileAccess.Read);
        return new FileStreamResult(thumbnailStream, "image/jpeg");
    }
    
    
    [HttpGet]
    [Route("{id}")]
    public IActionResult Get(string id)
    {
        var notFound = new JsonResult(new { generic = "NOT_FOUND", field = "ID", error = "File with id not found" })
        {
            ContentType = "application/json",
            SerializerSettings = null,
            StatusCode = 404
        };
        var streamId = Data.GetStreamIdByUploadId(id);
        if (streamId == null)
        {
            return notFound;
        }
        var extension = Data.GetExtensionFromId(id);
        var name = Data.GetNameFromId(id);
        extension ??= "";
        if (name == null)
        {
            var hash = Data.GetHashFromId(id);
            name = hash ?? "HASH_NOT_FOUND";
        }

        var path = Data.EnsureNotNullConfig().UploadFolder + streamId + ".upload";
        if (!System.IO.File.Exists(path)) return notFound;
        var contentBuilder = new StringBuilder();
        var description = Data.EnsureNotNullConfig().EmbedDescription;
        contentBuilder.Append("<!DOCTYPE html>" +
                              $"<meta http-equiv=\"Refresh\" content=\"0; url='{Data.EnsureNotNullConfig().DownloadUrl + id}'\" />" +
                              "<meta charset='UTF-8'>" +
                              "<meta property='og:type' content='website'>" +
                              "<meta property='twitter:card' content='" + ( "summary_large_image") + "'>" +
                              $"<meta name='title' content='{name + "." + extension}'>" +
                              $"<meta property='og:title' content='{name + "." + extension}'>" +
                              $"<meta name='theme-color' content='{Data.EnsureNotNullConfig().EmbedColor}'>" +
                              $"<meta name='description' content='{description}'>");
        var imageExtensions = new List<string> { ".JPG", ".JPEG", ".JPE", ".BMP", ".GIF", ".PNG" };
        var videoExtensions = new List<string> { ".MP4", ".MOV", ".M4V", ".AVI", ".WMV", ".MPG", ".MPEG", ".OGG", ".WEBM", ".MKV" };
        if (imageExtensions.Contains("." + extension.ToUpper()))
        {
            contentBuilder.Append($"<meta property='og:image' content='{Data.EnsureNotNullConfig().DownloadUrl + id}'>" + 
                                  $"<meta property='twitter:image' content='{Data.EnsureNotNullConfig().DownloadUrl + id}'>" + 
                                  $"<meta property='og:description' content='{description}'>" +
                                  $"<meta property='twitter:description' content='{description}'>");
        }else
        if(videoExtensions.Contains("." + extension.ToUpper()))
        {
            contentBuilder.Append($"<meta property='og:image' content='{Data.EnsureNotNullConfig().RootUrl + "t/" + id}'>" + 
                                  $"<meta property='twitter:image' content='{Data.EnsureNotNullConfig().RootUrl + "t/" + id}'>" + 
                                  $"<meta property='og:description' content='{description + "\n" + "Watch video at: " + Data.EnsureNotNullConfig().VideoUrl + id}'>" +
                                  $"<meta property='twitter:description' content='{description + "\n" + "Watch video at: " + Data.EnsureNotNullConfig().VideoUrl + id}'>");
        }
        else
        {
            contentBuilder.Append($"<meta property='og:description' content='{description}'>" +
                                  $"<meta property='twitter:description' content='{description}'>");
        }

        return base.Content(contentBuilder.ToString(), "text/html");
    }

}