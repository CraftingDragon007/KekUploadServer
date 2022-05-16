using Microsoft.AspNetCore.Mvc;

namespace KekUploadServer.Controllers;

public class UploadController : Controller
{

    [HttpPost]
    [Route("u/{stream}/{hash}")]
    public IActionResult Upload(string stream, string hash)
    {
        if (Data.UploadStreams.GetByStreamId(stream) == null)
        {
            return new JsonResult(new
            {
                generic = "PARAM_LENGTH",
                field = "EXTENSION",
                error = "EXTENSION must be in bounds 10"
            }) {ContentType = "application/json", SerializerSettings = null, StatusCode = 404};
        }

        var uploadStream = HttpContext.Request.Body;
        if (uploadStream.Length > Data.MaxChunkSize)
        {
            return new JsonResult(new {generic = "OVERFLOW", field = "CHUNK", error = "Chunk size exceeded"})
                {ContentType = "application/json", SerializerSettings = null, StatusCode = 400};
        }
        
        var chunkHash = Data.HashStream(uploadStream);
    }

    [HttpPost]
    [Route("c/{ext}")]
    public IActionResult Create(string ext)
    {
        var streamId = Data.RandomString(64);
        try
        {
            Directory.CreateDirectory("uploads/" + streamId);
        }catch(Exception e)
        {
            return new JsonResult(new { generic = "FS_CREATE", field = "FILE", error = "Error while creating file: " + e.Message })
            {
                ContentType = "application/json",
                SerializerSettings = null,
                StatusCode = 500
            };
        }
        Data.UploadStreams.Add(new UploadItem(streamId, ext));
        return new JsonResult(new { stream = streamId });
    }
    
    [HttpPost]
    [Route("c/{ext}/{name}")]
    public IActionResult Create(string ext, string name)
    {
        var streamId = Data.RandomString(64);
        try
        {
            Directory.CreateDirectory("uploads/" + streamId);
        }catch(Exception e)
        {
            return new JsonResult(new { generic = "FS_CREATE", field = "FILE", error = "Error while creating file: " + e.Message })
            {
                ContentType = "application/json",
                SerializerSettings = null,
                StatusCode = 500
            };
        }
        Data.UploadStreams.Add(new UploadItem(streamId, ext, name));
        return new JsonResult(new { stream = streamId });
    }

    [HttpPost]
    [Route("r/{stream}")]
    public IActionResult Remove(string stream)
    {
        Directory.Delete("uploads/" + stream, true);
        return new JsonResult(new { success = true });
    }

    [HttpGet]
    [Route("d/{id}")]
    public IActionResult Download(string id)
    {
        
    }

}