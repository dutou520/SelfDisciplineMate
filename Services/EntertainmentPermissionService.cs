using System;
using SelfDisciplineMate.Models;

namespace SelfDisciplineMate.Services
{
    public class EntertainmentPermissionService
    {
        private readonly DatabaseService _databaseService;

        public EntertainmentLevel CurrentPermission { get; private set; } = EntertainmentLevel.None;
        public double YesterdayCompletionRate { get; private set; }
        public double TodayCompletionRate { get; private set; }
        public bool IsTodayFullyCompleted { get; private set; }

        public event EventHandler<EntertainmentLevel>? PermissionChanged;

        public EntertainmentPermissionService(DatabaseService databaseService)
        {
            _databaseService = databaseService;
            RefreshPermission();
        }

        public void RefreshPermission()
        {
            YesterdayCompletionRate = _databaseService.GetYesterdayCompletionRate();
            TodayCompletionRate = _databaseService.GetTodayCompletionRate();
            IsTodayFullyCompleted = TodayCompletionRate >= 100.0;

            var previousLevel = CurrentPermission;
            CurrentPermission = CalculatePermission();

            if (previousLevel != CurrentPermission)
            {
                PermissionChanged?.Invoke(this, CurrentPermission);
            }
        }

        private EntertainmentLevel CalculatePermission()
        {
            // 如果今日100%，全解锁作为冲刺奖励
            if (IsTodayFullyCompleted)
            {
                return EntertainmentLevel.Overwatch;
            }

            // 根据昨日完成率降级映射
            return YesterdayCompletionRate switch
            {
                < 20 => EntertainmentLevel.Blender,      // >20%
                < 40 => EntertainmentLevel.UE_Engine,    // >40%
                < 60 => EntertainmentLevel.Godot,        // >60%
                < 70 => EntertainmentLevel.Steam,        // >70%
                < 80 => EntertainmentLevel.Minecraft,    // >80%
                < 90 => EntertainmentLevel.Overwatch,    // >90%
                _ => EntertainmentLevel.None             // 默认无权限
            };
        }

        public string GetPromptMessage()
        {
            if (IsTodayFullyCompleted)
            {
                return "🎉 完美！今日任务全清，娱乐权限已全开！";
            }

            if (YesterdayCompletionRate < 50 && TodayCompletionRate < 30)
            {
                return "🥱 有点懈怠了，昨天的债今天要还哦。";
            }

            if (YesterdayCompletionRate >= 80 && TodayCompletionRate == 0)
            {
                return "☀️ 昨天很棒！今天保持节奏，从第一个任务开始吧。";
            }

            return $"📊 当前权限等级: {GetLevelDisplayName(CurrentPermission)} ({TodayCompletionRate:F0}%)";
        }

        public static string GetLevelDisplayName(EntertainmentLevel level)
        {
            return level switch
            {
                EntertainmentLevel.None => "无",
                EntertainmentLevel.Blender => "Blender级",
                EntertainmentLevel.UE_Engine => "UE引擎级",
                EntertainmentLevel.Godot => "Godot级",
                EntertainmentLevel.Steam => "Steam级",
                EntertainmentLevel.Minecraft => "Minecraft级",
                EntertainmentLevel.Overwatch => "守望先锋级",
                _ => "未知"
            };
        }

        public bool IsAppUnlocked(string exeName)
        {
            // 如果今日100%，全部解锁
            if (IsTodayFullyCompleted) return true;

            // 这个方法会在ProcessBlockService中使用
            // 具体判断逻辑在那边实现
            return true;
        }
    }
}
