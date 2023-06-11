using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using SharpHash.Base;
using System;

namespace KekUploadServer.Controllers;

public class UploadController : Controller
{
    [HttpPost]
    [Route("u/{stream}/{hash}")]
    public async Task<IActionResult> Upload(string stream, string hash)
    {
        var item = Data.UploadStreams.GetByStreamId(stream);
        if (item == null)
        {
            return NotFound(new
            {
                generic = "NOT_FOUND",
                field = "STREAM",
                error = "Stream not found"
            });
        }

        var uploadStream = new MemoryStream();
        await HttpContext.Request.Body.CopyToAsync(uploadStream);
        var data = uploadStream.ToArray();
        if (data.Length > Data.MaxChunkSize)
            return new JsonResult(new { generic = "OVERFLOW", field = "CHUNK", error = "Chunk size exceeded" })
                { ContentType = "application/json", SerializerSettings = null, StatusCode = 400 };
        var chunkHash = HashFactory.Crypto.CreateSHA1().ComputeBytes(data).ToString().ToLower().Replace("-", "");
        if (chunkHash != hash)
            return new JsonResult(new { generic = "HASH_MISMATCH", field = "HASH", error = "Hash doesn't match" })
                { ContentType = "application/json", SerializerSettings = null, StatusCode = 400 };
        //uploadStream.CopyTo(item.FileStream);
        await item.FileStream.WriteAsync(data);
        item.Hash.TransformBytes(data);
        return new JsonResult(new { success = true })
            { ContentType = "application/json", SerializerSettings = null, StatusCode = 200 };
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
        }
        catch (Exception e)
        {
            return new JsonResult(new
                { generic = "FS_CREATE", field = "FILE", error = "Error while creating file: " + e.Message })
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
        }
        catch (Exception e)
        {
            return new JsonResult(new
                { generic = "FS_CREATE", field = "FILE", error = "Error while creating file: " + e.Message })
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
            return new JsonResult(new
                { generic = "FS_REMOVE", field = "FILE", error = "Error while deleting file: " + e.Message })
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
            return new JsonResult(new { generic = "NOT_FOUND", field = "STREAM", error = "Stream not found" })
            {
                ContentType = "application/json",
                SerializerSettings = null,
                StatusCode = 404
            };

        var fileHash = item.Hash.TransformFinal().ToString().ToLower();
        if (fileHash != hash)
            return new JsonResult(new { generic = "HASH_MISMATCH", field = "HASH", error = "Hash doesn't match" })
            {
                ContentType = "application/json",
                SerializerSettings = null,
                StatusCode = 400
            };
        item.FileStream.Close();
        Data.UploadStreams.Remove(item);
        var id = Data.RandomString(Data.EnsureNotNullConfig().IdSize);
        Data.InsertUploadedItemIntoDatabase(item, id);
        return new JsonResult(new { id })
            { ContentType = "application/json", SerializerSettings = null, StatusCode = 200 };
    }

    [HttpGet]
    [Route("d/{id}")]
    public IActionResult Download(string id)
    {
        var streamId = Data.GetStreamIdByUploadId(id);
        if (streamId == null) return NotFound(new { generic = "NOT_FOUND", field = "ID", error = "File with id not found" });

        var extension = Data.GetExtensionFromId(id);
        extension ??= "";
        var name = Data.GetNameFromId(id);
        if (name == null)
        {
            var hash = Data.GetHashFromId(id);
            name = hash ?? "HASH_NOT_FOUND";
        }
        
        var filePath = Path.Combine(Data.EnsureNotNullConfig().UploadFolder, streamId + ".upload");
        if (!System.IO.File.Exists(filePath))
            return NotFound(new {generic = "NOT_FOUND", field = "ID", error = "File with id not found"});
        var contentTypeEnumerable = MimeTypeMap.List.MimeTypeMap.GetMimeType(extension);
        var contentType = contentTypeEnumerable.FirstOrDefault();
        return PhysicalFile(Path.GetFullPath(filePath), contentType ?? "application/octet-stream", name + "." + extension);
    }

    [HttpGet]
    [Route("vs/{id}")]
    public async Task<IActionResult> StreamVideo(string id)
    {
        var notFound = NotFound(new { generic = "NOT_FOUND", field = "ID", error = "File with id not found" });
        var streamId = Data.GetStreamIdByUploadId(id);
        if (streamId == null) return notFound;
        var extension = Data.GetExtensionFromId(id);
        extension ??= "";
        var name = Data.GetNameFromId(id);
        if (name == null)
        {
            var hash = Data.GetHashFromId(id);
            name = hash ?? "HASH_NOT_FOUND";
        }

        var filePath = Path.Combine(Data.EnsureNotNullConfig().UploadFolder, streamId + ".upload");
        if (!System.IO.File.Exists(filePath)) return notFound;

        HttpContext.Response.ContentType = "video/" + extension;
        HttpContext.Response.Headers.Add("Content-Disposition", new ContentDispositionHeaderValue(name + (extension.Equals("none") ? "" : "." + extension)).ToString());

        using (var stream = new FileStream(filePath, FileMode.Open))
        {
            await stream.CopyToAsync(HttpContext.Response.Body);
        }

        return new EmptyResult();
    }

