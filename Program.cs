using System.Diagnostics;
using System.Globalization;

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

    string output = process.StandardOutput.ReadToEnd();

    process.WaitForExit();

    Console.ForegroundColor = ConsoleColor.Green;

    Console.WriteLine("✅ FFmpeg encontrado");

    Console.ResetColor();

    Console.WriteLine(output);
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;

    Console.WriteLine("❌ FFmpeg no encontrado");

    Console.WriteLine(ex.Message);

    Console.ResetColor();
}

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 500_000_000;
});

var app = builder.Build();

app.MapPost("/mensaje", async (HttpRequest request) =>
{
    await semaphore.WaitAsync();

    try
    {
        var form =
            await request.ReadFormAsync();

        float minDuration =
            float.Parse(
                form["minDuration"],
                CultureInfo.InvariantCulture);

        float maxDuration =
            float.Parse(
                form["maxDuration"],
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
                minDuration,
                maxDuration);

        string trimmedPath =
            await ffmpeg.TrimVideo();

        Console.WriteLine(
            $"✂️ Trimmed: {trimmedPath}");

        // =========================
        // NORMALIZE
        // =========================

        string normalizedPath =
            await ffmpeg.NormalizeVideo(
                trimmedPath);

        Console.WriteLine(
            $"📏 Normalized: {normalizedPath}");

        // =========================
        // FINAL VIDEO
        // =========================

        string finalVideoPath =
            "/tmp/final.mp4";

        // =========================
        // PRIMER VIDEO
        // =========================

        if (!File.Exists(finalVideoPath))
        {
            File.Copy(
                normalizedPath,
                finalVideoPath,
                true);
        }
        else
        {
            string concatPath =
                await ffmpeg.ConcatVideos(
                    finalVideoPath,
                    normalizedPath);

            File.Delete(finalVideoPath);

            File.Move(
                concatPath,
                finalVideoPath);
        }

        Console.WriteLine(
            "✅ Video concatenado");

        // =========================
        // DEBUG
        // =========================

        FfmpegModel.ShowTemporaryVideos();

        // =========================
        // RESPUESTA
        // =========================

        var multipartContent = new MultipartContent("mixed");

        // Video final concatenado
        byte[] finalBytes = await File.ReadAllBytesAsync(finalVideoPath);
        var finalContent = new ByteArrayContent(finalBytes);
        finalContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("video/mp4");
        finalContent.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment")
        {
            FileName = "final.mp4",
            Name = "final"
        };
        multipartContent.Add(finalContent);

        // Video trimmeado
        byte[] trimmedBytes = await File.ReadAllBytesAsync(trimmedPath);
        var trimmedContent = new ByteArrayContent(trimmedBytes);
        trimmedContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("video/mp4");
        trimmedContent.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment")
        {
            FileName = "trimmed.mp4",
            Name = "trimmed"
        };
        multipartContent.Add(trimmedContent);

        return Results.Stream(
            async stream => await multipartContent.CopyToAsync(stream),
            multipartContent.Headers.ContentType?.ToString() ?? "multipart/mixed");
    }
    finally
    {
        semaphore.Release();
    }
});

Console.ForegroundColor = ConsoleColor.Cyan;

Console.WriteLine("=================================");
Console.WriteLine("      SERVIDOR INICIADO          ");
Console.WriteLine("=================================");

Console.ResetColor();

app.Run();