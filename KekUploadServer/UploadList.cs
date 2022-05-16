namespace KekUploadServer;

public class UploadList : List<UploadItem>
{
    public UploadItem? GetByStreamId(string streamId)
    {
        return this.Find(x => x.UploadStreamId == streamId);
    }
}