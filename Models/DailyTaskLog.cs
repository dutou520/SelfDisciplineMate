using System;

namespace SelfDisciplineMate.Models
{
    public class DailyTaskLog
    {
        public int Id { get; set; }
        public int TaskId { get; set; }
        public string LogDate { get; set; } = string.Empty; // yyyy-MM-dd
        public bool IsCompleted { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
