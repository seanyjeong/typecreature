using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using TypingTamagotchi.Services;
using TypingTamagotchi.ViewModels;
using TypingTamagotchi.Views;

namespace TypingTamagotchi;

public partial class App : Application
{
    private TrayIcon? _trayIcon;
    private MiniWidget? _miniWidget;
    private MiniWidgetViewModel? _miniWidgetViewModel;
    private DatabaseService? _db;
    private IInputService? _inputService;
    private UpdateService? _updateService;

    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "TypingTamagotchi", "app.log"
    );

    public static void Log(string message)
    {
        try
        {
            var dir = Path.GetDirectoryName(LogPath);
            if (dir != null) Directory.CreateDirectory(dir);
            File.AppendAllText(LogPath, $"[{DateTime.Now:HH:mm:ss}] {message}\n");
        }
        catch { }
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        Log("=== App Starting ===");

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit.
            DisableAvaloniaDataAnnotationValidation();

            try
            {
                Log("Initializing database...");
                // 데이터베이스 초기화
                _db = new DatabaseService();
                _db.SeedCreaturesIfEmpty();
                _db.MigrateNewCreatures();

                // 디버그: 현재 진열장 상태 출력
                var displaySlots = _db.GetDisplaySlots();
                Console.WriteLine($"[DEBUG] Display slots count: {displaySlots.Count}");
                foreach (var (slot, creatureId) in displaySlots)
                {
                    Console.WriteLine($"[DEBUG] Slot {slot}: creature ID {creatureId}");
                }

                // 시스템 트레이 설정
                SetupTrayIcon(desktop);

                // 미니 위젯 바로 표시
                ShowMiniWidget();

                // 업데이트 완료 체크 (재시작 후 changelog 표시)
                CheckForCompletedUpdate();

                // 키보드/마우스 입력 감지 시작
                StartInputService();

                // 업데이트 체크 (백그라운드)
                Log("Starting update check...");
                _ = CheckForUpdatesAsync();

                desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Init error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void SetupTrayIcon(IClassicDesktopStyleApplicationLifetime desktop)
    {
        var widgetMenuItem = new NativeMenuItem("위젯 보이기/숨기기");
        widgetMenuItem.Click += (s, e) =>
        {
            if (_miniWidget == null || !_miniWidget.IsVisible)
            {
                ShowMiniWidget();
            }
            else
            {
                _miniWidget.Close();
                _miniWidget = null;
            }
        };

        var collectionMenuItem = new NativeMenuItem("도감 열기");
        collectionMenuItem.Click += (s, e) =>
        {
            OpenCollectionWindow();
        };

        var exitMenuItem = new NativeMenuItem("종료");
        exitMenuItem.Click += (s, e) =>
        {
            _miniWidget?.Close();
            _trayIcon?.Dispose();
            desktop.Shutdown();
        };

        var menu = new NativeMenu();
        menu.Items.Add(widgetMenuItem);
        menu.Items.Add(collectionMenuItem);
        menu.Items.Add(new NativeMenuItemSeparator());
        menu.Items.Add(exitMenuItem);

        // 트레이 아이콘 설정 (황금드래곤 - 트림된 버전)
        try
        {
            var iconUri = new Uri("avares://TypingTamagotchi/Assets/UI/app_icon.png");
            using var iconStream = AssetLoader.Open(iconUri);
            var icon = new WindowIcon(iconStream);

            _trayIcon = new TrayIcon
            {
                ToolTipText = "TypeCreature",
                Menu = menu,
                Icon = icon,
                IsVisible = true
            };

            // 트레이 아이콘 클릭 시 위젯 표시
            _trayIcon.Clicked += (s, e) =>
            {
                if (_miniWidget == null || !_miniWidget.IsVisible)
                {
                    ShowMiniWidget();
                }
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Tray icon error: {ex.Message}");
        }
    }

    private void ShowMiniWidget()
    {
        try
        {
            _miniWidgetViewModel = new MiniWidgetViewModel();
            _miniWidget = new MiniWidget
            {
                DataContext = _miniWidgetViewModel
            };
            _miniWidget.Show();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Widget display error: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }

    private void CheckForCompletedUpdate()
    {
        var updateInfo = CheckAndClearUpdateMarker();
        if (updateInfo.HasValue)
        {
            var (fromVersion, toVersion) = updateInfo.Value;
            Log($"Update completed: {fromVersion} -> {toVersion}. Showing changelog...");

            // 미니위젯에 업데이트 완료 팝업 표시
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                _miniWidgetViewModel?.ShowUpdateCompletePopup(toVersion);
            });
        }
    }

    private void OpenCollectionWindow()
    {
        try
        {
            var viewModel = new CollectionViewModel();
            var collectionWindow = new CollectionWindow
            {
                DataContext = viewModel
            };

            // 도감 창 닫힐 때 미니위젯 새로고침
            collectionWindow.Closed += (s, e) =>
            {
                _miniWidgetViewModel?.Refresh();
            };

            collectionWindow.Show();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Collection open error: {ex.Message}");
        }
    }

    private void StartInputService()
    {
        try
        {
            // Windows에서만 전역 키보드 훅 사용
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
                System.Runtime.InteropServices.OSPlatform.Windows))
            {
                _inputService = new WindowsInputService();
                _inputService.InputDetected += OnInputDetected;
                _inputService.Start();
                Console.WriteLine("[DEBUG] Keyboard/mouse input detection started");
            }
            else
            {
                Console.WriteLine("[DEBUG] Input detection unavailable (not Windows)");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Input service start error: {ex.Message}");
        }
    }

    private void OnInputDetected()
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            _miniWidgetViewModel?.OnInput();
        });
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }

    private async System.Threading.Tasks.Task CheckForUpdatesAsync()
    {
        try
        {
            Log("Creating UpdateService...");
            _updateService = new UpdateService();
            Log($"UpdateService created. CurrentVersion: {_updateService.CurrentVersion}");

            var hasUpdate = await _updateService.CheckForUpdatesAsync();
            Log($"Update check completed. HasUpdate: {hasUpdate}");

            if (hasUpdate)
            {
                var newVersion = _updateService.NewVersion;
                var currentVersion = _updateService.CurrentVersion;
                Log($"Update available: {currentVersion} -> {newVersion}. Starting silent download...");

                // 조용히 다운로드 (팝업 없음)
                await _updateService.DownloadUpdateAsync();
                Log("Download completed.");

                // 마커 파일 저장 (재시작 후 changelog 표시용)
                SaveUpdateMarker(currentVersion ?? "unknown", newVersion ?? "unknown");
                Log("Update marker saved. Applying update and restarting...");

                // 설치 및 재시작
                _updateService.ApplyUpdateAndRestart();
            }
        }
        catch (Exception ex)
        {
            Log($"Update error: {ex.Message}");
            Log($"Stack: {ex.StackTrace}");
        }
    }

    private static readonly string UpdateMarkerPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "TypingTamagotchi", "just_updated.txt"
    );

    private void SaveUpdateMarker(string fromVersion, string toVersion)
    {
        try
        {
            var dir = Path.GetDirectoryName(UpdateMarkerPath);
            if (dir != null) Directory.CreateDirectory(dir);
            File.WriteAllText(UpdateMarkerPath, $"{fromVersion}|{toVersion}");
        }
        catch (Exception ex)
        {
            Log($"Failed to save update marker: {ex.Message}");
        }
    }

    private (string fromVersion, string toVersion)? CheckAndClearUpdateMarker()
    {
        try
        {
            if (File.Exists(UpdateMarkerPath))
            {
                var content = File.ReadAllText(UpdateMarkerPath);
                File.Delete(UpdateMarkerPath);

                var parts = content.Split('|');
                if (parts.Length == 2)
                {
                    return (parts[0], parts[1]);
                }
            }
        }
        catch (Exception ex)
        {
            Log($"Failed to check update marker: {ex.Message}");
        }
        return null;
    }
}
