using System;
using System.Windows;

namespace BlockNotifier
{
    public partial class BlockWindow : Window
    {
        public BlockWindow()
        {
            InitializeComponent();
            LoadArguments();
        }

        private void LoadArguments()
        {
            var args = Environment.GetCommandLineArgs();

            if (args.Length >= 2)
            {
                // 第一个参数是被拦截的应用名称
                var appName = args[1];
                AppNameText.Text = $"无法运行「{appName}」";

                // 第二个参数（可选）是剩余天数
                var daysOwed = args.Length >= 3 && int.TryParse(args[2], out var days) ? days : 1;
                MessageText.Text = $"昨日完成率不足，当前权限无法运行此应用。\n" +
                                   $"请完成更多任务以提升权限等级。\n" +
                                   $"加油！💪";
            }
            else
            {
                AppNameText.Text = "应用已被拦截";
                MessageText.Text = "当前权限不足，无法运行此应用。\n请完成更多任务以提升权限等级。";
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
