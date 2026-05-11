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

    string texto = form["texto"].ToString();

    // ✅ Leer las duraciones
    float minDuration = float.Parse(form["minDuration"].ToString(), System.Globalization.CultureInfo.InvariantCulture);
    float maxDuration = float.Parse(form["maxDuration"].ToString(), System.Globalization.CultureInfo.InvariantCulture);

    Console.WriteLine($"⏱️ Min duración: {minDuration}s | Max duración: {maxDuration}s");

    // ✅ Recibir el video
    IFormFile? video = form.Files["video"];

    if (video != null)
    {
        string rutaGuardado = Path.Combine("/tmp", video.FileName);

        using var stream = File.Create(rutaGuardado);
        await video.CopyToAsync(stream);

        Console.WriteLine($"🎬 Video recibido: {video.FileName} ({video.Length / 1_000_000} MB)");
        Console.WriteLine($"   Guardado en: {rutaGuardado}");
    }

    Console.WriteLine($"→ Texto: {texto}");

    return Results.Ok(new { respuesta = "Recibido correctamente", ok = true });
});

Console.ForegroundColor = ConsoleColor.Cyan;

Console.WriteLine("=================================");
Console.WriteLine("      SERVIDOR INICIADO          ");
Console.WriteLine("=================================");

Console.ResetColor();

app.Run();