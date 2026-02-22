using ERP_Web.Core.Constants;
using SqlSugar;
using System.ComponentModel.DataAnnotations.Schema;


namespace ERP_Web.Models.PrivilegeHub
{
    // 日志级别枚举
    public enum LogLevel
    {
        Debug = 0,
        Info = 1,
        Warning = 2,
        Error = 3,
        Critical = 4
    }

    // 日志实体类
    [Table("SystemLogs")]
    public class LogEntry
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public long Id { get; set; }

        [SugarColumn(Length = 20)]
        public string Module { get; set; } = "Inventory";

        public DateTime Timestamp { get; set; } = DateTime.Now;

        [SugarColumn(Length = 10)]
        public LogLevel Level { get; set; }

        [SugarColumn(Length = 100)]
        public string? User { get; set; }

        [SugarColumn(Length = 50)]
        public string? DocumentType { get; set; }

        [SugarColumn(Length = 50)]
        public string? DocumentCode { get; set; }

        [SugarColumn(Length = 200)]
        public string? Operation { get; set; }

        [SugarColumn(Length = 1000)]
        public string Message { get; set; }

        [SugarColumn(Length = 200)]
        public string? Source { get; set; }

        [SugarColumn(IsJson = true)]
        public Dictionary<string, object>? Metadata { get; set; }
    }

    // 日志配置类
    public class LogConfig
    {
        public LogLevel MinLogLevel { get; set; } = LogLevel.Info;
        public bool EnableConsoleLogging { get; set; } = true;
        public bool EnableFileLogging { get; set; } = true;
        public bool EnableDatabaseLogging { get; set; } = true;
        public string LogFilePath { get; set; } = "Logs/application.log";
    }

    // 日志写入器接口
    public interface ILogWriter
    {
        void Write(LogEntry entry);
    }

    // 控制台日志写入器
    public class ConsoleLogWriter : ILogWriter
    {
        public void Write(LogEntry entry)
        {
            var color = entry.Level switch
            {
                LogLevel.Debug => ConsoleColor.Gray,
                LogLevel.Info => ConsoleColor.White,
                LogLevel.Warning => ConsoleColor.Yellow,
                LogLevel.Error => ConsoleColor.Red,
                LogLevel.Critical => ConsoleColor.DarkRed,
                _ => ConsoleColor.White
            };

            Console.ForegroundColor = color;
            Console.WriteLine($"[{entry.Timestamp:HH:mm:ss}] [{entry.Level}] {entry.Message}");
            Console.ResetColor();
        }
    }

    // 文件日志写入器
    public class FileLogWriter : ILogWriter
    {
        private readonly string _filePath;
        private static readonly object _lock = new object();

        public FileLogWriter(string filePath)
        {
            _filePath = filePath;
            EnsureDirectoryExists();
        }

        private void EnsureDirectoryExists()
        {
            var dir = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }

        public void Write(LogEntry entry)
        {
            lock (_lock)
            {
                File.AppendAllText(_filePath, $"{entry.Timestamp:yyyy-MM-dd HH:mm:ss} [{entry.Level}] {entry.Message}\n");
            }
        }
    }

    // 数据库日志写入器
    public class DatabaseLogWriter : ILogWriter
    {
        private readonly SqlSugarScope _db;

        public DatabaseLogWriter(SqlSugarScope db)
        {
            _db = db;
        }

        public void Write(LogEntry entry)
        {
            try
            {
                _db.Insertable(entry).ExecuteCommand();
            }
            catch (Exception ex)
            {
                // 数据库日志失败时回退到文件日志
                File.AppendAllText("Logs/db_error.log",
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Failed to write log to DB: {ex.Message}\n");
            }
        }
    }

    // 审计日志类
    public static class AuditLog
    {
        private static readonly List<ILogWriter> _writers = new List<ILogWriter>();
        private static LogConfig _config = new LogConfig();
        private static bool _initialized = false;
        private static readonly object _initLock = new object();

        // 初始化日志系统
        public static void Initialize(LogConfig config, SqlSugarScope db = null)
        {
            lock (_initLock)
            {
                if (_initialized) return;

                _config = config;
                _writers.Clear();

                if (config.EnableConsoleLogging)
                    _writers.Add(new ConsoleLogWriter());

                if (config.EnableFileLogging)
                    _writers.Add(new FileLogWriter(config.LogFilePath));

                if (config.EnableDatabaseLogging && db != null)
                    _writers.Add(new DatabaseLogWriter(db));

                _initialized = true;

                // 记录初始化日志
                Log(LogLevel.Info, "Log system initialized", "System");
            }
        }

        // 核心日志方法
        private static void Log(LogLevel level, string message, string operation = null,
            string documentType = null, string documentCode = null,
            Dictionary<string, object> metadata = null)
        {
            if (!_initialized) return;
            if (level < _config.MinLogLevel) return;

            var entry = new LogEntry
            {
                Level = level,
                Message = message,
                Operation = operation,
                DocumentType = documentType,
                DocumentCode = documentCode,
                User = UserContext.CurrentUser?.Name ?? "System",
                Source = GetCallingSource(),
                Metadata = metadata
            };

            foreach (var writer in _writers)
            {
                try
                {
                    writer.Write(entry);
                }
                catch
                {
                    // 防止单个写入器失败影响其他
                }
            }
        }

        // 获取调用源信息
        private static string GetCallingSource()
        {
            try
            {
                var stackTrace = new System.Diagnostics.StackTrace(2, false);
                var frame = stackTrace.GetFrame(0);
                var method = frame?.GetMethod();
                return $"{method?.DeclaringType?.Name}.{method?.Name}";
            }
            catch
            {
                return "Unknown";
            }
        }

        // 快捷方法 - 文档操作
        public static void DocumentCreated(string documentType, string documentCode, string message = null)
        {
            Log(LogLevel.Info,
                message ?? $"{documentType} {documentCode} 已创建",
                "Create",
                documentType,
                documentCode);
        }

        public static void DocumentUpdated(string documentType, string documentCode, string message = null)
        {
            Log(LogLevel.Info,
                message ?? $"{documentType} {documentCode} 已更新",
                "Update",
                documentType,
                documentCode);
        }

        public static void DocumentApproved(string documentType, string documentCode, string message = null)
        {
            Log(LogLevel.Info,
                message ?? $"{documentType} {documentCode} 已审批",
                "Approve",
                documentType,
                documentCode);
        }

        public static void DocumentCompleted(string documentType, string documentCode, string message = null)
        {
            Log(LogLevel.Info,
                message ?? $"{documentType} {documentCode} 已完成",
                "Complete",
                documentType,
                documentCode);
        }

        // 快捷方法 - 库存操作
        public static void InventoryUpdated(string warehouse, string invCode, decimal quantity, string operation)
        {
            Log(LogLevel.Debug,
                $"库存更新: 仓库 {warehouse}, 物料 {invCode}, 数量 {quantity}, 操作 {operation}",
                "InventoryUpdate",
                metadata: new Dictionary<string, object>
                {
                { "Warehouse", warehouse },
                { "InvCode", invCode },
                { "Quantity", quantity }
                });
        }

        public static void ReceivingCompleted(string receivingNoteCode, int transactionCount)
        {
            Log(LogLevel.Info,
                $"收货单 {receivingNoteCode} 已完成入库，生成 {transactionCount} 个入库单",
                "ReceivingComplete",
                "ReceivingNote",
                receivingNoteCode);
        }

        // 通用日志方法
        public static void Debug(string message, string operation = null)
            => Log(LogLevel.Debug, message, operation);

        public static void Info(string message, string operation = null)
            => Log(LogLevel.Info, message, operation);

        public static void Warning(string message, string operation = null)
            => Log(LogLevel.Warning, message, operation);

        public static void Error(string message, Exception ex = null, string operation = null)
        {
            var fullMessage = ex == null
                ? message
                : $"{message} | 错误: {ex.Message}";

            Log(LogLevel.Error, fullMessage, operation,
                metadata: ex != null
                    ? new Dictionary<string, object> { { "StackTrace", ex.StackTrace } }
                    : null);
        }

        public static void Critical(string message, Exception ex = null, string operation = null)
        {
            var fullMessage = ex == null
                ? message
                : $"{message} | 严重错误: {ex.Message}";

            Log(LogLevel.Critical, fullMessage, operation,
                metadata: ex != null
                    ? new Dictionary<string, object> { { "StackTrace", ex.StackTrace } }
                    : null);
        }
    }
}
