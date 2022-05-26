namespace KekUploadServer;

public class UploadServerConfig
{
    public UploadServerConfig(int port, string address, string apiBaseUrl, string tmpFolder, string uploadFolder, string webRoot, string databaseUser, string databasePassword, string databaseHost, string databaseName, string embedDescription, string embedColor, string downloadUrl, int chunkSize, string videoUrl)
    {
        Port = port;
        Address = address;
        ApiBaseUrl = apiBaseUrl;
        TmpFolder = tmpFolder;
        UploadFolder = uploadFolder;
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
    }
    
    public UploadServerConfig()
    {
        Port = 6942;
        Address = "0.0.0.0";
        ApiBaseUrl = "/api/";
        TmpFolder = "tmp/";
        UploadFolder = "uploads/";
        WebRoot = "web/";
        DatabaseUser = "username";
        DatabasePassword = "password";
        DatabaseHost = "host:port";
        DatabaseName = "database";
        EmbedDescription = "File";
        EmbedColor = "#ffffff";
        DownloadUrl = "http://localhost:6942/api/d/";
        VideoUrl = "http://localhost:6942/api/v/";
        ChunkSize = 2048;
    }

    public int Port { get; set; }
    public string Address { get; set; }
    
    public string ApiBaseUrl { get; set; }
    
    public string TmpFolder { get; set; }
    public string UploadFolder { get; set; }
    public string WebRoot { get; set; }
    
    public string DatabaseUser { get; set; }
    public string DatabasePassword { get; set; }
    public string DatabaseHost { get; set; }
    public string DatabaseName { get; set; }
    
    public string EmbedDescription { get; set; }
    public string EmbedColor { get; set; }
    public string DownloadUrl { get; set; }
    public string VideoUrl { get; set; }
    
    public int ChunkSize { get; set; }

    public override string ToString()
    {   
        return $"Port: {Port}\n" +
               $"Address: {Address}\n" +
               $"ApiBaseUrl: {ApiBaseUrl}\n" +
               $"TmpFolder: {TmpFolder}\n" +
               $"UploadFolder: {UploadFolder}\n" +
               $"WebRoot: {WebRoot}\n" +
               $"DatabaseUser: {DatabaseUser}\n" +
               $"DatabasePassword: {DatabasePassword}\n" +
               $"DatabaseHost: {DatabaseHost}\n" +
               $"DatabaseName: {DatabaseName}\n" +
               $"EmbedDescription: {EmbedDescription}\n" +
               $"EmbedColor: {EmbedColor}\n" +
               $"DownloadUrl: {DownloadUrl}\n" +
               $"ChunkSize: {ChunkSize}";
    }
}