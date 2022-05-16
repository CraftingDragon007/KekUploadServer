namespace KekUploadServer;

public class UploadItem
{
    public UploadItem(string uploadStreamId, string extension)
    {
        UploadStreamId = uploadStreamId;
        Extension = extension;
    }
    public UploadItem(string uploadStreamId, string extension, string name)
    {
        UploadStreamId = uploadStreamId;
        Extension = extension;
        Name = name;
    }
    
    public string UploadStreamId { get; set; }
    public string Extension { get; set; }
    public string? Name { get; set; }
}