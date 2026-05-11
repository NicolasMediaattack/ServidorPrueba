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

    void trimVideo()
    {
        // Aquí iría la lógica para recortar el video usando FFmpeg
    }

    void concatVideos()
    {
        // Aquí iría la lógica para concatenar videos usando FFmpeg
    }
}