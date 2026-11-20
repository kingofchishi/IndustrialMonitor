namespace IndustrialMonitor.WinForms.Infrastructure;

/// <summary>
/// 本项目的数据根目录解析规则：
/// 1. 优先使用环境变量 INDUSTRIAL_MONITOR_HOME；
/// 2. 开发/调试时向上查找 IndustrialMonitor.sln，使用项目根目录下的 Data；
/// 3. 发布后使用程序所在目录下的 Data。
/// 不再使用 %LOCALAPPDATA%，避免把运行数据写到 C 盘。
/// </summary>
public sealed class AppPaths
{
    public AppPaths()
    {
        RootDirectory = ResolveRootDirectory();
        DataDirectory = Path.Combine(RootDirectory, "Data");
        DatabasePath = Path.Combine(DataDirectory, "industrial_monitor.db");
        Directory.CreateDirectory(DataDirectory);
    }

    /// <summary>项目根目录或程序目录；Data 目录的父目录。</summary>
    public string RootDirectory { get; }

    /// <summary>实际数据目录：<see cref="RootDirectory"/> 下的 Data。</summary>
    public string DataDirectory { get; }

    public string DatabasePath { get; }

    public long GetDatabaseSizeBytes()
    {
        var files = new[]
        {
            DatabasePath,
            DatabasePath + "-wal",
            DatabasePath + "-shm"
        };

        return files
            .Where(File.Exists)
            .Sum(path => new FileInfo(path).Length);
    }

    private static string ResolveRootDirectory()
    {
        var overridden = Environment.GetEnvironmentVariable("INDUSTRIAL_MONITOR_HOME");
        if (!string.IsNullOrWhiteSpace(overridden))
            return Path.GetFullPath(overridden);

        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "IndustrialMonitor.sln")))
                return current.FullName;

            current = current.Parent;
        }

        return AppContext.BaseDirectory;
    }
}
