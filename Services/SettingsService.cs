using System;
using System.IO;
using System.Text.Json;
using SelfDisciplineMate.Models;

namespace SelfDisciplineMate.Services
{
    public class SettingsService
    {
        private readonly string _settingsPath;
        private AppSettings _settings;

        public AppSettings Settings => _settings;

        public SettingsService()
        {
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SelfDisciplineMate");

            Directory.CreateDirectory(appDataPath);
            _settingsPath = Path.Combine(appDataPath, "settings.json");

            _settings = LoadSettings();
        }

        private AppSettings LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsPath))
                {
                    var json = File.ReadAllText(_settingsPath);
                    return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load settings: {ex.Message}");
            }

            return new AppSettings();
        }

        public void SaveSettings()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(_settings, options);
                File.WriteAllText(_settingsPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save settings: {ex.Message}");
            }
        }

        public void UpdateAppPath(string exeName, string exePath, EntertainmentLevel level)
        {
            _settings.AppPathMap[exeName] = new AppPathMapping
            {
                AppName = GetDisplayName(exeName),
                ExePath = exePath,
                RequiredLevel = level
            };
            SaveSettings();
        }

        public void RemoveAppPath(string exeName)
        {
            if (_settings.AppPathMap.ContainsKey(exeName))
            {
                _settings.AppPathMap.Remove(exeName);
                SaveSettings();
            }
        }

        private string GetDisplayName(string exeName)
        {
            return exeName.ToLower() switch
            {
                "overwatch.exe" => "守望先锋",
                "minecraft.exe" => "我的世界",
                "steam.exe" => "Steam",
                "godot.exe" => "Godot",
                "ue4editor.exe" or "ue5editor.exe" => "UE引擎",
                "blender.exe" => "Blender",
                _ => exeName.Replace(".exe", "")
            };
        }
    }
}
