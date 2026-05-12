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

/*app.MapPost("/mensaje", async (HttpRequest request) =>
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
});*/

app.MapPost("/mensaje", async (HttpContext context) =>
{
    var form =
        await context.Request.ReadFormAsync();

    float minDuration =
        float.Parse(form["minDuration"]);

    float maxDuration =
        float.Parse(form["maxDuration"]);

    IFormFile? video =
        form.Files["video"];

    if (video == null)
    {
        return Results.BadRequest(
            "No video");
    }

    // =========================
    // GUARDAR VIDEO ORIGINAL
    // =========================

    string inputPath =
        Path.Combine(
            "/tmp",
            $"{Guid.NewGuid()}_{video.FileName}");

    await using (var stream =
        File.Create(inputPath))
    {
        await video.CopyToAsync(stream);
    }

    Console.WriteLine(
        $"🎬 Video guardado: {inputPath}");

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

    // =========================
    // VIDEO FINAL ACUMULADO
    // =========================

    string finalVideoPath =
        "/tmp/final.mp4";

    // =========================
    // SI ES EL PRIMER VIDEO
    // =========================

    if (!File.Exists(finalVideoPath))
    {
        File.Copy(
            trimmedPath,
            finalVideoPath,
            true);
    }
    else
    {
        // =========================
        // CONCATENAR
        // =========================

        string concatenated =
            await ffmpeg.ConcatVideos(
                finalVideoPath,
                trimmedPath);

        // borrar viejo final
        File.Delete(finalVideoPath);

        // reemplazar
        File.Move(
            concatenated,
            finalVideoPath);
    }

    // =========================
    // AUTO CLEANUP
    // =========================

    context.Response.OnCompleted(() =>
    {
        try
        {
            if (File.Exists(inputPath))
                File.Delete(inputPath);

            if (File.Exists(trimmedPath))
                File.Delete(trimmedPath);

            Console.WriteLine(
                "🧹 Temporales eliminados");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

        return Task.CompletedTask;
    });

    // =========================
    // DEVOLVER VIDEO ACUMULADO
    // =========================

    return Results.File(
        finalVideoPath,
        "video/mp4",
        "final.mp4");
});

Console.ForegroundColor = ConsoleColor.Cyan;

Console.WriteLine("=================================");
Console.WriteLine("      SERVIDOR INICIADO          ");
Console.WriteLine("=================================");

Console.ResetColor();

app.Run();