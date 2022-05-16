using System.Security.Cryptography;
using System.Text;
using HashLib;

namespace KekUploadServer;

public static class Data
{
    public static readonly UploadList UploadStreams = new();
    public static long MaxChunkSize => 0;

    public static string RandomString(int size)
    {
        const string str = "abcdefghijklmnopqrstuvwxyz0123456789";
        var randomString = "";
        var random = new Random();

        for (int i = 0; i < size; i++)
        {
            var x = random.Next(str.Length);
            randomString += str[x];
        }

        return randomString;
    }
    
    public static string HashStream(Stream stream) {
        var hash = SHA1.Create().ComputeHash(stream);
        return string.Concat(hash.Select(b => b.ToString("x2")));
    }
}