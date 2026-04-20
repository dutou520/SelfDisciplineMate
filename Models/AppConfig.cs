using System;
using System.Collections.Generic;

namespace SelfDisciplineMate.Models
{
    public class AppSettings
    {
        public Dictionary<string, AppPathMapping> AppPathMap { get; set; } = new();
        public int FocusDurationMinutes { get; set; } = 25;
        public int ShortBreakMinutes { get; set; } = 5;
        public int LongBreakMinutes { get; set; } = 15;
        public int PomodorosUntilLongBreak { get; set; } = 4;
        public bool StartWithWindows { get; set; } = false;
        public bool MinimizeToTray { get; set; } = true;
        public bool PlaySound { get; set; } = true;
        public DateTime LastDailyReset { get; set; } = DateTime.MinValue;
        public double WindowTop { get; set; } = -1;
        public double WindowLeft { get; set; } = -1;
        public double WindowWidth { get; set; } = 450;
        public double WindowHeight { get; set; } = 700;
    }

    public class AppPathMapping
    {
        public string AppName { get; set; } = string.Empty;
        public string ExePath { get; set; } = string.Empty;
        public EntertainmentLevel RequiredLevel { get; set; } = EntertainmentLevel.Overwatch;
    }

    public enum EntertainmentLevel
    {
        None = 0,           // 0%
        Blender = 1,        // >20% 或 今日100%时
        UE_Engine = 2,      // >40%
        Godot = 3,          // >60%
        Steam = 4,          // >70%
        Minecraft = 5,      // >80%
        Overwatch = 6       // >90%
    }
}
