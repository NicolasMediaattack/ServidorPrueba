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
        // Nombre output

        string output =
            Path.Combine(
                "/tmp",
                $"cut_{Guid.NewGuid()}.mp4");

        // Duración real

        float duration =
            maxDuration - minDuration;

        // Crear conversión

        var conversion = await FFmpeg.Conversions.FromSnippet.Split(
            video,
            output,
            TimeSpan.FromSeconds(minDuration),
            TimeSpan.FromSeconds(duration));

        // Ejecutar

        await conversion.Start();

        Console.WriteLine("✅ Video recortado");

        return output;
    }

    void concatVideos()
    {
        // Aquí iría la lógica para concatenar videos usando FFmpeg
    }
}