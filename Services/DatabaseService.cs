using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.Sqlite;
using SelfDisciplineMate.Models;

namespace SelfDisciplineMate.Services
{
    public class DatabaseService : IDisposable
    {
        private readonly string _connectionString;
        private SqliteConnection? _connection;

        public DatabaseService()
        {
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SelfDisciplineMate");

            Directory.CreateDirectory(appDataPath);

            var dbPath = Path.Combine(appDataPath, "data.db");
            _connectionString = $"Data Source={dbPath}";

            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS TaskTemplates (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Title TEXT NOT NULL,
                    Weight INTEGER DEFAULT 1,
                    IsDeleted INTEGER DEFAULT 0,
                    CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP
                );

                CREATE TABLE IF NOT EXISTS DailyTaskLogs (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    TaskId INTEGER NOT NULL,
                    LogDate TEXT NOT NULL,
                    IsCompleted INTEGER DEFAULT 0,
                    CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (TaskId) REFERENCES TaskTemplates(Id)
                );

                CREATE TABLE IF NOT EXISTS PomodoroLogs (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    StartTime TEXT NOT NULL,
                    DurationMinutes INTEGER NOT NULL,
                    Type INTEGER NOT NULL
                );

                CREATE INDEX IF NOT EXISTS idx_dailytasklogs_date ON DailyTaskLogs(LogDate);
                CREATE INDEX IF NOT EXISTS idx_pomodoro_starttime ON PomodoroLogs(StartTime);
            ";
            command.ExecuteNonQuery();

            // 升级数据库 schema，增加 SortOrder
            try
            {
                var alterCmd = connection.CreateCommand();
                alterCmd.CommandText = "ALTER TABLE TaskTemplates ADD COLUMN SortOrder INTEGER DEFAULT 0;";
                alterCmd.ExecuteNonQuery();
            }
            catch { /* Column probably already exists */ }

            // 清理已经被删除的任务在今天的日志记录，防止由于删除任务导致今日进度无法到达100%
            try
            {
                var cleanupCmd = connection.CreateCommand();
                cleanupCmd.CommandText = "DELETE FROM DailyTaskLogs WHERE LogDate = @today AND TaskId IN (SELECT Id FROM TaskTemplates WHERE IsDeleted = 1)";
                cleanupCmd.Parameters.AddWithValue("@today", DateTime.Now.ToString("yyyy-MM-dd"));
                cleanupCmd.ExecuteNonQuery();
            }
            catch { /* Ignore */ }
        }

