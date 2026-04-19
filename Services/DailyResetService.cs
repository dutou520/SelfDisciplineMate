using System;
using System.Timers;

namespace SelfDisciplineMate.Services
{
    public class DailyResetService : IDisposable
    {
        private readonly DatabaseService _databaseService;
        private readonly SettingsService _settingsService;
        private readonly PomodoroService _pomodoroService;
        private Timer? _midnightTimer;

        public event EventHandler? DayReset;

        public DailyResetService(
            DatabaseService databaseService,
            SettingsService settingsService,
            PomodoroService pomodoroService)
        {
            _databaseService = databaseService;
            _settingsService = settingsService;
            _pomodoroService = pomodoroService;

            CheckAndResetIfNeeded();
            ScheduleMidnightReset();
        }

        public void CheckAndResetIfNeeded()
        {
            var lastReset = _settingsService.Settings.LastDailyReset;
            var today = DateTime.Now.Date;

            if (lastReset.Date < today)
            {
                PerformDailyReset(today);
            }
        }

        private void PerformDailyReset(DateTime newDate)
        {
            // 1. 确保今天的日志存在
            _databaseService.EnsureTodayLogsExist();

            // 2. 重置番茄钟计数
            _pomodoroService.ResetDaily();

            // 3. 更新最后重置日期
            _settingsService.Settings.LastDailyReset = newDate;
            _settingsService.SaveSettings();

            DayReset?.Invoke(this, EventArgs.Empty);
        }

        private void ScheduleMidnightReset()
        {
            _midnightTimer?.Dispose();

            var now = DateTime.Now;
            var nextMidnight = now.Date.AddDays(1);
            var timeUntilMidnight = (nextMidnight - now).TotalMilliseconds;

            _midnightTimer = new Timer(timeUntilMidnight);
            _midnightTimer.Elapsed += OnMidnightElapsed;
            _midnightTimer.AutoReset = false;
            _midnightTimer.Start();
        }

        private void OnMidnightElapsed(object? sender, ElapsedEventArgs e)
        {
            // 在UI线程执行
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                PerformDailyReset(DateTime.Now.Date);
                ScheduleMidnightReset();
            });
        }

        public void Dispose()
        {
            _midnightTimer?.Stop();
            _midnightTimer?.Dispose();
        }
    }
}
