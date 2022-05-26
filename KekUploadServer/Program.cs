using System.Net;
using KekUploadServer;
using Npgsql;

Console.WriteLine("░█░█░█▀▀░█░█░█░█░█▀█░█░░░█▀█░█▀█░█▀▄░█▀▀░█▀▀░█▀▄░█░█░█▀▀░█▀▄░░░█▀▀░▄█▄█▄\n" +
                  "░█▀▄░█▀▀░█▀▄░█░█░█▀▀░█░░░█░█░█▀█░█░█░▀▀█░█▀▀░█▀▄░▀▄▀░█▀▀░█▀▄░░░█░░░▄█▄█▄\n" +
                  "░▀░▀░▀▀▀░▀░▀░▀▀▀░▀░░░▀▀▀░▀▀▀░▀░▀░▀▀░░▀▀▀░▀▀▀░▀░▀░░▀░░▀▀▀░▀░▀░░░▀▀▀░░▀░▀░");


if (File.Exists("config.xml"))
{
    Data.LoadConfig();
    Console.WriteLine("Server starting with following settings:");
    Console.WriteLine(Data.EnsureNotNullConfig().ToString());
}
else
{
    Data.Config = new UploadServerConfig();
    Data.SaveConfig();
    Console.WriteLine("Please configure the server by editing config.xml");
    return;
}

var config = Data.EnsureNotNullConfig();

Data.MaxChunkSize = config.ChunkSize * 1024;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(config.Port);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

var cs = $"Host={config.DatabaseHost};Username={config.DatabaseUser};Password={config.DatabasePassword};Database={config.DatabaseName};";

var con = new NpgsqlConnection(cs);
con.Open();

Console.WriteLine("Database connection established");

var cmd = new NpgsqlCommand("SELECT version()", con);
var version = (string?)cmd.ExecuteScalar();
Console.WriteLine(version == null
    ? "WARNING: Could not determine database version! Proceed with caution!"
    : $"PostgreSQL version: {version}");

var cmd2 = new NpgsqlCommand("create table if not exists files\n" +
                             "(\n" +
                             "    id   char(7)     not null\n" +
                             "        primary key,\n" +
                             "    ext  varchar(10) not null,\n" +
                             "    hash char(40)    not null\n" +
                             ");", con);
var cmd3 = new NpgsqlCommand("create table if not exists id_mapping\n" +
                             "(\n    stream_id char(64) not null\n" +
                             "        constraint id_mapping_pk\n" +
                             "            primary key,\n" +
                             "    id        char(7),\n" +
                             "    name      varchar(255)\n);", con);
cmd2.ExecuteNonQuery();
cmd3.ExecuteNonQuery();

Data.Connection = con;

AppDomain.CurrentDomain.ProcessExit += (sender, args) =>
{
    con.Close();
    con.Dispose();
    foreach (var uploadStream in Data.UploadStreams)
    {
        uploadStream.FileStream.Close();
        File.Delete("uploads/" + uploadStream.UploadStreamId + ".upload");
    }
};

app.Run();