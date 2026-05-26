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

// =========================
// CARPETA PUBLICA VIDEOS
// =========================

string publicVideosPath =
    Path.Combine(
        Directory.GetCurrentDirectory(),
        "videos");

Directory.CreateDirectory(
    publicVideosPath);

// =========================
// STATIC FILES
// =========================

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

        // =========================
        // INPUT
        // =========================

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

        // =========================
        // TRIM
        // =========================

        FfmpegModel ffmpeg =
            new FfmpegModel(
                inputPath,
                startTime,
                endTime);

        string trimmedPath =
            await ffmpeg.TrimVideo();

        Console.WriteLine(
            $"✂️ Trimmed: {trimmedPath}");

        // =========================
        // NORMALIZE
        // =========================

        /*string normalizedPath =
            await ffmpeg.NormalizeVideo(
                trimmedPath);

        Console.WriteLine(
            $"📏 Normalized: {normalizedPath}");*/

        // =========================
        // VIDEO FINAL PUBLICO
        // =========================

        string finalVideoName =
            $"final_{Guid.NewGuid()}.mp4";

        string finalVideoPath =
            Path.Combine(
                publicVideosPath,
                finalVideoName);

        // =========================
        // PRIMER VIDEO
        // =========================

        /*if (!File.Exists(finalVideoPath))
        {
            File.Copy(
                normalizedPath,
                finalVideoPath,
                true);
        }

        Console.WriteLine(
            "✅ Video generado");*/

        // =========================
        // URL PUBLICA
        // =========================

        string videoUrl =
            $"https://{request.Host}/videos/{finalVideoName}";

        Console.WriteLine(
            $"🌍 URL: {videoUrl}");

        // =========================
        // RESPUESTA
        // =========================

        return Results.Ok(videoUrl);
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