    [HttpGet]
    [Route("l/{id}")]
    public IActionResult Length(string id)
    {
        var notFound = NotFound(new { generic = "NOT_FOUND", field = "ID", error = "File with id not found" });
        var streamId = Data.GetStreamIdByUploadId(id);
        if (streamId == null) return notFound;

        var filePath = Path.Combine(Data.EnsureNotNullConfig().UploadFolder, streamId + ".upload");
        if (!System.IO.File.Exists(filePath)) return notFound;

        return Content(new FileInfo(filePath).Length.ToString());
    }

    [HttpGet]
    [Route("d/{id}/{startByte:long}/{length:int}")]
    public IActionResult DownloadRange(string id, long startByte, int length)
    {
        var notFound = NotFound(new { generic = "NOT_FOUND", field = "ID", error = "File with id not found" });
        var streamId = Data.GetStreamIdByUploadId(id);
        if (streamId == null) return notFound;

        var filePath = Path.Combine(Data.EnsureNotNullConfig().UploadFolder, streamId + ".upload");
        if (!System.IO.File.Exists(filePath)) return notFound;

        var extension = Data.GetExtensionFromId(id);
        extension ??= "";
        var name = Data.GetNameFromId(id);
        if (name == null)
        {
            var hash = Data.GetHashFromId(id);
            name = hash ?? "HASH_NOT_FOUND";
        }

        var fileSize = new FileInfo(filePath).Length;
        if (startByte >= fileSize) return notFound;

        var endByte = Math.Min(startByte + length - 1, fileSize - 1);
        var contentLength = endByte - startByte + 1;

        HttpContext.Response.Headers.Add("Content-Range", new ContentRangeHeaderValue(startByte, endByte, fileSize).ToString());
        HttpContext.Response.Headers.Add("Accept-Ranges", "bytes");
        HttpContext.Response.ContentType = "application/octet-stream";
        HttpContext.Response.Headers.Add("Content-Disposition", new ContentDispositionHeaderValue(name + (extension.Equals("none") ? "" : "." + extension)).ToString());

        using var stream = new FileStream(filePath, FileMode.Open);
        stream.Seek(startByte, SeekOrigin.Begin);
        var range = new byte[contentLength];
        var read = stream.Read(range, 0, (int)contentLength);
        stream.Close();
        return new FileContentResult(range, "application/octet-stream");
    }


    [HttpGet]
    [Route("v/{id}")]
    public IActionResult Video(string id)
    {
        var notFound = NotFound(new { generic = "NOT_FOUND", field = "ID", error = "File with id not found" });
        var streamId = Data.GetStreamIdByUploadId(id);
        if (streamId == null) return notFound;

        var extension = Data.GetExtensionFromId(id);
        var name = Data.GetNameFromId(id);
        extension ??= "";
        if (name == null)
        {
            var hash = Data.GetHashFromId(id);
            name = hash ?? "HASH_NOT_FOUND";
        }

        var filePath = Path.Combine(Data.EnsureNotNullConfig().UploadFolder, streamId + ".upload");
        if (!System.IO.File.Exists(filePath)) return notFound;

        var videoExtensions = new List<string>
            { ".MP4", ".MOV", ".M4V", ".AVI", ".WMV", ".MPG", ".MPEG", ".OGG", ".WEBM", ".MKV" };
        if (!videoExtensions.Contains("." + extension.ToUpper()))
        {
            return new JsonResult(new { generic = "NOT_VIDEO", field = "ID", error = "File is not a video" })
            {
                ContentType = "application/json",
                SerializerSettings = null,
                StatusCode = 405
            };
        }

        var description = Data.EnsureNotNullConfig().VideoEmbedDescription;
        var config = Data.EnsureNotNullConfig();

        var html = System.IO.File.ReadAllText(Path.GetFullPath("VideoPlayer.html"));
        html = html.Replace("%id%", id);
        html = html.Replace("%name%", name);
        html = html.Replace("%description%", description);
        html = html.Replace("%extension%", extension);
        html = html.Replace("%downloadUrl%", config.DownloadUrl + id);
        html = html.Replace("%rootUrl%", config.RootUrl);
        html = html.Replace("%thumbnail%", config.RootUrl + "t/" + id);
        html = html.Replace("%videoEmbedColor%", config.VideoEmbedColor);
        return Content(html, "text/html");
    }

