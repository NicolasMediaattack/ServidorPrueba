using System.Diagnostics;
using System.Globalization;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.StaticFiles;
using System.Collections.Concurrent;

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

ConcurrentDictionary<string, string> trimsDictionary =
    new ConcurrentDictionary<string, string>();

int trimCounter = 0;

app.MapPost("/mensaje", async (HttpRequest request) =>
{
    await semaphore.WaitAsync();

    try
    {
        var form = await request.ReadFormAsync();

        float startTime = float.Parse(form["startTime"], CultureInfo.InvariantCulture);
        float endTime = float.Parse(form["endTime"], CultureInfo.InvariantCulture);

        IFormFile? video = form.Files["video"];

        if (video == null)
            return Results.BadRequest("No video");

        string inputPath = Path.Combine("/tmp", $"{Guid.NewGuid()}_{video.FileName}");

        using (var stream = File.Create(inputPath))
        {
            await video.CopyToAsync(stream);
        }

        FfmpegModel ffmpeg = new FfmpegModel(inputPath, trimsPath, startTime, endTime);

        var (trimmedPath, thumbnailPath) = await ffmpeg.TrimVideo();

        int currentNumber =
            Interlocked.Increment(ref trimCounter);

        string trimKey =
            $"trim{currentNumber}";

        trimsDictionary[trimKey] =
            trimmedPath;

        Console.WriteLine(
            $"📦 Guardado: {trimKey} -> {trimmedPath}");

        if (trimsDictionary.Count > 1)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;

            Console.WriteLine(
                $"⚠️ Hay {trimsDictionary.Count} trims almacenados");

            Console.ResetColor();

            string mergedVideo =
                await ffmpeg.ConcatVideos(
                    trimsDictionary);

            Console.WriteLine(
                $"🎬 Resultado final: {mergedVideo}");

            // Copiar a carpeta pública

            string mergedVideoName =
                $"merged_{Guid.NewGuid()}.mp4";

            string publicMergedVideoPath =
                Path.Combine(
                    publicVideosPath,
                    mergedVideoName);

            File.Copy(
                mergedVideo,
                publicMergedVideoPath,
                true);

            string mergedVideoUrl =
                $"https://{request.Host}/videos/{mergedVideoName}";

            Console.ForegroundColor =
                ConsoleColor.Green;

            Console.WriteLine(
                $"🌍 MERGED VIDEO url: {mergedVideoUrl}");

            Console.ResetColor();

            return Results.Ok(new
            {
                videoUrl = mergedVideoUrl,
                thumbnailUrl = "",
                mergedVideoUrl
            });
        }

        FfmpegModel.ShowTrimVideos(trimsPath);

        string finalVideoName = $"final_{Guid.NewGuid()}.mp4";
        string finalVideoPath = Path.Combine(publicVideosPath, finalVideoName);
        File.Copy(trimmedPath, finalVideoPath, true);

        string finalThumbName = Path.GetFileNameWithoutExtension(finalVideoName) + ".jpg";
        string finalThumbPath = Path.Combine(publicVideosPath, finalThumbName);
        File.Copy(thumbnailPath, finalThumbPath, true);

        string videoUrl = $"https://{request.Host}/videos/{finalVideoName}";
        string thumbUrl = $"https://{request.Host}/videos/{finalThumbName}";

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"🌍 Video URL:     {videoUrl}");
        Console.WriteLine($"🖼️  Thumbnail URL: {thumbUrl}");
        Console.ResetColor();

        return Results.Ok(new { videoUrl, thumbnailUrl = thumbUrl });
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