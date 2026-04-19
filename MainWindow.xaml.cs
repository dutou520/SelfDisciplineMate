using System.Windows;
using System.Windows.Input;
using System;
using System.Windows.Controls;
using SelfDisciplineMate.ViewModels;
using SelfDisciplineMate.Models;

namespace SelfDisciplineMate
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            // 最小化到托盘而不是关闭
            if (DataContext is MainViewModel vm && vm is not null)
            {
                // 检查是否设置了最小化到托盘
                e.Cancel = true;
                this.Hide();
            }
            else
            {
                base.OnClosing(e);
            }
        }

        private void OpenSettings_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow();
            settingsWindow.DataContext = App.Services.GetService(typeof(SettingsViewModel));
            settingsWindow.ShowDialog();
        }

        private void OpenCalendar_Click(object sender, RoutedEventArgs e)
        {
            ((App)Application.Current).OpenCalendar();
        }

        private Point _startPoint;

        private void Task_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _startPoint = e.GetPosition(null);
        }

        private void Task_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Point mousePos = e.GetPosition(null);
                Vector diff = _startPoint - mousePos;

                if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    if (sender is Border border && border.DataContext is TaskItem task)
                    {
                        DragDrop.DoDragDrop(border, task, DragDropEffects.Move);
                    }
                }
            }
        }

        private void Task_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(TaskItem)))
            {
                var sourceTask = (TaskItem)e.Data.GetData(typeof(TaskItem));
                if (sender is Border border && border.DataContext is TaskItem targetTask)
                {
                    if (sourceTask != targetTask && DataContext is MainViewModel vm)
                    {
                        int sourceIndex = vm.TodayTasks.IndexOf(sourceTask);
                        int targetIndex = vm.TodayTasks.IndexOf(targetTask);
                        if (sourceIndex >= 0 && targetIndex >= 0)
                        {
                            vm.MoveTask(sourceIndex, targetIndex);
                        }
                    }
                }
            }
        }
    }
}
