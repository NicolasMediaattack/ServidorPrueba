using System.Diagnostics;
using System.Globalization;

public class FfmpegModel
{
    private readonly string videoPath;
    private readonly float startTime;
    private readonly float endTime;

    public FfmpegModel(
        string videoPath,
        float startTime,
        float endTime)
    {
        this.videoPath = videoPath;
        this.startTime = startTime;
        this.endTime = endTime;
    }

    public async Task<string> TrimVideo()
    {
        double totalSeconds =
            await GetVideoDuration();

        double outputDuration =
            endTime - startTime;

        Console.WriteLine($"TOTAL: {totalSeconds}");
        Console.WriteLine($"START: {startTime}");
        Console.WriteLine($"END: {endTime}");
        Console.WriteLine($"DURATION: {outputDuration}");

        if (outputDuration <= 0)
        {
            throw new Exception(
                "Duración inválida");
        }

        string outputPath =
            Path.Combine(
                "/tmp",
                $"trimmed_{Guid.NewGuid()}.mp4");

        string arguments =
            $"-ss {startTime.ToString(CultureInfo.InvariantCulture)} " +
            $"-i \"{videoPath}\" " +
            $"-t {outputDuration.ToString(CultureInfo.InvariantCulture)} " +

            // NORMALIZACIÓN
            $"-vf scale=1280:-2 " +
            $"-r 30 " +

            // VIDEO
            $"-c:v libx264 " +
            $"-preset ultrafast " +
            $"-crf 28 " +

            // AUDIO
            $"-c:a aac " +
            $"-b:a 128k " +

            // WEB
            $"-movflags +faststart " +

            // OVERWRITE
            $"-y " +

            $"\"{outputPath}\"";

        Console.WriteLine(arguments);

        await RunFfmpeg(arguments);

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
            CultureInfo.InvariantCulture);
    }

    private async Task RunFfmpeg(string arguments)
    {
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
    }
}