using System;

namespace SelfDisciplineMate.Models
{
    public class TaskTemplate
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int Weight { get; set; } = 1;
        public bool IsDeleted { get; set; } = false;
        public int SortOrder { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
