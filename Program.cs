using System.Diagnostics;

var process = new Process();

process.StartInfo.FileName = "ffmpeg";
process.StartInfo.Arguments = "-version";

process.StartInfo.RedirectStandardOutput = true;

process.Start();

string output = process.StandardOutput.ReadToEnd();

process.WaitForExit();

Console.WriteLine(output);

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 500_000_000; // 500 MB
});
var app = builder.Build();

app.MapPost("/mensaje", async (HttpRequest request) =>
{
    // Leer el formulario multipart
    var form = await request.ReadFormAsync();

    string texto = form["texto"].ToString();

    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("\n✅ Mensaje recibido del cliente:");
    Console.ResetColor();
    Console.WriteLine($"   → Texto: {texto}");

    Console.WriteLine($"   (Recibido a las {DateTime.Now:HH:mm:ss})");

    return Results.Ok(new { respuesta = "Mensaje recibido correctamente", ok = true });
});

Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("=================================");
Console.WriteLine("   SERVIDOR ESCUCHANDO en :5000  ");
Console.WriteLine("=================================");
Console.ResetColor();
Console.WriteLine("Esperando mensajes...\n");

app.Run();