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

    // =========================
    // TIEMPOS
    // =========================

    float minTime =
        float.Parse(form["minTime"]);

    float maxTime =
        float.Parse(form["maxTime"]);

    // =========================
    // VIDEO
    // =========================

    var videoFile = form.Files["video"];

    if (videoFile == null)
    {
        return Results.BadRequest("No se recibió video");
    }

    // Guardar temporalmente

    string tempPath =
        Path.Combine(
            Path.GetTempPath(),
            videoFile.FileName);

    using (var stream =
        File.Create(tempPath))
    {
        await videoFile.CopyToAsync(stream);
    }

    // =========================
    // LOGS
    // =========================

    Console.ForegroundColor = ConsoleColor.Green;

    Console.WriteLine("\n✅ Video recibido");

    Console.ResetColor();

    Console.WriteLine($"Archivo: {videoFile.FileName}");
    Console.WriteLine($"MinTime: {minTime}");
    Console.WriteLine($"MaxTime: {maxTime}");

    Console.WriteLine($"Guardado en: {tempPath}");

    return Results.Ok(new
    {
        ok = true,
        mensaje = "Video recibido correctamente"
    });
});