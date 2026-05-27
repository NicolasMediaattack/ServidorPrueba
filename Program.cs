using System.Diagnostics;
using System.Globalization;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.StaticFiles;

SemaphoreSlim semaphore =
    new SemaphoreSlim(1, 1);

try
{
    var process = new Process();

    process.StartInfo.FileName = "ffmpeg";
    process.StartInfo.Arguments = "-version";

    process.StartInfo.RedirectStandardOutput = true;
    process.StartInfo.RedirectStandardError = true;

    process.StartInfo.UseShellExecute = false;

    process.Start();

    string output =
        process.StandardOutput.ReadToEnd();

    process.WaitForExit();

    Console.ForegroundColor =
        ConsoleColor.Green;

    Console.WriteLine(
        "✅ FFmpeg encontrado");

    Console.ResetColor();

    Console.WriteLine(output);
}
catch (Exception ex)
{
    Console.ForegroundColor =
        ConsoleColor.Red;

    Console.WriteLine(
        "❌ FFmpeg no encontrado");

    Console.WriteLine(ex.Message);

    Console.ResetColor();
}

var builder =
    WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize =
        500_000_000;
});

var app = builder.Build();

string publicVideosPath =
    Path.Combine(
        Directory.GetCurrentDirectory(),
        "videos");

Directory.CreateDirectory(
    publicVideosPath);

string trimsPath =
    Path.Combine(
        Directory.GetCurrentDirectory(),
        "trims");

Directory.CreateDirectory(
    trimsPath);

var provider =
    new FileExtensionContentTypeProvider();

provider.Mappings[".mp4"] =
    "video/mp4";

app.UseStaticFiles(
    new StaticFileOptions
    {
        FileProvider =
            new PhysicalFileProvider(
                publicVideosPath),

        RequestPath = "/videos",

        ContentTypeProvider =
            provider
    });

app.MapPost("/mensaje", async (HttpRequest request) =>
{
    await semaphore.WaitAsync();

    try
    {
        var form =
            await request.ReadFormAsync();

        float startTime =
            float.Parse(
                form["startTime"],
                CultureInfo.InvariantCulture);

        float endTime =
            float.Parse(
                form["endTime"],
                CultureInfo.InvariantCulture);

        IFormFile? video =
            form.Files["video"];

        if (video == null)
        {
            return Results.BadRequest(
                "No video");
        }

        string inputPath =
            Path.Combine(
                "/tmp",
                $"{Guid.NewGuid()}_{video.FileName}");

        using (var stream =
            File.Create(inputPath))
        {
            await video.CopyToAsync(stream);
        }

        Console.WriteLine(
            $"🎬 Input: {inputPath}");

        FfmpegModel ffmpeg =
            new FfmpegModel(
                inputPath,
                trimsPath,
                startTime,
                endTime);

        var result =
            await ffmpeg.TrimVideo();

            string trimmedPath =
                result.videoPath;

            string thumbnailPath =
                result.thumbnailPath;

        Console.WriteLine(
            $"✂️ Trimmed: {trimmedPath}");

        FfmpegModel.ShowTrimVideos(
            trimsPath);

        string finalVideoName =
            $"final_{Guid.NewGuid()}.mp4";

        string finalVideoPath =
            Path.Combine(
                publicVideosPath,
                finalVideoName);

        string finalThumbnailName =
            $"thumb_{Guid.NewGuid()}.jpg";

        string finalThumbnailPath =
            Path.Combine(
                publicVideosPath,
                finalThumbnailName);

        if (!File.Exists(finalVideoPath))
        {
            File.Copy(
                trimmedPath,
                finalVideoPath,
                true);

            File.Copy(
                thumbnailPath,
                finalThumbnailPath,
                true);
        }

        Console.WriteLine(
            "✅ Video generado");

        string videoUrl =
            $"https://{request.Host}/videos/{finalVideoName}";

        string thumbnailUrl =
            $"https://{request.Host}/videos/{finalThumbnailName}";

        Console.WriteLine(
            $"🌍 URL: {videoUrl}");

        return Results.Ok(new
            {
                video = videoUrl,
                thumbnail = thumbnailUrl
            });
    }
    finally
    {
        semaphore.Release();
    }
});

Console.ForegroundColor =
    ConsoleColor.Cyan;

Console.WriteLine(
    "=================================");

Console.WriteLine(
    "      SERVIDOR INICIADO          ");

Console.WriteLine(
    "=================================");

Console.ResetColor();

app.Run();