    [HttpGet]
    [Route("m/{id}")]
    public IActionResult Metadata(string id)
    {
        var notFound = new JsonResult(new { generic = "NOT_FOUND", field = "ID", error = "File with id not found" })
        {
            ContentType = "application/json",
            StatusCode = 404
        };
        var streamId = Data.GetStreamIdByUploadId(id);
        if (streamId == null) return notFound;
        var extension = Data.GetExtensionFromId(id) ?? string.Empty;
        var videoExtensions = new HashSet<string> { ".MP4", ".MOV", ".M4V", ".AVI", ".WMV", ".MPG", ".MPEG", ".OGG", ".WEBM", ".MKV" };
        if (!videoExtensions.Contains("." + extension.ToUpper()))
        {
            return new JsonResult(new { generic = "NOT_VIDEO", field = "ID", error = "File is not a video" })
            {
                ContentType = "application/json",
                StatusCode = 405
            };
        }
        var path = Data.EnsureNotNullConfig().UploadFolder + streamId + ".upload";
        if (!System.IO.File.Exists(path)) return notFound;
        var metadata = Data.GetVideoMetadata(path);
        return new JsonResult(metadata.RawMetaData)
        {
            ContentType = "application/json",
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
            StatusCode = 404
        };
        var streamId = Data.GetStreamIdByUploadId(id);
        if (streamId == null) return notFound;
        var extension = Data.GetExtensionFromId(id) ?? string.Empty;
        var name = Data.GetNameFromId(id) ?? Data.GetHashFromId(id) ?? "HASH_NOT_FOUND";
        var path = Data.EnsureNotNullConfig().UploadFolder + streamId + ".upload";
        if (!System.IO.File.Exists(path)) return notFound;
        var videoExtensions = new HashSet<string> { ".MP4", ".MOV", ".M4V", ".AVI", ".WMV", ".MPG", ".MPEG", ".OGG", ".WEBM", ".MKV" };
        if (!videoExtensions.Contains("." + extension.ToUpper()))
        {
            return new JsonResult(new { generic = "NOT_VIDEO", field = "ID", error = "File is not a video" })
            {
                ContentType = "application/json",
                StatusCode = 405
            };
        }
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
            StatusCode = 404
        };
        var streamId = Data.GetStreamIdByUploadId(id);
        if (streamId == null) return notFound;
        var extension = Data.GetExtensionFromId(id) ?? string.Empty;
        var name = Data.GetNameFromId(id) ?? Data.GetHashFromId(id) ?? "HASH_NOT_FOUND";
        var path = Data.EnsureNotNullConfig().UploadFolder + streamId + ".upload";
        if (!System.IO.File.Exists(path)) return notFound;
        var contentBuilder = new StringBuilder();
        var description = Data.EnsureNotNullConfig().EmbedDescription;
        contentBuilder.Append("<!DOCTYPE html>" +
                              $"<meta http-equiv=\"Refresh\" content=\"0; url='{Data.EnsureNotNullConfig().DownloadUrl + id}'\" />" +
                              "<meta charset='UTF-8'>" +
                              "<meta property='og:type' content='website'>" +
                              "<meta property='twitter:card' content='summary_large_image'>");
        contentBuilder.Append($"<meta name='title' content='{name + "." + extension}'>" +
                              $"<meta property='og:title' content='{name + "." + extension}'>" +
                              $"<meta name='theme-color' content='{Data.EnsureNotNullConfig().EmbedColor}'>" +
                              $"<meta name='description' content='{description}'>");
        var imageExtensions = new HashSet<string> { ".JPG", ".JPEG", ".JPE", ".BMP", ".GIF", ".PNG" };
        var videoExtensions = new HashSet<string>
            { ".MP4", ".MOV", ".M4V", ".AVI", ".WMV", ".MPG", ".MPEG", ".OGG", ".WEBM", ".MKV" };
        if (imageExtensions.Contains("." + extension.ToUpper()))
        {
            contentBuilder.Append(
                $"<meta property='og:image' content='{Data.EnsureNotNullConfig().DownloadUrl + id}'>" +
                $"<meta property='twitter:image' content='{Data.EnsureNotNullConfig().DownloadUrl + id}'>");
        }
        else if (videoExtensions.Contains("." + extension.ToUpper()))
        {
            contentBuilder.Append(
                $"<meta property='og:image' content='{Data.EnsureNotNullConfig().RootUrl}t/{id}'>" +
                $"<meta property='twitter:image' content='{Data.EnsureNotNullConfig().RootUrl}t/{id}'>" +
                $"<meta property='og:description' content='{description}\nWatch video at: {Data.EnsureNotNullConfig().VideoUrl + id}'>" +
                $"<meta property='twitter:description' content='{description}\nWatch video at: {Data.EnsureNotNullConfig().VideoUrl + id}'>");
        }
        else
        {
            contentBuilder.Append($"<meta property='og:description' content='{description}'>" +
                                  $"<meta property='twitter:description' content='{description}'>");
        }
        return Content(contentBuilder.ToString(), "text/html");
    }
}