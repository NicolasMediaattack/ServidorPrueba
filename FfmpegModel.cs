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

    // MÉTODO PARA RECORTAR EL VIDEO
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

        return outputPath;
    }

    // MÉTODO PARA OBTENER LA DURACIÓN DEL VIDEO

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

    // MÉTODO PARA MOSTRAR LOS VIDEOS TEMPORALES EN /tmp

    public static void ShowTemporaryVideos()
    {
        Console.ForegroundColor =
            ConsoleColor.Cyan;

        Console.WriteLine(
            "\n==============================");

        Console.WriteLine(
            "📂 VIDEOS TEMPORALES EN /tmp");

        Console.WriteLine(
            "==============================");

        Console.ResetColor();

        string[] files =
            Directory.GetFiles(
                "/tmp",
                "*.mp4");

        if (files.Length == 0)
        {
            Console.WriteLine(
                "No hay videos temporales");

            return;
        }

        foreach (string file in files)
        {
            FileInfo info =
                new FileInfo(file);

            Console.WriteLine(
                $"🎬 {info.Name}");

            Console.WriteLine(
                $"   📦 {(info.Length / 1024f / 1024f):F2} MB");

            Console.WriteLine(
                $"   🕒 {info.CreationTime}");

            Console.WriteLine(
                $"   📍 {info.FullName}");

            Console.WriteLine();
        }
    }

    public async Task<string> NormalizeVideo(string inputVideo)
    {
        string outputPath =
            Path.Combine(
                "/tmp",
                $"normalized_{Guid.NewGuid()}.mp4");

        string arguments =
            $"-i \"{inputVideo}\" " +
            $"-vf scale=1280:720,fps=30 " +
            $"-c:v libx264 " +
            $"-preset veryfast " +
            $"-c:a aac " +
            $"-y " +
            $"\"{outputPath}\"";

        await RunFfmpeg(arguments);

        return outputPath;
    }

    public async Task<string> ConcatVideos(string firstVideo, string secondVideo)
    {
        string listPath =
            Path.Combine(
                "/tmp",
                $"list_{Guid.NewGuid()}.txt");

        await File.WriteAllTextAsync(
            listPath,
            $"file '{firstVideo}'\n" +
            $"file '{secondVideo}'");

        string outputPath =
            Path.Combine(
                "/tmp",
                $"concat_{Guid.NewGuid()}.mp4");

        string arguments =
            $"-f concat " +
            $"-safe 0 " +
            $"-i \"{listPath}\" " +
            $"-c copy " +
            $"-y " +
            $"\"{outputPath}\"";

        await RunFfmpeg(arguments);

        File.Delete(listPath);

        return outputPath;
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