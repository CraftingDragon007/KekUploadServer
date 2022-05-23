using SharpHash.Base;
using SharpHash.Interfaces;

namespace KekUploadServer;

public class UploadItem
{
    public UploadItem(string uploadStreamId, string extension, FileStream fileStream)
    {
        UploadStreamId = uploadStreamId;
        Extension = extension;
        FileStream = fileStream;
        Hash = HashFactory.Crypto.CreateSHA1();
        Hash.Initialize();
    }
    public UploadItem(string uploadStreamId, string extension, string name, FileStream fileStream)
    {
        UploadStreamId = uploadStreamId;
        Extension = extension;
        Name = name;
        FileStream = fileStream;
        Hash = HashFactory.Crypto.CreateSHA1();
        Hash.Initialize();
    }
    
    public string UploadStreamId { get; set; }
    public string Extension { get; set; }
    public string? Name { get; set; }
    public FileStream FileStream { get; set; }
    public IHash Hash { get; set; }
}