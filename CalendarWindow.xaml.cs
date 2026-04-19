using System.Windows;
using SelfDisciplineMate.ViewModels;

namespace SelfDisciplineMate
{
    public partial class CalendarWindow : Window
    {
        public CalendarWindow(CalendarViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
