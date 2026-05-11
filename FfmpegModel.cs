using Xabe.FFmpeg;

public class FfmpegModel
{
    public string video;
    public float minDuration;
    public float maxDuration;

    public FfmpegModel(
        string video,
        float minDuration,
        float maxDuration)
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

        if (duration <= 0)
        {
            throw new Exception(
                "La duración debe ser mayor que 0");
        }

        var conversion =
            await FFmpeg.Conversions
                .FromSnippet
                .Split(
                    video,
                    output,
                    TimeSpan.FromSeconds(minDuration),
                    TimeSpan.FromSeconds(duration));

        await conversion.Start();

        // MUY IMPORTANTE

        if (!File.Exists(output))
        {
            throw new Exception(
                "FFmpeg no generó el archivo");
        }

        FileInfo fileInfo =
            new FileInfo(output);

        Console.WriteLine(
            $"✅ Archivo generado: {fileInfo.Length} bytes");

        return output;
    }
}