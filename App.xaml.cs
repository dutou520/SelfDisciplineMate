using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using SelfDisciplineMate.Services;
using SelfDisciplineMate.ViewModels;
using H.NotifyIcon;
using System.Drawing;
using H.NotifyIcon.Core;

namespace SelfDisciplineMate
{
    public partial class App : Application
    {
        private IServiceProvider? _serviceProvider;
        private TaskbarIcon? _notifyIcon;
        private MainWindow? _mainWindow;

        public static IServiceProvider Services { get; private set; } = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Configure DI
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();
            Services = _serviceProvider;

            // Create tray icon
            CreateTrayIcon();

            // Create and show main window
            _mainWindow = new MainWindow();
            _mainWindow.DataContext = _serviceProvider.GetRequiredService<MainViewModel>();
            _mainWindow.Show();

            // Handle shutdown
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            // Check daily reset
            var dailyResetService = _serviceProvider.GetRequiredService<DailyResetService>();
            dailyResetService.DayReset += (s, args) =>
            {
                Dispatcher.Invoke(() =>
                {
                    if (_mainWindow?.DataContext is MainViewModel vm)
                    {
                        vm.RefreshTasks();
                    }
                });
            };
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Services
            services.AddSingleton<DatabaseService>();
            services.AddSingleton<SettingsService>();
            services.AddSingleton<PomodoroService>();
            services.AddSingleton<EntertainmentPermissionService>();
            services.AddSingleton<ProcessBlockService>();
            services.AddSingleton<TrayIconService>();
            services.AddSingleton<DailyResetService>();

            // ViewModels
            services.AddSingleton<MainViewModel>();
            services.AddTransient<SettingsViewModel>();
            services.AddTransient<CalendarViewModel>();
        }

        private void CreateTrayIcon()
        {
            try
            {
                // 创建番茄形状的图标
                var icon = CreateTomatoIcon();

                _notifyIcon = new TaskbarIcon
                {
                    ToolTipText = "🍅 自律桌面伴侣",
                    Icon = icon,
                    Visibility = Visibility.Visible
                };

                // Create context menu
                var contextMenu = new System.Windows.Controls.ContextMenu();

                var showItem = new System.Windows.Controls.MenuItem { Header = "显示主窗口" };
                showItem.Click += (s, e) => ShowMainWindow();
                contextMenu.Items.Add(showItem);

                var calendarItem = new System.Windows.Controls.MenuItem { Header = "月度统计" };
                calendarItem.Click += (s, e) => OpenCalendar();
                contextMenu.Items.Add(calendarItem);

                var settingsItem = new System.Windows.Controls.MenuItem { Header = "设置" };
                settingsItem.Click += (s, e) => OpenSettings();
                contextMenu.Items.Add(settingsItem);

                contextMenu.Items.Add(new System.Windows.Controls.Separator());

                var exitItem = new System.Windows.Controls.MenuItem { Header = "退出" };
                exitItem.Click += (s, e) => ExitApplication();
                contextMenu.Items.Add(exitItem);

                _notifyIcon.ContextMenu = contextMenu;
                _notifyIcon.TrayMouseDoubleClick += (s, e) => ShowMainWindow();

                // 强制创建托盘图标，使其显示在任务栏
                _notifyIcon.ForceCreate();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to create tray icon: {ex.Message}");
            }
        }

        private System.Drawing.Icon CreateTomatoIcon()
        {
            // 创建一个简单的番茄图标
            using var bitmap = new Bitmap(32, 32);
            using var graphics = Graphics.FromImage(bitmap);

            // 透明背景
            graphics.Clear(Color.Transparent);

            // 画番茄圆形
            using var redBrush = new SolidBrush(Color.FromArgb(220, 61, 69));
            graphics.FillEllipse(redBrush, 4, 6, 24, 24);

            // 画番茄顶部绿色
            using var greenBrush = new SolidBrush(Color.FromArgb(76, 175, 80));
            graphics.FillEllipse(greenBrush, 13, 2, 6, 8);

            // 画高光
            graphics.FillEllipse(Brushes.White, 8, 10, 6, 6);

            // 转换为图标
            var iconHandle = bitmap.GetHicon();
            return System.Drawing.Icon.FromHandle(iconHandle);
        }

        public void ShowNotification(string title, string message, NotificationIcon icon = NotificationIcon.Info)
        {
            try
            {
                // 使用 PopupNotification 方式
                _notifyIcon?.ShowNotification(title, message, icon, null);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to show notification: {ex.Message}");
            }
        }

        private void ShowMainWindow()
        {
            if (_mainWindow == null)
            {
                _mainWindow = new MainWindow();
                _mainWindow.DataContext = _serviceProvider?.GetRequiredService<MainViewModel>();
            }

            _mainWindow.Show();
            _mainWindow.WindowState = WindowState.Normal;
            _mainWindow.Activate();
        }

        private void OpenSettings()
        {
            var settingsWindow = new SettingsWindow();
            settingsWindow.DataContext = _serviceProvider?.GetRequiredService<SettingsViewModel>();
            settingsWindow.ShowDialog();
        }

        public void OpenCalendar()
        {
            var calendarWindow = new CalendarWindow(_serviceProvider!.GetRequiredService<CalendarViewModel>());
            calendarWindow.ShowDialog();
        }

        private void ExitApplication()
        {
            _notifyIcon?.Dispose();
            _mainWindow?.Close();
            Shutdown();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _notifyIcon?.Dispose();
            base.OnExit(e);
        }
    }
}
