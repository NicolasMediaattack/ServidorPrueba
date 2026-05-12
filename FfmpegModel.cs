using Xabe.FFmpeg;
public class FfmpegModel
{
    private readonly string videoPath;
    private readonly float minDuration;
    private readonly float maxDuration;

    public FfmpegModel(string videoPath, float minDuration, float maxDuration)
    {
        this.videoPath = videoPath;
        this.minDuration = minDuration;
        this.maxDuration = maxDuration;
    }

    public async Task<string> TrimVideo()
    {
        // =========================
        // CONFIGURAR FFMPEG
        // =========================

        FFmpeg.SetExecutablesPath("/usr/bin");

        // =========================
        // LEER INFO VIDEO
        // =========================

        IMediaInfo mediaInfo = await FFmpeg.GetMediaInfo(videoPath);

        TimeSpan totalDuration = mediaInfo.Duration;

        Console.WriteLine($"🎞️ Duración total: {totalDuration}");

        // =========================
        // CALCULAR RECORTE
        // =========================

        TimeSpan startTime = TimeSpan.FromSeconds(minDuration);
        TimeSpan finalDuration = totalDuration - TimeSpan.FromSeconds(minDuration) - TimeSpan.FromSeconds(maxDuration);

        if (finalDuration.TotalSeconds <= 0)
        {
            throw new Exception("⛔ Duración final no válida. Verifica los valores de minDuration y maxDuration.");
        }

        // =========================
        // OUTPUT
        // =========================

        string outputPath = Path.Combine("/tmp", $"trimmed_{Guid.NewGuid()}.mp4");

        // =========================
        // RECORTAR
        // =========================

        /*IConversion conversion = FFmpeg.Conversions.New();

        conversion.AddParameter($"-ss {startTime}", ParameterPosition.PreInput);
        conversion.AddParameter($"-t {finalDuration}");
        conversion.AddParameter($"-i \"{videoPath}\"");
        conversion.SetOutput(outputPath);

        Console.WriteLine($"✂️ Recortando video...");

        await conversion.Start();*/

        IConversion conversion =
        await FFmpeg.Conversions.FromSnippet.Split(
            videoPath,
            outputPath,
            startTime,
            finalDuration);

        await conversion.Start();

        Console.WriteLine($"✅ Video recortado guardado en: {outputPath}");

        return outputPath;
    }
}