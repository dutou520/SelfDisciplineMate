using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using SelfDisciplineMate.Models;
using SelfDisciplineMate.Services;

namespace SelfDisciplineMate.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly SettingsService _settingsService;
        private readonly ProcessBlockService _processBlockService;
        private readonly TrayIconService _trayIconService;

        [ObservableProperty]
        private ObservableCollection<AppPathMapping> _appMappings = new();

        [ObservableProperty]
        private int _focusDuration = 25;

        [ObservableProperty]
        private int _shortBreakDuration = 5;

        [ObservableProperty]
        private int _longBreakDuration = 15;

        [ObservableProperty]
        private int _pomodorosUntilLongBreak = 4;

        [ObservableProperty]
        private bool _startWithWindows;

        [ObservableProperty]
        private bool _minimizeToTray = true;

        [ObservableProperty]
        private bool _playSound = true;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private bool _requiresAdmin;

        public Dictionary<EntertainmentLevel, string> LevelOptions { get; } = new Dictionary<EntertainmentLevel, string>
        {
            { EntertainmentLevel.Overwatch, "守望先锋级" },
            { EntertainmentLevel.Minecraft, "Minecraft级" },
            { EntertainmentLevel.Steam, "Steam级" },
            { EntertainmentLevel.Godot, "Godot级" },
            { EntertainmentLevel.UE_Engine, "UE引擎级" },
            { EntertainmentLevel.Blender, "Blender级" }
        };

        public SettingsViewModel(
            SettingsService settingsService,
            ProcessBlockService processBlockService,
            TrayIconService trayIconService)
        {
            _settingsService = settingsService;
            _processBlockService = processBlockService;
            _trayIconService = trayIconService;

            LoadSettings();
        }

        private void LoadSettings()
        {
            var settings = _settingsService.Settings;

            FocusDuration = settings.FocusDurationMinutes;
            ShortBreakDuration = settings.ShortBreakMinutes;
            LongBreakDuration = settings.LongBreakMinutes;
            PomodorosUntilLongBreak = settings.PomodorosUntilLongBreak;
            StartWithWindows = settings.StartWithWindows;
            MinimizeToTray = settings.MinimizeToTray;
            PlaySound = settings.PlaySound;

            AppMappings = new ObservableCollection<AppPathMapping>(settings.AppPathMap.Values);
            RequiresAdmin = _processBlockService.RequiresAdminRights();
        }

        [RelayCommand]
        private void AddAppMapping()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "可执行文件 (*.exe)|*.exe",
                Title = "选择要拦截的应用程序"
            };

            if (dialog.ShowDialog() == true)
            {
                var fileName = System.IO.Path.GetFileName(dialog.FileName);
                var mapping = new AppPathMapping
                {
                    AppName = fileName.Replace(".exe", ""),
                    ExePath = dialog.FileName,
                    RequiredLevel = EntertainmentLevel.Overwatch
                };

                AppMappings.Add(mapping);
                _settingsService.UpdateAppPath(fileName, dialog.FileName, EntertainmentLevel.Overwatch);
                StatusMessage = $"已添加: {mapping.AppName}";
            }
        }

        [RelayCommand]
        private void RemoveAppMapping(AppPathMapping mapping)
        {
            var exeName = mapping.AppName + ".exe";
            _settingsService.RemoveAppPath(exeName);
            AppMappings.Remove(mapping);
            StatusMessage = $"已移除: {mapping.AppName}";
        }

        [RelayCommand]
        private void UpdateRequiredLevel(AppPathMapping mapping)
        {
            var exeName = mapping.AppName + ".exe";
            _settingsService.UpdateAppPath(exeName, mapping.ExePath, mapping.RequiredLevel);
            StatusMessage = $"已更新: {mapping.AppName} 的权限要求";
        }

        [RelayCommand]
        private void ApplyBlockPolicy()
        {
            var (success, message) = _processBlockService.ApplyBlockPolicy();
            StatusMessage = message;
        }

        [RelayCommand]
        private void ClearBlockPolicy()
        {
            var (success, message) = _processBlockService.ClearAllBlocks();
            StatusMessage = message;
        }

        [RelayCommand]
        private void SaveSettings()
        {
            var settings = _settingsService.Settings;
            settings.FocusDurationMinutes = FocusDuration;
            settings.ShortBreakMinutes = ShortBreakDuration;
            settings.LongBreakMinutes = LongBreakDuration;
            settings.PomodorosUntilLongBreak = PomodorosUntilLongBreak;
            settings.StartWithWindows = StartWithWindows;
            settings.MinimizeToTray = MinimizeToTray;
            settings.PlaySound = PlaySound;

            _settingsService.SaveSettings();

            // 应用开机自启
            _trayIconService.SetStartup(StartWithWindows);

            StatusMessage = "设置已保存";
        }
    }
}
