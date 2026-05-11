using System.Diagnostics;

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

    string texto =
        form["texto"].ToString();

    float minDuration =
        float.Parse(form["minDuration"]);

    float maxDuration =
        float.Parse(form["maxDuration"]);

    Console.WriteLine(
        $"⏱️ Min: {minDuration} | Max: {maxDuration}");

    IFormFile? video =
        form.Files["video"];

    if (video == null)
    {
        return Results.BadRequest(
            "No se recibió video");
    }

    // =========================
    // GUARDAR VIDEO
    // =========================

    string rutaGuardado =
        Path.Combine(
            "/tmp",
            video.FileName);

    using (var stream =
        File.Create(rutaGuardado))
    {
        await video.CopyToAsync(stream);
    }

    Console.WriteLine(
        $"🎬 Video guardado: {rutaGuardado}");

    // =========================
    // RECORTAR VIDEO
    // =========================

    FfmpegModel ffmpeg =
        new FfmpegModel(
            rutaGuardado,
            minDuration,
            maxDuration);

    string trimmedVideo =
        await ffmpeg.TrimVideo();

    Console.WriteLine(
        $"✅ Video recortado: {trimmedVideo}");

    // =========================
    // DEVOLVER MP4
    // =========================

    return Results.File(
        trimmedVideo,
        "video/mp4",
        "trimmed.mp4");
});

Console.ForegroundColor = ConsoleColor.Cyan;

Console.WriteLine("=================================");
Console.WriteLine("      SERVIDOR INICIADO          ");
Console.WriteLine("=================================");

Console.ResetColor();

app.Run();