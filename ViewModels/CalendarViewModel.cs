using System;
using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SelfDisciplineMate.Services;

namespace SelfDisciplineMate.ViewModels
{
    public partial class CalendarViewModel : ObservableObject
    {
        private readonly DatabaseService _databaseService;

        [ObservableProperty]
        private DateTime _currentMonth;

        [ObservableProperty]
        private ObservableCollection<DayCell> _days = new();

        [ObservableProperty]
        private DayCell? _selectedDay;

        public string MonthYearDisplay => CurrentMonth.ToString("yyyy年 M月");

        public CalendarViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService;
            _currentMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            LoadMonthData();
        }

        partial void OnCurrentMonthChanged(DateTime value)
        {
            OnPropertyChanged(nameof(MonthYearDisplay));
            LoadMonthData();
        }

        [RelayCommand]
        private void PreviousMonth()
        {
            CurrentMonth = CurrentMonth.AddMonths(-1);
        }

        [RelayCommand]
        private void NextMonth()
        {
            CurrentMonth = CurrentMonth.AddMonths(1);
        }

        [RelayCommand]
        private void SelectDay(DayCell? day)
        {
            if (day != null)
            {
                foreach (var d in Days)
                {
                    d.IsSelected = false;
                }
                day.IsSelected = true;
                SelectedDay = day;
            }
        }

        private void LoadMonthData()
        {
            Days.Clear();

            var statistics = _databaseService.GetMonthStatistics(CurrentMonth.Year, CurrentMonth.Month);

            // Get the first day of the month
            var firstDayOfMonth = new DateTime(CurrentMonth.Year, CurrentMonth.Month, 1);
            var startDayOfWeek = (int)firstDayOfMonth.DayOfWeek;

            // Add empty cells for days before the first day of month
            for (int i = 0; i < startDayOfWeek; i++)
            {
                Days.Add(new DayCell { IsPlaceholder = true });
            }

            // Add days of the month
            foreach (var stat in statistics)
            {
                var date = DateTime.Parse(stat.Date);
                var dayCell = new DayCell
                {
                    Day = date.Day,
                    Date = stat.Date,
                    CompletionRate = stat.CompletionRate,
                    PomodoroCount = stat.PomodoroCount,
                    IsToday = stat.Date == DateTime.Now.ToString("yyyy-MM-dd"),
                    IsPlaceholder = false
                };
                Days.Add(dayCell);
            }

            OnPropertyChanged(nameof(MonthYearDisplay));
        }
    }

    public partial class DayCell : ObservableObject
    {
        [ObservableProperty]
        private int _day;

        [ObservableProperty]
        private string _date = string.Empty;

        [ObservableProperty]
        private double _completionRate;

        [ObservableProperty]
        private int _pomodoroCount;

        [ObservableProperty]
        private bool _isToday;

        [ObservableProperty]
        private bool _isSelected;

        [ObservableProperty]
        private bool _isPlaceholder;

        public string CompletionColor
        {
            get
            {
                if (CompletionRate >= 100) return "#4CAF50";  // Green
                if (CompletionRate >= 80) return "#8BC34A";   // Light Green
                if (CompletionRate >= 60) return "#FFC107";  // Yellow
                if (CompletionRate >= 40) return "#FF9800";  // Orange
                if (CompletionRate >= 20) return "#FF5722";  // Deep Orange
                if (CompletionRate > 0) return "#F44336";     // Red
                return "#E0E0E0";                             // Gray
            }
        }

        public string ToolTip => $"{Date}\n进度: {CompletionRate:F0}%\n番茄: {PomodoroCount} 个";
    }
}
