using System.Text;
using System.Xml;
using System.Xml.Serialization;
using MediaToolkit.Core.Infrastructure;
using MediaToolkit.Core.Meta;
using MediaToolkit.Core.Services;
using Npgsql;
using SharpHash.Base;

namespace KekUploadServer;

public static class Data
{
    public static bool IsDataBaseCommandRunning;
    public static readonly UploadList UploadStreams = new();
    public static long MaxChunkSize { get; set; }

    public static UploadServerConfig? Config { get; set; }
    public static NpgsqlConnection? Connection { get; set; }

    public static string RandomString(int size)
    {
        const string str = "abcdefghijklmnopqrstuvwxyz0123456789";
        var randomString = "";
        var random = new Random();

        for (var i = 0; i < size; i++)
        {
            var x = random.Next(str.Length);
            randomString += str[x];
        }

        return randomString;
    }

    public static string HashStream(Stream stream)
    {
        var hash = HashFactory.Crypto.CreateSHA1();
        return hash.ComputeStream(stream).ToString().ToLower();
    }

    public static void LoadConfig()
    {
        var serializer = new XmlSerializer(typeof(UploadServerConfig));
        var reader = XmlReader.Create("config.xml");
        var config = (UploadServerConfig?)serializer.Deserialize(reader);
        Config = config ?? throw new Exception("Config file cannot be parsed!");
    }

    public static void SaveConfig()
    {
        var serializer = new XmlSerializer(typeof(UploadServerConfig));
        var fileStream = new FileStream("config.xml", FileMode.OpenOrCreate, FileAccess.Write);
        var writer = new XmlTextWriter(fileStream, Encoding.UTF8);
        writer.Formatting = Formatting.Indented;
        serializer.Serialize(writer, Config);
    }

    public static UploadServerConfig EnsureNotNullConfig()
    {
        if (Config == null) throw new NullReferenceException("The config is null!");
        return Config;
    }

    public static T EnsureNotNull<T>(T? obj)
    {
        if (obj == null) throw new NullReferenceException("The " + typeof(T).Name + " object is null!");
        return obj;
    }

    public static string GetVideoThumbnail(string videoPath, string id)
    {
        var folder = EnsureNotNullConfig().ThumbnailsFolder;
        Directory.CreateDirectory(folder);

        if (File.Exists(folder + id + ".jpg")) return id + ".jpg";

        var tcs = new TaskCompletionSource<bool>();

        var mediaConverter = new MediaConverterService(new FFmpegServiceConfiguration());
        mediaConverter.OnCompleteEventHandler += (sender, args) => { tcs.SetResult(true); };

        var builder = new ExtractThumbnailInstructionBuilder
        {
            InputFilePath = videoPath,
            OutputFilePath = folder + id + ".jpg",
            SeekFrom = TimeSpan.FromSeconds(1)
        };

        mediaConverter.ExecuteInstructionAsync(builder).Wait();
        tcs.Task.Wait();
        return id + ".jpg";
    }

    public static Metadata GetVideoMetadata(string videoPath)
    {
        var tcs = new TaskCompletionSource<Metadata>();
        var metadataService = new MetadataService(new FFprobeServiceConfiguration());
        metadataService.OnMetadataProcessedEventHandler += (sender, args) => { tcs.TrySetResult(args.Metadata); };
        var instruction = new GetMetadataInstructionBuilder
        {
            InputFilePath = videoPath
        };
        metadataService.ExecuteInstructionAsync(instruction).Wait();
        tcs.Task.Wait();
        return tcs.Task.Result;
    }

