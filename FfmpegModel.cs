using Xabe.FFmpeg;
public class FfmpegModel
{
    public string video;
    public float minDuration;
    public float maxDuration;

    public FfmpegModel(string video, float minDuration, float maxDuration)
    {
        this.video = video;
        this.minDuration = minDuration;
        this.maxDuration = maxDuration;
    }

    public async Task<string> TrimVideo()
    {
        string output =
            Path.Combine(
                "/tmp",
                $"cut_{Guid.NewGuid()}.mp4");

        float duration =
            maxDuration - minDuration;

        try
        {
            var conversion =
                await FFmpeg.Conversions
                    .FromSnippet
                    .Split(
                        video,
                        output,
                        TimeSpan.FromSeconds(minDuration),
                        TimeSpan.FromSeconds(duration));

            await conversion.Start();

            Console.WriteLine(
                "✅ FFmpeg terminó correctamente");
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                "⚠️ Xabe lanzó excepción");

            Console.WriteLine(ex.Message);

            // MUY IMPORTANTE:
            // verificar si el archivo sí existe

            if (File.Exists(output))
            {
                Console.WriteLine(
                    "✅ Pero el video SÍ fue generado");

                return output;
            }

            throw;
        }

        return output;
    }

    void concatVideos()
    {
        // Aquí iría la lógica para concatenar videos usando FFmpeg
    }
}