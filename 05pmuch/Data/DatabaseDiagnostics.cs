using System;
using System.Data.Entity;
using System.Data.Entity.Core;
using System.IO;
using System.Linq;
using System.Text;

namespace _05pmuch.Data
{
    public static class DatabaseDiagnostics
    {
        public static string RunFullReportSafe()
        {
            var report = new StringBuilder();
            var path = DatabaseBootstrap.GetDatabasePath();

            report.AppendLine("=== Диагностика базы данных ===");
            report.AppendLine($"Время: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine($"DataDirectory: {AppDomain.CurrentDomain.GetData("DataDirectory")}");
            report.AppendLine($"BaseDirectory: {AppDomain.CurrentDomain.BaseDirectory}");
            report.AppendLine($"Файл БД: {path}");
            report.AppendLine($"Существует: {File.Exists(path)}");

            if (File.Exists(path))
                report.AppendLine($"Размер: {new FileInfo(path).Length} байт (0 = пустой/битый файл)");

            report.AppendLine();
            AppendTables(report, path);
            return report.ToString();
        }

        public static string FormatException(Exception ex)
        {
            var sb = new StringBuilder();
            var i = 0;
            for (var cur = ex; cur != null; cur = cur.InnerException, i++)
            {
                sb.AppendLine($"[{i}] {cur.GetType().Name}: {cur.Message}");
                if (cur is EntityException)
                    sb.AppendLine(cur.ToString());
            }
            return sb.ToString();
        }

        public static void SaveReportToFile(string report)
        {
            var logPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "db-diagnostic.log");
            File.WriteAllText(logPath, report, Encoding.UTF8);
        }

        private static void AppendTables(StringBuilder report, string path)
        {
            report.AppendLine("--- Таблицы в SQLite ---");
            if (!File.Exists(path) || new FileInfo(path).Length == 0)
            {
                report.AppendLine("(файл отсутствует или пуст — EF не сможет выполнять запросы)");
                return;
            }

            try
            {
                using (var conn = new System.Data.SQLite.SQLiteConnection($"Data Source={path};Version=3;"))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' ORDER BY name";
                        using (var reader = cmd.ExecuteReader())
                        {
                            var any = false;
                            while (reader.Read())
                            {
                                any = true;
                                report.AppendLine("  • " + reader.GetString(0));
                            }
                            if (!any)
                                report.AppendLine("  (таблиц нет — нужен DatabaseBootstrap.EnsureReady())");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                report.AppendLine($"Ошибка чтения схемы: {ex.Message}");
            }
        }
    }
}
