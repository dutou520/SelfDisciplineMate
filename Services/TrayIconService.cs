using System;
using System.Windows;
using Microsoft.Win32;

namespace SelfDisciplineMate.Services
{
    public class TrayIconService
    {
        private readonly SettingsService _settingsService;

        public TrayIconService(SettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        public void SetStartup(bool enable)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);

                if (key == null) return;

                if (enable)
                {
                    var exePath = System.IO.Path.Combine(AppContext.BaseDirectory, "SelfDisciplineMate.exe");
                    key.SetValue("SelfDisciplineMate", $"\"{exePath}\"");
                }
                else
                {
                    key.DeleteValue("SelfDisciplineMate", false);
                }

                _settingsService.Settings.StartWithWindows = enable;
                _settingsService.SaveSettings();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to set startup: {ex.Message}");
            }
        }

        public bool IsStartupEnabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false);

                return key?.GetValue("SelfDisciplineMate") != null;
            }
            catch
            {
                return false;
            }
        }
    }
}
