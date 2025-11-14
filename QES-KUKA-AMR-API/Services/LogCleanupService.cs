using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QES_KUKA_AMR_API.Data;
using QES_KUKA_AMR_API.Data.Entities;
using QES_KUKA_AMR_API.Options;
using System.Globalization;

namespace QES_KUKA_AMR_API.Services
{
    public class LogCleanupService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<LogCleanupService> _logger;
        private readonly string _logRootPath;

        public LogCleanupService(
            ApplicationDbContext context,
            ILogger<LogCleanupService> logger,
            IOptions<LogCleanupOptions> options)
        {
            _context = context;
            _logger = logger;

            var configPath = options.Value.LogDirectory;
            _logRootPath = string.IsNullOrWhiteSpace(configPath)
                ? Path.Combine(AppContext.BaseDirectory, "Logs") // fallback default
                : configPath;

            if (!Directory.Exists(_logRootPath))
            {
                Directory.CreateDirectory(_logRootPath);
                _logger.LogInformation("Created log directory at {Path}", _logRootPath);
            }

            _logger.LogInformation("Using log directory: {Path}", _logRootPath);
        }

        public async Task CleanOldLogsAsync()
        {
            try
            {
                var setting = await _context.SystemSettings
                    .FirstOrDefaultAsync(s => s.Key == "LogRetentionMonths");

                int retentionMonths = 1;
                if (setting != null && int.TryParse(setting.Value, out var value))
                    retentionMonths = Math.Max(0, Math.Min(120, value));

                bool keepPreviousMonth = true;

                _logger.LogInformation("Starting log cleanup. RetentionMonths={RetentionMonths}, keepPreviousMonth={KeepPrevious}",
                    retentionMonths, keepPreviousMonth);

                if (!Directory.Exists(_logRootPath))
                {
                    _logger.LogWarning("Log folder not found: {Path}", _logRootPath);
                    return;
                }

                var rawFolders = Directory.GetDirectories(_logRootPath)
                    .Select(Path.GetFileName)
                    .Where(n => !string.IsNullOrEmpty(n))
                    .Select(n => n!.Trim())
                    .Distinct()
                    .ToList();

                _logger.LogInformation("Found {Count} raw folders: {Folders}", rawFolders.Count, string.Join(", ", rawFolders));

                var parsed = new List<(string Name, DateTime Date)>();
                foreach (var name in rawFolders)
                {
                    if (DateTime.TryParseExact(name, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                    {
                        parsed.Add((name, new DateTime(dt.Year, dt.Month, 1)));
                    }
                    else
                    {
                        _logger.LogWarning("Skipping folder with non-matching name format (expected yyyy-MM): {Folder}", name);
                    }
                }

                if (!parsed.Any())
                {
                    _logger.LogInformation("No parseable yyyy-MM folders found in {Path}", _logRootPath);
                    return;
                }

                var ordered = parsed.OrderBy(p => p.Date).ToList();
                _logger.LogInformation("Parsed folders (oldest->newest): {Folders}",
                    string.Join(", ", ordered.Select(o => o.Name)));

                var latestMonth = ordered.Last().Date;
                int keepCount = keepPreviousMonth ? (retentionMonths + 1) : retentionMonths;
                if (keepCount < 0) keepCount = 0;

                DateTime cutoffDate = keepCount == 0
                    ? DateTime.MaxValue
                    : latestMonth.AddMonths(-(keepCount - 1));

                _logger.LogInformation("LatestMonth={Latest:yyyy-MM}, KeepCount={KeepCount}, CutoffDate={Cutoff:yyyy-MM}",
                    latestMonth, keepCount, cutoffDate);

                var toDelete = keepCount == 0
                    ? ordered.ToList()
                    : ordered.Where(f => f.Date < cutoffDate).ToList();

                _logger.LogInformation("Folders to delete ({Count}): {Folders}",
                    toDelete.Count, string.Join(", ", toDelete.Select(d => d.Name)));

                foreach (var folder in toDelete)
                {
                    var fullPath = Path.Combine(_logRootPath, folder.Name);
                    try
                    {
                        if (Directory.Exists(fullPath))
                        {
                            Directory.Delete(fullPath, recursive: true);
                            _logger.LogInformation("Deleted old log folder: {Folder}", fullPath);
                        }
                        else
                        {
                            _logger.LogWarning("Folder missing at deletion time: {Folder}", fullPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to delete log folder: {Folder}", fullPath);
                    }
                }

                _logger.LogInformation("Log cleanup completed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during log cleanup.");
            }
        }
    }
}
