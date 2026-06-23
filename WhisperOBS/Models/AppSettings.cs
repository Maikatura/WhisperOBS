using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace WhisperOBS.Models;

public sealed class AppSettings
{
    private static readonly string FilePath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
    private static readonly object FileLock = new();
    
    private Dictionary<string, string> _registry = new(StringComparer.OrdinalIgnoreCase);

    public static AppSettings Instance { get; private set; } = Load();
    
    public void Set(string key, string value)
    {
        lock (FileLock)
        {
            _registry[key] = value;
        }
    }
    
    public void Set(string key, int value)
    {
        Set(key, value.ToString());
    }
    
    public void Set(string key, bool value)
    {
        Set(key, value.ToString());
    }


    public string Get(string key, string defaultValue = "")
    {
        lock (FileLock)
        {
            return _registry.TryGetValue(key, out var val) ? val : defaultValue;
        }
    }
    
    public int GetInt(string key, int defaultValue = 0) => 
        int.TryParse(Get(key), out int result) ? result : defaultValue;

    public bool GetBool(string key, bool defaultValue = false) => 
        bool.TryParse(Get(key), out bool result) ? result : defaultValue;
    
    public static AppSettings Load()
    {
        lock (FileLock)
        {
            var settings = new AppSettings();
            if (!File.Exists(FilePath)) return settings;

            try
            {
                string json = File.ReadAllText(FilePath);
                settings._registry = JsonSerializer.Deserialize<Dictionary<string, string>>(json) 
                                     ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
            catch
            {
                settings._registry = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
            return settings;
        }
    }

    public void Save()
    {
        lock (FileLock)
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(_registry, options);
                File.WriteAllText(FilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Config serialization fault: {ex.Message}");
            }
        }
    }
}