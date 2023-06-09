namespace KekUploadServer;

public class UploadList : List<UploadItem>
{
    public UploadItem? GetByStreamId(string streamId)
    {
        return Find(x => x.UploadStreamId == streamId);
    }
}