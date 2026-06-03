using System.Diagnostics;
using System.Globalization;
using System.Collections.Concurrent;

public class FfmpegModel
{
    private readonly string videoPath;
    private readonly string trimsPath;
    private readonly float startTime;
    private readonly float endTime;

    public FfmpegModel(
        string videoPath,
        string trimsPath,
        float minDuration,
        float maxDuration)
    {
        this.videoPath = videoPath;
        this.trimsPath = trimsPath; 
        this.startTime = minDuration;
        this.endTime = maxDuration;
    }

    public async Task<(string videoPath, string thumbnailPath)> TrimVideo()
    {

        double totalSeconds =
            await GetVideoDuration();

        double outputDuration =
        endTime - startTime;

        Console.WriteLine($"TOTAL: {totalSeconds}");
        Console.WriteLine($"START CUT: {startTime}");
        Console.WriteLine($"END CUT: {endTime}");
        Console.WriteLine($"FINAL DURATION: {outputDuration}");

        if (outputDuration <= 0)
        {
            throw new Exception(
                "Duración inválida");
        }

        int trimCount =
            Directory.GetFiles(
                trimsPath,
                "trim_*.mp4")
            .Length + 1;

        string outputPath =
            Path.Combine(
                trimsPath,
                $"trim_{trimCount}.mp4");

        string arguments =
            $"-ss {startTime.ToString(CultureInfo.InvariantCulture)} " +
            $"-i \"{videoPath}\" " +
            $"-t {outputDuration.ToString(CultureInfo.InvariantCulture)} " +
            $"-vf scale=1280:-2 " +
            $"-r 30 " +
            $"-c:v libx264 " +
            $"-preset ultrafast " +
            $"-crf 28 " +
            $"-c:a aac " +
            $"-b:a 128k " +
            $"-movflags +faststart " +
            $"-y " +

            $"\"{outputPath}\"";

        Console.WriteLine(arguments);

        await RunFfmpeg(arguments);

        string thumbnailPath = await GenerateThumbnail(outputPath, trimsPath);

        return (outputPath, thumbnailPath);
    }

    public async Task<string> GenerateThumbnail(string videoPath, string trimsPath)
    {
        string thumbnailPath = Path.ChangeExtension(videoPath, ".jpg");

        string arguments =
            $"-ss 00:00:01 " +          // Frame en el segundo 1
            $"-i \"{videoPath}\" " +
            $"-frames:v 1 " +
            $"-vf scale=640:-2 " +
            $"-q:v 2 " +
            $"-y " +
            $"\"{thumbnailPath}\"";

        await RunFfmpeg(arguments);

        // Mostrar por consola
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("\n📸 THUMBNAIL GENERADO");
        Console.WriteLine($"   🖼️  {Path.GetFileName(thumbnailPath)}");
        Console.WriteLine($"   📦 {(new FileInfo(thumbnailPath).Length / 1024f):F2} KB");
        Console.ResetColor();

        return thumbnailPath;
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

    public static void ShowTrimVideos(
        string trimsPath)
    {
        Console.ForegroundColor =
            ConsoleColor.Cyan;

        Console.WriteLine(
            "\n==============================");

        Console.WriteLine(
            "🎬 TRIMS GUARDADOS");

        Console.WriteLine(
            "==============================");

        Console.ResetColor();

        string[] files =
            Directory.GetFiles(
                trimsPath,
                "*.mp4");

        if (files.Length == 0)
        {
            Console.WriteLine(
                "No hay trims");

            return;
        }

        Array.Sort(files);

        foreach (string file in files)
        {
            FileInfo info =
                new FileInfo(file);

            Console.WriteLine(
                $"🎞️ {info.Name}");

            Console.WriteLine(
                $"   📦 {(info.Length / 1024f / 1024f):F2} MB");

            Console.WriteLine(
                $"   🕒 {info.CreationTime}");

            Console.WriteLine();
        }
    }

    public async Task<string> ConcatVideos(ConcurrentDictionary<string, string> trimsDictionary)
    {
        if (trimsDictionary.Count < 2)
        {
            throw new Exception(
                "Se necesitan al menos 2 vídeos");
        }

        string listFile =
            Path.Combine(
                trimsPath,
                "concat_list.txt");

        var orderedVideos =
            trimsDictionary
                .OrderBy(x =>
                    int.Parse(
                        x.Key.Replace("trim", "")))
                .Select(x =>
                    $"file '{x.Value}'");

        await File.WriteAllLinesAsync(
            listFile,
            orderedVideos);

        string outputPath =
            Path.Combine(
                trimsPath,
                $"merged_{DateTime.Now:yyyyMMdd_HHmmss}.mp4");

        string arguments =
            $"-f concat " +
            $"-safe 0 " +
            $"-i \"{listFile}\" " +
            $"-c copy " +
            $"-y " +
            $"\"{outputPath}\"";

        await RunFfmpeg(arguments);

        Console.ForegroundColor =
            ConsoleColor.Green;

        Console.WriteLine(
            $"✅ Vídeo concatenado: {outputPath}");

        Console.ResetColor();

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