using System;

namespace SelfDisciplineMate.Models
{
    public class PomodoroLog
    {
        public int Id { get; set; }
        public DateTime StartTime { get; set; }
        public int DurationMinutes { get; set; }
        public PomodoroType Type { get; set; }
    }

    public enum PomodoroType
    {
        Focus = 0,      // 专注
        ShortBreak = 1,  // 短休息
        LongBreak = 2   // 长休息
    }
}
