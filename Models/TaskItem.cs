using CommunityToolkit.Mvvm.ComponentModel;

namespace SelfDisciplineMate.Models
{
    public partial class TaskItem : ObservableObject
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;

        [ObservableProperty]
        private int _weight = 1;

        [ObservableProperty]
        private bool _isCompleted = false;

        [ObservableProperty]
        private int _sortOrder = 0;

        public int LogId { get; set; }
        public string LogDate { get; set; } = string.Empty;
    }
}
