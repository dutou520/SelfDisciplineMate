using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using Microsoft.Win32;

namespace SelfDisciplineMate.Services
{
    public class ProcessBlockService
    {
        private readonly SettingsService _settingsService;
        private readonly EntertainmentPermissionService _permissionService;

        private const string IFEO_PATH = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options";
        private readonly string _blockNotifierPath;

        public string LastErrorMessage { get; private set; } = string.Empty;
        public int BlockedAppsCount { get; private set; }
        public int UnblockedAppsCount { get; private set; }

        public ProcessBlockService(SettingsService settingsService, EntertainmentPermissionService permissionService)
        {
            _settingsService = settingsService;
            _permissionService = permissionService;

            // BlockNotifier.exe 路径 - 相对于当前程序目录
            var exeDir = AppContext.BaseDirectory;
            _blockNotifierPath = Path.Combine(exeDir, "BlockNotifier.exe");
        }

        public bool IsBlockNotifierExists()
        {
            var exists = File.Exists(_blockNotifierPath);
            if (!exists)
            {
                LastErrorMessage = $"未找到拦截组件: {_blockNotifierPath}";
            }
            return exists;
        }

        public string GetBlockNotifierPath()
        {
            return _blockNotifierPath;
        }

        public (bool Success, string Message) ApplyBlockPolicy()
        {
            LastErrorMessage = string.Empty;
            BlockedAppsCount = 0;
            UnblockedAppsCount = 0;

            // 检查 BlockNotifier 是否存在
            if (!IsBlockNotifierExists())
            {
                return (false, $"未找到拦截组件 BlockNotifier.exe\n请确保文件位于: {_blockNotifierPath}");
            }

            // 检查是否配置了任何应用
            if (_settingsService.Settings.AppPathMap.Count == 0)
            {
                return (false, "尚未配置任何应用拦截\n请在设置中添加要拦截的应用程序");
            }

            try
            {
                using var regKey = Registry.LocalMachine.OpenSubKey(IFEO_PATH, true);
                if (regKey == null)
                {
                    return (false, "无法访问注册表\n请尝试以管理员身份运行程序");
                }

                var currentLevel = _permissionService.CurrentPermission;
                var sb = new StringBuilder();

                foreach (var kvp in _settingsService.Settings.AppPathMap)
                {
                    var exeName = kvp.Key;
                    var mapping = kvp.Value;

                    try
                    {
                        var subKey = regKey.CreateSubKey(exeName);

                        // 如果当前权限等级 < 该应用要求的最低等级，则阻断
                        if (currentLevel < mapping.RequiredLevel)
                        {
                            // 杀死当前已经运行的不可用应用
                            var processName = exeName.Replace(".exe", "", StringComparison.OrdinalIgnoreCase);
                            var runningProcesses = Process.GetProcessesByName(processName);
                            foreach (var p in runningProcesses)
                            {
                                try
                                {
                                    p.Kill();
                                }
                                catch (Exception) { /* 忽略无法杀死的进程，可能是权限不够或其他原因 */ }
                            }

                            // 设置 Debugger 键值指向 BlockNotifier
                            subKey.SetValue("Debugger", $"\"{_blockNotifierPath}\" \"{mapping.AppName}\" {CalculateDaysOwed()}");
                            BlockedAppsCount++;
                        }
                        else
                        {
                            // 权限足够，移除阻断
                            try
                            {
                                subKey.DeleteValue("Debugger", false);
                            }
                            catch
                            {
                                // Value doesn't exist, ignore
                            }
                            UnblockedAppsCount++;
                        }
                        subKey.Close();
                    }
                    catch (UnauthorizedAccessException)
                    {
                        sb.AppendLine($"无法修改 {exeName}: 权限不足");
                    }
                    catch (Exception ex)
                    {
                        sb.AppendLine($"处理 {exeName} 时出错: {ex.Message}");
                    }
                }

                if (sb.Length > 0)
                {
                    return (true, $"部分应用处理失败:\n{sb}");
                }

                return (true, $"拦截策略已应用\n阻止了 {BlockedAppsCount} 个应用\n当前权限等级: {EntertainmentPermissionService.GetLevelDisplayName(currentLevel)}");
            }
            catch (UnauthorizedAccessException)
            {
                return (false, "需要管理员权限\n请右键以管理员身份运行程序");
            }
            catch (Exception ex)
            {
                LastErrorMessage = ex.Message;
                return (false, $"应用拦截策略失败: {ex.Message}");
            }
        }

        private int CalculateDaysOwed()
        {
            // 计算连续未达标天数
            var daysOwed = 0;
            var rate = _permissionService.YesterdayCompletionRate;

            while (rate < 90)
            {
                daysOwed++;
                rate += 10; // 简化计算
            }

            return Math.Min(daysOwed, 7); // 最多7天
        }

        public (bool Success, string Message) ClearAllBlocks()
        {
            try
            {
                using var regKey = Registry.LocalMachine.OpenSubKey(IFEO_PATH, true);
                if (regKey == null)
                {
                    return (false, "无法访问注册表");
                }

                var clearedCount = 0;

                foreach (var exeName in _settingsService.Settings.AppPathMap.Keys)
                {
                    try
                    {
                        using var subKey = regKey.OpenSubKey(exeName, true);
                        if (subKey != null)
                        {
                            subKey.DeleteValue("Debugger", false);
                            clearedCount++;
                        }
                    }
                    catch
                    {
                        // Key doesn't exist, ignore
                    }
                }

                return (true, $"已清除 {clearedCount} 个应用的拦截策略");
            }
            catch (UnauthorizedAccessException)
            {
                return (false, "需要管理员权限");
            }
            catch (Exception ex)
            {
                return (false, $"清除失败: {ex.Message}");
            }
        }

        public bool RequiresAdminRights()
        {
            // 检查是否需要管理员权限
            try
            {
                using var regKey = Registry.LocalMachine.OpenSubKey(IFEO_PATH, true);
                return regKey == null;
            }
            catch
            {
                return true;
            }
        }
    }
}