    public static void InsertUploadedItemIntoDatabase(UploadItem item, string id)
    {
        var con = EnsureNotNull(Connection);
        while (IsDataBaseCommandRunning) Thread.Sleep(100);
        IsDataBaseCommandRunning = true;
        var cmd = new NpgsqlCommand("insert into files (id, ext, hash) values (@id, @ext, @hash)", con);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@ext", item.Extension);
        cmd.Parameters.AddWithValue("@hash", item.Hash.TransformFinal().ToString().ToLower());
        NpgsqlCommand cmd2;
        if (item.Name == null)
        {
            cmd2 = new NpgsqlCommand("insert into id_mapping (stream_id, id) values (@stream_id, @id)", con);
            cmd2.Parameters.AddWithValue("@stream_id", item.UploadStreamId);
            cmd2.Parameters.AddWithValue("@id", id);
        }
        else
        {
            cmd2 = new NpgsqlCommand("insert into id_mapping (stream_id, id, name) values (@stream_id, @id, @name)",
                con);
            cmd2.Parameters.AddWithValue("@stream_id", item.UploadStreamId);
            cmd2.Parameters.AddWithValue("@id", id);
            cmd2.Parameters.AddWithValue("@name", item.Name);
        }

        cmd.ExecuteNonQuery();
        cmd2.ExecuteNonQuery();
        IsDataBaseCommandRunning = false;
    }

    public static string? GetIdFromStreamId(string streamId)
    {
        var con = EnsureNotNull(Connection);
        while (IsDataBaseCommandRunning) Thread.Sleep(100);
        IsDataBaseCommandRunning = true;
        var cmd = new NpgsqlCommand("select id from id_mapping where stream_id = @stream_id", con);
        cmd.Parameters.AddWithValue("@stream_id", streamId);
        var result = cmd.ExecuteScalar();
        IsDataBaseCommandRunning = false;
        switch (result)
        {
            case null:
            case DBNull:
                return null;
            default:
                return (string?)result;
        }
    }

    public static string? GetStreamIdByUploadId(string id)
    {
        var con = EnsureNotNull(Connection);
        while (IsDataBaseCommandRunning) Thread.Sleep(100);
        IsDataBaseCommandRunning = true;
        var cmd = new NpgsqlCommand("select stream_id from id_mapping where id = @id", con);
        cmd.Parameters.AddWithValue("@id", id);
        var result = cmd.ExecuteScalar();
        IsDataBaseCommandRunning = false;
        switch (result)
        {
            case null:
            case DBNull:
                return null;
            default:
                return (string?)result;
        }
    }

    public static string? GetExtensionFromId(string id)
    {
        var con = EnsureNotNull(Connection);
        while (IsDataBaseCommandRunning) Thread.Sleep(100);
        IsDataBaseCommandRunning = true;
        var cmd = new NpgsqlCommand("select ext from files where id = @id", con);
        cmd.Parameters.AddWithValue("@id", id);
        var result = cmd.ExecuteScalar();
        IsDataBaseCommandRunning = false;
        switch (result)
        {
            case null:
            case DBNull:
                return null;
            default:
                return (string?)result;
        }
    }

    public static string? GetNameFromId(string id)
    {
        var con = EnsureNotNull(Connection);
        while (IsDataBaseCommandRunning) Thread.Sleep(100);
        IsDataBaseCommandRunning = true;
        var cmd = new NpgsqlCommand("select name from id_mapping where id = @id", con);
        cmd.Parameters.AddWithValue("@id", id);
        var result = cmd.ExecuteScalar();
        IsDataBaseCommandRunning = false;
        switch (result)
        {
            case null:
            case DBNull:
                return null;
            default:
                return (string?)result;
        }
    }

    public static string? GetHashFromId(string id)
    {
        var con = EnsureNotNull(Connection);
        while (IsDataBaseCommandRunning) Thread.Sleep(100);
        IsDataBaseCommandRunning = true;
        var cmd = new NpgsqlCommand("select hash from files where id = @id", con);
        cmd.Parameters.AddWithValue("@id", id);
        var result = cmd.ExecuteScalar();
        IsDataBaseCommandRunning = false;
        switch (result)
        {
            case null:
            case DBNull:
                return null;
            default:
                return (string?)result;
        }
    }
}