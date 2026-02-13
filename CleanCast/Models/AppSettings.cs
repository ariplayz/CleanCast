using System;
using System.IO;
using System.Text.Json;

namespace CleanCast.Models;

public class AppSettings
{
    public bool UseHardwareDecoding { get; set; } = true;


    private static string SettingsPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CleanCast", "settings.json");

    public static AppSettings Load()
    {
        try
        {
            var file = SettingsPath;
            if (!File.Exists(file)) return new AppSettings();

            var json = File.ReadAllText(file);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(SettingsPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir!);

            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }
        catch
        {
            // ignore errors
        }
    }
}
