using Microsoft.Data.Sqlite;

namespace HotelApi.Services;

// Chạy nền: sao lưu hotel.db định kỳ vào thư mục Backups, chỉ giữ N bản mới nhất
public class BackupService : BackgroundService
{
    private readonly IConfiguration _config;
    private readonly ILogger<BackupService> _logger;

    public BackupService(IConfiguration config, ILogger<BackupService> logger)
    {
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var hours = _config.GetValue("Backup:IntervalHours", 6);
        var keep = _config.GetValue("Backup:KeepLast", 7);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                Backup(keep);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sao lưu cơ sở dữ liệu thất bại");
            }

            try
            {
                await Task.Delay(TimeSpan.FromHours(hours), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private void Backup(int keep)
    {
        var connectionString = _config.GetConnectionString("DefaultConnection");
        var folder = Path.Combine(Directory.GetCurrentDirectory(), "Backups");
        Directory.CreateDirectory(folder);

        var file = Path.Combine(folder, $"hotel-{DateTime.Now:yyyyMMdd-HHmmss}.db");

        using (var source = new SqliteConnection(connectionString))
        using (var target = new SqliteConnection($"Data Source={file};Pooling=False"))
        {
            source.Open();
            target.Open();
            source.BackupDatabase(target);   // sao lưu nhất quán, kể cả khi ứng dụng đang ghi dữ liệu
        }

        // Chỉ giữ N bản mới nhất, xóa các bản cũ hơn
        foreach (var old in new DirectoryInfo(folder).GetFiles("hotel-*.db")
                     .OrderByDescending(f => f.CreationTimeUtc).Skip(keep))
        {
            old.Delete();
        }

        _logger.LogInformation("Đã sao lưu cơ sở dữ liệu: {File}", Path.GetFileName(file));
    }
}