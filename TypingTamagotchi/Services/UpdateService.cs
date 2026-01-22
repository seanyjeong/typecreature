using System;
using System.Threading.Tasks;
using Velopack;
using Velopack.Sources;

namespace TypingTamagotchi.Services;

public class UpdateService
{
    private readonly UpdateManager _updateManager;
    private UpdateInfo? _updateInfo;

    public event Action<string>? UpdateAvailable;
    public event Action<int>? DownloadProgress;
    public event Action? UpdateReady;

    public string? CurrentVersion => _updateManager.CurrentVersion?.ToString();
    public string? NewVersion => _updateInfo?.TargetFullRelease?.Version?.ToString();
    public bool HasUpdate => _updateInfo != null;

    public UpdateService()
    {
        // GitHub Releases를 업데이트 소스로 사용
        var source = new GithubSource(
            "https://github.com/seanyjeong/typecreature",
            null,  // public repo라서 토큰 불필요
            false  // prerelease 제외
        );
        _updateManager = new UpdateManager(source);
    }

    public async Task<bool> CheckForUpdatesAsync()
    {
        try
        {
            Console.WriteLine($"[Update] Current version: {CurrentVersion ?? "unknown"}");
            Console.WriteLine($"[Update] IsInstalled: {_updateManager.IsInstalled}");

            if (!_updateManager.IsInstalled)
            {
                Console.WriteLine("[Update] App is not installed via Velopack, skipping update check");
                return false;
            }

            _updateInfo = await _updateManager.CheckForUpdatesAsync();

            if (_updateInfo != null)
            {
                var newVersion = _updateInfo.TargetFullRelease.Version.ToString();
                Console.WriteLine($"[Update] New version available: {newVersion}");
                UpdateAvailable?.Invoke(newVersion);
                return true;
            }
            else
            {
                Console.WriteLine("[Update] No updates available");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Update] Check failed: {ex.Message}");
            Console.WriteLine($"[Update] Stack: {ex.StackTrace}");
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
