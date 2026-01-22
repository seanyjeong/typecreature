using System;
using System.IO;
using System.Threading.Tasks;
using Velopack;
using Velopack.Sources;

namespace TypingTamagotchi.Services;

public class UpdateService
{
    private readonly UpdateManager _updateManager;
    private UpdateInfo? _updateInfo;

    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "TypingTamagotchi", "update.log"
    );

    private static void Log(string message)
    {
        try
        {
            var dir = Path.GetDirectoryName(LogPath);
            if (dir != null) Directory.CreateDirectory(dir);
            File.AppendAllText(LogPath, $"[{DateTime.Now:HH:mm:ss}] {message}\n");
        }
        catch { }
    }

    public event Action<string>? UpdateAvailable;
    public event Action<int>? DownloadProgress;
    public event Action? UpdateReady;

    public string? CurrentVersion => _updateManager.CurrentVersion?.ToString();
    public string? NewVersion => _updateInfo?.TargetFullRelease?.Version?.ToString();
    public bool HasUpdate => _updateInfo != null;

    public UpdateService()
    {
        Log("UpdateService initializing...");

        // GitHub Releases를 업데이트 소스로 사용
        var source = new GithubSource(
            "https://github.com/seanyjeong/typecreature",
            null,  // public repo라서 토큰 불필요
            false  // prerelease 제외
        );
        _updateManager = new UpdateManager(source);

        Log($"UpdateService initialized. IsInstalled: {_updateManager.IsInstalled}");
    }

    public async Task<bool> CheckForUpdatesAsync(int retryCount = 3)
    {
        for (int attempt = 1; attempt <= retryCount; attempt++)
        {
            try
            {
                Log($"[Attempt {attempt}/{retryCount}] Current version: {CurrentVersion ?? "unknown"}");
                Log($"IsInstalled: {_updateManager.IsInstalled}");

                if (!_updateManager.IsInstalled)
                {
                    Log("App is not installed via Velopack, skipping update check");
                    return false;
                }

                // 첫 시도가 아니면 잠시 대기
                if (attempt > 1)
                {
                    Log($"Waiting 3 seconds before retry...");
                    await Task.Delay(3000);
                }

                _updateInfo = await _updateManager.CheckForUpdatesAsync();

                if (_updateInfo != null)
                {
                    var newVersion = _updateInfo.TargetFullRelease.Version.ToString();
                    Log($"New version available: {newVersion}");
                    UpdateAvailable?.Invoke(newVersion);
                    return true;
                }
                else
                {
                    Log("No updates available");
                    return false; // 성공적으로 체크했지만 업데이트 없음
                }
            }
            catch (Exception ex)
            {
                Log($"[Attempt {attempt}] Check failed: {ex.Message}");
                if (attempt == retryCount)
                {
                    Log($"All {retryCount} attempts failed. Stack: {ex.StackTrace}");
                }
            }
        }

        return false;
    }

    public async Task DownloadUpdateAsync()
    {
        if (_updateInfo == null) return;

        try
        {
            await _updateManager.DownloadUpdatesAsync(_updateInfo, progress =>
            {
                DownloadProgress?.Invoke(progress);
            });

            UpdateReady?.Invoke();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Download failed: {ex.Message}");
        }
    }

    public void ApplyUpdateAndRestart()
    {
        if (_updateInfo == null) return;

        try
        {
            _updateManager.ApplyUpdatesAndRestart(_updateInfo.TargetFullRelease);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Apply update failed: {ex.Message}");
        }
    }
}
