using System;
using System.Windows;
using System.Windows.Threading;
using SelfDisciplineMate.Models;

namespace SelfDisciplineMate.Services
{
    public class PomodoroService
    {
        private readonly DatabaseService _databaseService;
        private readonly SettingsService _settingsService;
        private readonly DispatcherTimer _timer;

        private int _remainingSeconds;
        private int _completedPomodoros;
        private PomodoroState _currentState;
        private bool _isRunning;

        public event EventHandler<PomodoroState>? StateChanged;
        public event EventHandler<int>? TimerTick;
        public event EventHandler<PomodoroCompletedEventArgs>? PomodoroCompleted;
        public event EventHandler? CycleCompleted;

        public PomodoroState CurrentState => _currentState;
        public int RemainingSeconds => _remainingSeconds;
        public int CompletedPomodoros => _completedPomodoros;
        public bool IsRunning => _isRunning;

        public PomodoroService(DatabaseService databaseService, SettingsService settingsService)
        {
            _databaseService = databaseService;
            _settingsService = settingsService;

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += OnTimerTick;

            ResetToFocus();
        }

        private void OnTimerTick(object? sender, EventArgs e)
        {
            _remainingSeconds--;

            TimerTick?.Invoke(this, _remainingSeconds);

            if (_remainingSeconds <= 0)
            {
                CompleteCurrentPhase();
            }
        }

        private void CompleteCurrentPhase()
        {
            _timer.Stop();
            _isRunning = false;

            switch (_currentState)
            {
                case PomodoroState.Focus:
                    _completedPomodoros++;
                    _databaseService.AddPomodoroLog(
                        _settingsService.Settings.FocusDurationMinutes,
                        PomodoroType.Focus);

                    // 发送 Windows 通知
                    ShowWindowsNotification(
                        "🍅 番茄完成！",
                        $"太棒了！你已完成 {_completedPomodoros} 个番茄。休息一下吧~");

                    PomodoroCompleted?.Invoke(this, new PomodoroCompletedEventArgs
                    {
                        CompletedType = PomodoroType.Focus,
                        TotalPomodoros = _completedPomodoros
                    });

                    // Check if it's time for a long break
                    if (_completedPomodoros % _settingsService.Settings.PomodorosUntilLongBreak == 0)
                    {
                        _isRunning = true;
                        _timer.Start();
                        SwitchToState(PomodoroState.LongBreak);
                    }
                    else
                    {
                        _isRunning = true;
                        _timer.Start();
                        SwitchToState(PomodoroState.ShortBreak);
                    }
                    break;

                case PomodoroState.ShortBreak:
                    // 发送 Windows 通知
                    ShowWindowsNotification(
                        "☕ 短休息结束",
                        "休息好了吗？继续下一个番茄吧！");
                    SwitchToState(PomodoroState.Focus);
                    CycleCompleted?.Invoke(this, EventArgs.Empty);
                    break;

                case PomodoroState.LongBreak:
                    // 发送 Windows 通知
                    ShowWindowsNotification(
                        "🌟 长休息结束",
                        "充分休息后，继续加油！");
                    SwitchToState(PomodoroState.Focus);
                    CycleCompleted?.Invoke(this, EventArgs.Empty);
                    break;
            }
        }

        private void ShowWindowsNotification(string title, string message)
        {
            try
            {
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    var app = Application.Current as App;
                    app?.ShowNotification(title, message, H.NotifyIcon.Core.NotificationIcon.Info);
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to show notification: {ex.Message}");
            }
        }

        private void SwitchToState(PomodoroState newState)
        {
            _currentState = newState;
            switch (newState)
            {
                case PomodoroState.Focus:
                    _remainingSeconds = _settingsService.Settings.FocusDurationMinutes * 60;
                    break;
                case PomodoroState.ShortBreak:
                    _remainingSeconds = _settingsService.Settings.ShortBreakMinutes * 60;
                    _databaseService.AddPomodoroLog(
                        _settingsService.Settings.ShortBreakMinutes,
                        PomodoroType.ShortBreak);
                    break;
                case PomodoroState.LongBreak:
                    _remainingSeconds = _settingsService.Settings.LongBreakMinutes * 60;
                    _databaseService.AddPomodoroLog(
                        _settingsService.Settings.LongBreakMinutes,
                        PomodoroType.LongBreak);
                    break;
            }

            StateChanged?.Invoke(this, newState);
        }

        public void Start()
        {
            if (!_isRunning)
            {
                _isRunning = true;
                _timer.Start();
            }
        }

        public void Pause()
        {
            _isRunning = false;
            _timer.Stop();
        }

        public void Reset()
        {
            _timer.Stop();
            _isRunning = false;
            ResetToFocus();
        }

        public void Skip()
        {
            _timer.Stop();
            _isRunning = false;
            CompleteCurrentPhase();
        }

        private void ResetToFocus()
        {
            _currentState = PomodoroState.Focus;
            _remainingSeconds = _settingsService.Settings.FocusDurationMinutes * 60;
            _completedPomodoros = _databaseService.GetTodayPomodoroCount();
        }

        public void ResetDaily()
        {
            _completedPomodoros = 0;
            Reset();
        }
    }

    public enum PomodoroState
    {
        Focus,
        ShortBreak,
        LongBreak
    }

    public class PomodoroCompletedEventArgs : EventArgs
    {
        public PomodoroType CompletedType { get; set; }
        public int TotalPomodoros { get; set; }
    }
}
