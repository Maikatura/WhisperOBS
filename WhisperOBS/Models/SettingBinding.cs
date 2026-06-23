namespace WhisperOBS.Models;

public class SettingBinding
{
    public string Key;
    public CheckBox Control;
    public SettingBinding(string key, CheckBox control) { Key = key; Control = control; }
}