        // Task Templates
        public List<TaskTemplate> GetAllTasks()
        {
            var tasks = new List<TaskTemplate>();
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT Id, Title, Weight, IsDeleted, CreatedAt, SortOrder FROM TaskTemplates WHERE IsDeleted = 0 ORDER BY SortOrder ASC, Id ASC";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                tasks.Add(new TaskTemplate
                {
                    Id = reader.GetInt32(0),
                    Title = reader.GetString(1),
                    Weight = reader.GetInt32(2),
                    IsDeleted = reader.GetInt32(3) == 1,
                    CreatedAt = DateTime.Parse(reader.GetString(4)),
                    SortOrder = reader.IsDBNull(5) ? 0 : reader.GetInt32(5)
                });
            }
            return tasks;
        }

        public int AddTask(string title, int weight = 1)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO TaskTemplates (Title, Weight) VALUES (@title, @weight);
                SELECT last_insert_rowid();";
            command.Parameters.AddWithValue("@title", title);
            command.Parameters.AddWithValue("@weight", weight);

            return Convert.ToInt32(command.ExecuteScalar());
        }

        public void UpdateTask(TaskTemplate task)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE TaskTemplates SET Title = @title, Weight = @weight WHERE Id = @id";
            command.Parameters.AddWithValue("@id", task.Id);
            command.Parameters.AddWithValue("@title", task.Title);
            command.Parameters.AddWithValue("@weight", task.Weight);
            command.ExecuteNonQuery();
        }

        public void DeleteTask(int taskId)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "UPDATE TaskTemplates SET IsDeleted = 1 WHERE Id = @id";
            command.Parameters.AddWithValue("@id", taskId);
            command.ExecuteNonQuery();

            // 删除该任务针对今天的日志，防止其权重继续计入今日进度中
            var deleteLogCmd = connection.CreateCommand();
            deleteLogCmd.CommandText = "DELETE FROM DailyTaskLogs WHERE TaskId = @id AND LogDate = @today";
            deleteLogCmd.Parameters.AddWithValue("@id", taskId);
            deleteLogCmd.Parameters.AddWithValue("@today", DateTime.Now.ToString("yyyy-MM-dd"));
            deleteLogCmd.ExecuteNonQuery();
        }

        public void UpdateTaskOrders(List<int> orderedTaskIds)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();

            var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "UPDATE TaskTemplates SET SortOrder = @sortOrder WHERE Id = @id";
            var idParam = command.Parameters.Add("@id", SqliteType.Integer);
            var sortOrderParam = command.Parameters.Add("@sortOrder", SqliteType.Integer);

            for (int i = 0; i < orderedTaskIds.Count; i++)
            {
                idParam.Value = orderedTaskIds[i];
                sortOrderParam.Value = i;
                command.ExecuteNonQuery();
            }

            transaction.Commit();
        }

        // Daily Task Logs
        public void EnsureTodayLogsExist()
        {
            var today = DateTime.Now.ToString("yyyy-MM-dd");

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            // Check what tasks already have logs today
            var existingTaskIds = new HashSet<int>();
            var checkCmd = connection.CreateCommand();
            checkCmd.CommandText = "SELECT TaskId FROM DailyTaskLogs WHERE LogDate = @date";
            checkCmd.Parameters.AddWithValue("@date", today);
            using (var reader = checkCmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    existingTaskIds.Add(reader.GetInt32(0));
                }
            }

            // Get all non-deleted tasks
            var tasks = GetAllTasks();
            foreach (var task in tasks)
            {
                if (!existingTaskIds.Contains(task.Id))
                {
                    var insertCommand = connection.CreateCommand();
                    insertCommand.CommandText = @"
                        INSERT INTO DailyTaskLogs (TaskId, LogDate, IsCompleted)
                        VALUES (@taskId, @date, 0)";
                    insertCommand.Parameters.AddWithValue("@taskId", task.Id);
                    insertCommand.Parameters.AddWithValue("@date", today);
                    insertCommand.ExecuteNonQuery();
                }
            }
        }

        public List<TaskItem> GetTodayTasks()
        {
            var today = DateTime.Now.ToString("yyyy-MM-dd");
            var tasks = new List<TaskItem>();

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT t.Id, t.Title, t.Weight, d.IsCompleted, d.Id as LogId, t.SortOrder
                FROM TaskTemplates t
                LEFT JOIN DailyTaskLogs d ON t.Id = d.TaskId AND d.LogDate = @date
                WHERE t.IsDeleted = 0
                ORDER BY t.SortOrder ASC, t.Id ASC";
            command.Parameters.AddWithValue("@date", today);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                tasks.Add(new TaskItem
                {
                    Id = reader.GetInt32(0),
                    Title = reader.GetString(1),
                    Weight = reader.GetInt32(2),
                    IsCompleted = reader.IsDBNull(3) ? false : reader.GetInt32(3) == 1,
                    LogId = reader.IsDBNull(4) ? 0 : reader.GetInt32(4),
                    SortOrder = reader.IsDBNull(5) ? 0 : reader.GetInt32(5),
                    LogDate = today
                });
            }
            return tasks;
        }

        public void UpdateTaskCompletion(int logId, bool isCompleted)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "UPDATE DailyTaskLogs SET IsCompleted = @completed WHERE Id = @id";
            command.Parameters.AddWithValue("@id", logId);
            command.Parameters.AddWithValue("@completed", isCompleted ? 1 : 0);
            command.ExecuteNonQuery();
        }

        // Statistics
        public double GetYesterdayCompletionRate()
        {
            var yesterday = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            return GetCompletionRateForDate(yesterday);
        }

        public double GetTodayCompletionRate()
        {
            var today = DateTime.Now.ToString("yyyy-MM-dd");
            return GetCompletionRateForDate(today);
        }

        private double GetCompletionRateForDate(string date)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            // Get total weight
            var totalCmd = connection.CreateCommand();
            totalCmd.CommandText = @"
                SELECT COALESCE(SUM(t.Weight), 0)
                FROM DailyTaskLogs d
                JOIN TaskTemplates t ON d.TaskId = t.Id
                WHERE d.LogDate = @date";
            totalCmd.Parameters.AddWithValue("@date", date);
            var totalWeight = Convert.ToDouble(totalCmd.ExecuteScalar());

            if (totalWeight == 0) return 0;

            // Get completed weight
            var completedCmd = connection.CreateCommand();
            completedCmd.CommandText = @"
                SELECT COALESCE(SUM(t.Weight), 0)
                FROM DailyTaskLogs d
                JOIN TaskTemplates t ON d.TaskId = t.Id
                WHERE d.LogDate = @date AND d.IsCompleted = 1";
            completedCmd.Parameters.AddWithValue("@date", date);
            var completedWeight = Convert.ToDouble(completedCmd.ExecuteScalar());

            return (completedWeight / totalWeight) * 100;
        }

        public int GetTodayPomodoroCount()
        {
            var today = DateTime.Now.ToString("yyyy-MM-dd");
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT COUNT(*) FROM PomodoroLogs
                WHERE Type = 0 AND StartTime >= @start AND StartTime < @end";
            command.Parameters.AddWithValue("@start", today + " 00:00:00");
            command.Parameters.AddWithValue("@end", DateTime.Now.AddDays(1).ToString("yyyy-MM-dd") + " 00:00:00");

            return Convert.ToInt32(command.ExecuteScalar());
        }

        public int GetPomodoroCountForDate(string date)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT COUNT(*) FROM PomodoroLogs
                WHERE Type = 0 AND StartTime >= @start AND StartTime < @end";
            command.Parameters.AddWithValue("@start", date + " 00:00:00");
            command.Parameters.AddWithValue("@end", DateTime.Parse(date).AddDays(1).ToString("yyyy-MM-dd") + " 00:00:00");

            return Convert.ToInt32(command.ExecuteScalar());
        }

        public class DailyStatistics
        {
            public string Date { get; set; } = string.Empty;
            public double CompletionRate { get; set; }
            public int PomodoroCount { get; set; }
        }

        public List<DailyStatistics> GetMonthStatistics(int year, int month)
        {
            var result = new List<DailyStatistics>();
            var daysInMonth = DateTime.DaysInMonth(year, month);

            for (int day = 1; day <= daysInMonth; day++)
            {
                var date = new DateTime(year, month, day).ToString("yyyy-MM-dd");
                var completionRate = GetCompletionRateForDate(date);
                var pomodoroCount = GetPomodoroCountForDate(date);

                result.Add(new DailyStatistics
                {
                    Date = date,
                    CompletionRate = completionRate,
                    PomodoroCount = pomodoroCount
                });
            }

            return result;
        }

        // Pomodoro Logs
        public void AddPomodoroLog(int durationMinutes, PomodoroType type)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO PomodoroLogs (StartTime, DurationMinutes, Type)
                VALUES (@startTime, @duration, @type)";
            command.Parameters.AddWithValue("@startTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@duration", durationMinutes);
            command.Parameters.AddWithValue("@type", (int)type);
            command.ExecuteNonQuery();
        }

        public void Dispose()
        {
            _connection?.Dispose();
        }
    }
}
