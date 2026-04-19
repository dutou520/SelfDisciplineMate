using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SelfDisciplineMate.Models;
using SelfDisciplineMate.Services;

namespace SelfDisciplineMate.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly DatabaseService _databaseService;
        private readonly SettingsService _settingsService;
        private readonly PomodoroService _pomodoroService;
        private readonly EntertainmentPermissionService _permissionService;
        private readonly ProcessBlockService _processBlockService;

        [ObservableProperty]
        private ObservableCollection<TaskItem> _todayTasks = new();

        [ObservableProperty]
        private int _remainingSeconds;

        [ObservableProperty]
        private PomodoroState _currentPomodoroState;

        [ObservableProperty]
        private bool _isRunning;

        [ObservableProperty]
        private int _completedPomodoros;

        [ObservableProperty]
        private double _todayCompletionRate;

        [ObservableProperty]
        private double _yesterdayCompletionRate;

        [ObservableProperty]
        private string _promptMessage = string.Empty;

        [ObservableProperty]
        private EntertainmentLevel _currentPermission;

        [ObservableProperty]
        private string _newTaskTitle = string.Empty;

        public MainViewModel(
            DatabaseService databaseService,
            SettingsService settingsService,
            PomodoroService pomodoroService,
            EntertainmentPermissionService permissionService,
            ProcessBlockService processBlockService)
        {
            _databaseService = databaseService;
            _settingsService = settingsService;
            _pomodoroService = pomodoroService;
            _permissionService = permissionService;
            _processBlockService = processBlockService;

            // Subscribe to events
            _pomodoroService.TimerTick += OnTimerTick;
            _pomodoroService.StateChanged += OnStateChanged;
            _pomodoroService.PomodoroCompleted += OnPomodoroCompleted;
            _permissionService.PermissionChanged += OnPermissionChanged;

            LoadData();
        }

        private void LoadData()
        {
            // 确保今日日志存在
            _databaseService.EnsureTodayLogsExist();

            // Load today's tasks
            var tasks = _databaseService.GetTodayTasks();
            TodayTasks = new ObservableCollection<TaskItem>(tasks);

            // Load initial state
            RemainingSeconds = _pomodoroService.RemainingSeconds;
            CurrentPomodoroState = _pomodoroService.CurrentState;
            IsRunning = _pomodoroService.IsRunning;
            CompletedPomodoros = _pomodoroService.CompletedPomodoros;

            _permissionService.RefreshPermission();
            UpdateCompletionRate();
            UpdatePromptMessage();
            UpdatePermission();
        }

        private void OnTimerTick(object? sender, int remainingSeconds)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                RemainingSeconds = remainingSeconds;
            });
        }

        private void OnStateChanged(object? sender, PomodoroState state)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                CurrentPomodoroState = state;
                RemainingSeconds = _pomodoroService.RemainingSeconds;
                IsRunning = _pomodoroService.IsRunning;

                // 刷新权限（任务完成可能解锁更多）
                UpdatePermission();
                _processBlockService.ApplyBlockPolicy();
            });
        }

        private void OnPomodoroCompleted(object? sender, PomodoroCompletedEventArgs e)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                CompletedPomodoros = e.TotalPomodoros;
                UpdateCompletionRate();
                UpdatePromptMessage();

                // 发送消息显示鼓励
                WeakReferenceMessenger.Default.Send(new PomodoroCompletedMessage(e));
            });
        }

        private void OnPermissionChanged(object? sender, EntertainmentLevel level)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                UpdatePermission();
                _processBlockService.ApplyBlockPolicy();
            });
        }

        [RelayCommand]
        private void Start()
        {
            _pomodoroService.Start();
            IsRunning = true;
        }

        [RelayCommand]
        private void Pause()
        {
            _pomodoroService.Pause();
            IsRunning = false;
        }

        [RelayCommand]
        private void Reset()
        {
            _pomodoroService.Reset();
            RemainingSeconds = _pomodoroService.RemainingSeconds;
            CurrentPomodoroState = _pomodoroService.CurrentState;
            IsRunning = false;
        }

        [RelayCommand]
        private void Skip()
        {
            _pomodoroService.Skip();
        }

        [RelayCommand]
        private void AddTask()
        {
            if (string.IsNullOrWhiteSpace(NewTaskTitle))
                return;

            var taskId = _databaseService.AddTask(NewTaskTitle.Trim(), 1);

            // 创建今日日志
            _databaseService.EnsureTodayLogsExist();

            // 重新加载任务
            var tasks = _databaseService.GetTodayTasks();
            TodayTasks = new ObservableCollection<TaskItem>(tasks);

            NewTaskTitle = string.Empty;
            UpdateCompletionRate();
        }

        [RelayCommand]
        private void ToggleTaskCompletion(TaskItem task)
        {
            if (task.LogId > 0)
            {
                // IsCompleted has already been toggled by the CheckBox binding
                _databaseService.UpdateTaskCompletion(task.LogId, task.IsCompleted);
            }
            else
            {
                // 如果 LogId 为 0，说明日志记录不存在，需要先创建
                _databaseService.EnsureTodayLogsExist();
                // 重新获取任务列表
                var tasks = _databaseService.GetTodayTasks();
                var updatedTask = tasks.FirstOrDefault(t => t.Id == task.Id);
                if (updatedTask != null && updatedTask.LogId > 0)
                {
                    _databaseService.UpdateTaskCompletion(updatedTask.LogId, task.IsCompleted);
                    task.LogId = updatedTask.LogId;
                }
            }

            // 刷新权限和进度
            _permissionService.RefreshPermission();
            UpdateCompletionRate();
            UpdatePromptMessage();
            _processBlockService.ApplyBlockPolicy();
        }

        [RelayCommand]
        private void IncreaseWeight(TaskItem task)
        {
            if (task.Weight < 5)
            {
                task.Weight++;
                var template = new TaskTemplate
                {
                    Id = task.Id,
                    Title = task.Title,
                    Weight = task.Weight
                };
                _databaseService.UpdateTask(template);
                UpdateCompletionRate();
            }
        }

        [RelayCommand]
        private void DecreaseWeight(TaskItem task)
        {
            if (task.Weight > 1)
            {
                task.Weight--;
                var template = new TaskTemplate
                {
                    Id = task.Id,
                    Title = task.Title,
                    Weight = task.Weight
                };
                _databaseService.UpdateTask(template);
                UpdateCompletionRate();
            }
        }

        [RelayCommand]
        private void DeleteTask(TaskItem task)
        {
            _databaseService.DeleteTask(task.Id);
            TodayTasks.Remove(task);
            UpdateCompletionRate();
            _permissionService.RefreshPermission();
            UpdatePromptMessage();
        }

        private void UpdateCompletionRate()
        {
            TodayCompletionRate = _permissionService.TodayCompletionRate;
            YesterdayCompletionRate = _permissionService.YesterdayCompletionRate;
        }

        private void UpdatePromptMessage()
        {
            PromptMessage = _permissionService.GetPromptMessage();
        }

        private void UpdatePermission()
        {
            CurrentPermission = _permissionService.CurrentPermission;
        }

        public void RefreshTasks()
        {
            _databaseService.EnsureTodayLogsExist();
            var tasks = _databaseService.GetTodayTasks();
            TodayTasks = new ObservableCollection<TaskItem>(tasks);
            UpdateCompletionRate();
        }

        public void MoveTask(int oldIndex, int newIndex)
        {
            if (oldIndex >= 0 && oldIndex < TodayTasks.Count && newIndex >= 0 && newIndex < TodayTasks.Count && oldIndex != newIndex)
            {
                TodayTasks.Move(oldIndex, newIndex);
                var orderedIds = TodayTasks.Select(t => t.Id).ToList();
                _databaseService.UpdateTaskOrders(orderedIds);
            }
        }
    }

    public class PomodoroCompletedMessage
    {
        public PomodoroCompletedEventArgs EventArgs { get; }

        public PomodoroCompletedMessage(PomodoroCompletedEventArgs eventArgs)
        {
            EventArgs = eventArgs;
        }
    }
}
