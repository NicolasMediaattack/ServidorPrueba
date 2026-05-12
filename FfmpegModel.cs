using System.Diagnostics;

public class FfmpegModel
{
    private readonly string videoPath;
    private readonly float minDuration;
    private readonly float maxDuration;

    public FfmpegModel(
        string videoPath,
        float minDuration,
        float maxDuration)
    {
        this.videoPath = videoPath;
        this.minDuration = minDuration;
        this.maxDuration = maxDuration;
    }

    public async Task<string> TrimVideo()
    {
        // =========================
        // DURACIÓN VIDEO
        // =========================

        double totalSeconds =
            await GetVideoDuration();

        double outputDuration =
            totalSeconds
            - minDuration
            - maxDuration;

        if (outputDuration <= 0)
        {
            throw new Exception(
                "Duración inválida");
        }

        // =========================
        // OUTPUT
        // =========================

        string outputPath =
            Path.Combine(
                "/tmp",
                $"trimmed_{Guid.NewGuid()}.mp4");

        // =========================
        // ARGUMENTOS
        // =========================

        string arguments =
            $"-ss {minDuration} " +
            $"-i \"{videoPath}\" " +
            $"-t {outputDuration} " +
            $"-c copy " +
            $"\"{outputPath}\" -y";

        // =========================
        // PROCESO
        // =========================

        Process process =
            new Process();

        process.StartInfo.FileName =
            "ffmpeg";

        process.StartInfo.Arguments =
            arguments;

        process.StartInfo.RedirectStandardError =
            true;

        process.StartInfo.UseShellExecute =
            false;

        process.Start();

        string output =
            await process.StandardError
                .ReadToEndAsync();

        await process.WaitForExitAsync();

        Console.WriteLine(output);

        if (process.ExitCode != 0)
        {
            throw new Exception(output);
        }

        ShowTrimmedVideos();

        return outputPath;
    }

    private async Task<double> GetVideoDuration()
    {
        Process process =
            new Process();

        process.StartInfo.FileName =
            "ffprobe";

        process.StartInfo.Arguments =
            $"-v error -show_entries format=duration " +
            $"-of default=noprint_wrappers=1:nokey=1 " +
            $"\"{videoPath}\"";

        process.StartInfo.RedirectStandardOutput =
            true;

        process.StartInfo.UseShellExecute =
            false;

        process.Start();

        string output =
            await process.StandardOutput
                .ReadToEndAsync();

        await process.WaitForExitAsync();

        return double.Parse(
            output,
            System.Globalization.CultureInfo.InvariantCulture);
    }

    public void ShowTrimmedVideos ()
    {
        string[] trimmedVideos = Directory.GetFiles("/tmp", "trimmed_*.mp4");
    
            Console.WriteLine("Videos recortados:");
    
            foreach (string video in trimmedVideos)
            {
                Console.WriteLine(video);
            }
    }
}