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
                generic = "PARAM_LENGTH",
                field = "EXTENSION",
                error = "EXTENSION must be in bounds 10"
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
        var streamId = Data.RandomString(64);
        FileStream fileStream;
        try
        {
            fileStream = System.IO.File.Create("uploads/" + streamId + ".upload");
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
        var streamId = Data.RandomString(64);
        FileStream fileStream;
        try
        {
            fileStream = System.IO.File.Create("uploads/" + streamId + ".upload");
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
            System.IO.File.Delete("uploads/" + stream + ".upload");
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
        var id = Data.RandomString(7);
        Data.InsertUploadedItemIntoDatabase(item, id);
        return new JsonResult(new { id = id }) {ContentType = "application/json", SerializerSettings = null, StatusCode = 200};
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
        if (extension == null)
        {
            extension = "";
        }
        if (name == null)
        {
            var hash = Data.GetHashFromId(id);
            name = hash ?? "HASH_NOT_FOUND";
        }

        if (System.IO.File.Exists("uploads/" + streamId + ".upload"))
        {
            return File(new FileStream("uploads/" + streamId + ".upload", FileMode.Open), "application/octet-stream", name + "." + extension);
        }
        return notFound;
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
        if (extension == null)
        {
            extension = "";
        }
        if (name == null)
        {
            var hash = Data.GetHashFromId(id);
            name = hash ?? "HASH_NOT_FOUND";
        }

        if (System.IO.File.Exists("uploads/" + streamId + ".upload"))
        {
            var contentBuilder = new StringBuilder();
            var description = Data.EnsureNotNullConfig().EmbedDescription;
            contentBuilder.Append("<!DOCTYPE html>" +
                                                                   $"<meta http-equiv=\"Refresh\" content=\"0; url='{Data.EnsureNotNullConfig().DownloadUrl + id}'\" />" +
                                                                   "<meta charset='UTF-8'>" +
                                                                   "<meta property='og:type' content='website'>" +
                                                                   "<meta property='twitter:card' content='summary_large_image'>" +
                                                                   $"<meta name='title' content='{name + "." + extension}'>" +
                                                                   $"<meta property='og:title' content='{name + "." + extension}'>" +
                                                                   $"<meta name='theme-color' content='{Data.EnsureNotNullConfig().EmbedColor}'>" +
                                                                   $"<meta name='description' content='{description}'>" +
                                                                   $"<meta property='og:description' content='{description}'>" +
                                                                   $"<meta property='twitter:description' content='{description}'>");
            var imageExtensions = new List<string> { ".JPG", ".JPEG", ".JPE", ".BMP", ".GIF", ".PNG" };
            if (imageExtensions.Contains("." + extension.ToUpper()))
            {
                contentBuilder.Append($"<meta property='og:image' content='{Data.EnsureNotNullConfig().DownloadUrl + id}'>" + $"<meta property='twitter:image' content='{Data.EnsureNotNullConfig().DownloadUrl + id}'>");
            }

            return base.Content(contentBuilder.ToString(), "text/html");
        }
        return notFound;
    }

}