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
        // CARPETA TRIMS
        // =========================

        string trimsFolder =
            "/tmp/trims";

        Directory.CreateDirectory(
            trimsFolder);

        // =========================
        // GUARDAR NORMALIZED
        // =========================

        string storedVideoPath =
            Path.Combine(
                trimsFolder,
                $"{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid()}.mp4");

        File.Copy(
            normalizedPath,
            storedVideoPath,
            true);

        Console.WriteLine(
            $"💾 Stored: {storedVideoPath}");

        // =========================
        // TODOS LOS VIDEOS
        // =========================

        string[] allVideos =
            Directory.GetFiles(
                trimsFolder,
                "*.mp4")
            .OrderBy(x => x)
            .ToArray();

        Console.WriteLine(
            $"🎞️ Total videos: {allVideos.Length}");

        // =========================
        // LIST.TXT COMPLETO
        // =========================

        string listPath =
            Path.Combine(
                "/tmp",
                "list.txt");

        List<string> lines =
            new List<string>();

        foreach (string videoFile in allVideos)
        {
            lines.Add(
                $"file '{videoFile}'");
        }

        await File.WriteAllLinesAsync(
            listPath,
            lines);

        Console.WriteLine(
            $"📝 List generado: {listPath}");

        // =========================
        // FINAL VIDEO
        // =========================

        string finalVideoPath =
            "/tmp/final.mp4";

        await ffmpeg.ConcatFromList(
            listPath,
            finalVideoPath);

        Console.WriteLine(
            "✅ Video final regenerado");

        Console.WriteLine(
            "✅ Video concatenado");

        // =========================
        // DEBUG
        // =========================

        FfmpegModel.ShowTemporaryVideos();

        // =========================
        // RESPUESTA
        // =========================

        byte[] bytes =
            await File.ReadAllBytesAsync(
                finalVideoPath);

        return Results.File(
            bytes,
            "video/mp4",
            "final.mp4");
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