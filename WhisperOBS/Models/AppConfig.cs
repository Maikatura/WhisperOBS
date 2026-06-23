namespace WhisperOBS.Models;

public class AppConfig
{
    public string ModelPath { get; set; } = string.Empty;
    public string Language  { get; set; } = "auto";
    public int    DeviceIndex { get; set; } = 0;
    public int    Port        { get; set; } = 5000;
}