using System.Diagnostics;
using System.Globalization;

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
    var form = await request.ReadFormAsync();

    string texto = form["texto"].ToString();

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
            "No se envió video");
    }

    if (video.Length == 0)
    {
        return Results.BadRequest(
            "Video vacío");
    }

    // =========================
    // GUARDAR VIDEO
    // =========================

    string inputPath =
        Path.Combine(
            "/tmp",
            $"{Guid.NewGuid()}_{video.FileName}");

    using (var stream = File.Create(inputPath))
    {
        await video.CopyToAsync(stream);
    }

    Console.WriteLine(
        $"🎬 Video guardado: {inputPath}");

    // =========================
    // RECORTAR
    // =========================

    if (minDuration < 0 || maxDuration < 0)
    {
        return Results.BadRequest(
            "Duraciones inválidas");
    }

    FfmpegModel ffmpeg =
        new FfmpegModel(
            inputPath,
            minDuration,
            maxDuration);

    string trimmedPath =
        await ffmpeg.TrimVideo();

    FfmpegModel.ShowTemporaryVideos();

    // =========================
    // LEER RESULTADO
    // =========================

    byte[] videoBytes =
        await File.ReadAllBytesAsync(trimmedPath);

    // =========================
    // LIMPIAR TEMPORALES
    // =========================

    File.Delete(inputPath);
    File.Delete(trimmedPath);

    // =========================
    // DEVOLVER VIDEO
    // =========================

    return Results.File(
        videoBytes,
        "video/mp4",
        "trimmed.mp4");
});

Console.ForegroundColor = ConsoleColor.Cyan;

Console.WriteLine("=================================");
Console.WriteLine("      SERVIDOR INICIADO          ");
Console.WriteLine("=================================");

Console.ResetColor();

app.Run();