namespace KekUploadServer;

public class UploadServerConfig
{
    public UploadServerConfig(int port, string address, string uploadFolder, string thumbnailsFolder, string webRoot, string databaseUser, string databasePassword, string databaseHost, string databaseName, string embedDescription, string embedColor, string videoEmbedDescription, string videoEmbedColor, string downloadUrl, int chunkSize, string videoUrl, string rootUrl, int idSize, string contactEmail)
    {
        Port = port;
        Address = address;
        UploadFolder = uploadFolder;
        ThumbnailsFolder = thumbnailsFolder;
        WebRoot = webRoot;
        DatabaseUser = databaseUser;
        DatabasePassword = databasePassword;
        DatabaseHost = databaseHost;
        DatabaseName = databaseName;
        EmbedDescription = embedDescription;
        EmbedColor = embedColor;
        DownloadUrl = downloadUrl;
        ChunkSize = chunkSize;
        VideoUrl = videoUrl;
        RootUrl = rootUrl;
        IdSize = idSize;
        VideoEmbedDescription = videoEmbedDescription;
        VideoEmbedColor = videoEmbedColor;
        ContactEmail = contactEmail;
    }
    
    public UploadServerConfig()
    {
        Port = 5102;
        Address = "0.0.0.0";
        UploadFolder = "uploads/";
        ThumbnailsFolder = "thumbs/";
        WebRoot = "web/";
        DatabaseUser = "username";
        DatabasePassword = "password";
        DatabaseHost = "host:port";
        DatabaseName = "database";
        EmbedDescription = "File";
        EmbedColor = "#ffffff";
        DownloadUrl = $"http://localhost:{Port}/d/";
        VideoUrl = $"http://localhost:{Port}/v/";
        RootUrl = $"http://localhost:{Port}";
        ChunkSize = 2048;
        IdSize = 8;
        VideoEmbedColor = "#007fff";
        VideoEmbedDescription = "Video";
        ContactEmail = "contact@example.com";
    }

    public int Port { get; set; }
    public string Address { get; set; }
    public string UploadFolder { get; set; }
    public string ThumbnailsFolder { get; set; }
    public string WebRoot { get; set; }
    public string DatabaseUser { get; set; }
    public string DatabasePassword { get; set; }
    public string DatabaseHost { get; set; }
    public string DatabaseName { get; set; }
    public string EmbedDescription { get; set; }
    public string EmbedColor { get; set; }
    public string DownloadUrl { get; set; }
    public string VideoUrl { get; set; }
    public string RootUrl { get; set; }
    public int ChunkSize { get; set; }
    public int IdSize { get; set; }
    public string VideoEmbedColor { get; set; }
    public string VideoEmbedDescription { get; set; }
    public string ContactEmail { get; set; }


    public override string ToString()
    {   
        return $"Port: {Port}\n" +
               $"Address: {Address}\n" +
               $"UploadFolder: {UploadFolder}\n" +
               $"ThumbnailsFolder: {ThumbnailsFolder}" +
               $"WebRoot: {WebRoot}\n" +
               $"DatabaseUser: {DatabaseUser}\n" +
               $"DatabasePassword: {DatabasePassword}\n" +
               $"DatabaseHost: {DatabaseHost}\n" +
               $"DatabaseName: {DatabaseName}\n" +
               $"EmbedDescription: {EmbedDescription}\n" +
               $"EmbedColor: {EmbedColor}\n" +
               $"VideoEmbedDescription: {VideoEmbedDescription}\n" +
               $"VideoEmbedColor: {VideoEmbedColor}\n" +
               $"DownloadUrl: {DownloadUrl}\n" +
               $"VideoUrl: {VideoUrl}\n" +
               $"RootUrl: {RootUrl}\n" +
               $"IdSize: {IdSize}\n" +
               $"ChunkSize: {ChunkSize}\n" +
               $"ContactEmail: {ContactEmail}";
    }
}