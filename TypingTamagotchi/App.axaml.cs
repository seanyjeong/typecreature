using System;
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

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit.
            DisableAvaloniaDataAnnotationValidation();

            try
            {
                // 데이터베이스 초기화
                _db = new DatabaseService();
                _db.SeedCreaturesIfEmpty();

                // 디버그: 현재 진열장 상태 출력
                var displaySlots = _db.GetDisplaySlots();
                Console.WriteLine($"[DEBUG] 진열장 슬롯 수: {displaySlots.Count}");
                foreach (var (slot, creatureId) in displaySlots)
                {
                    Console.WriteLine($"[DEBUG] 슬롯 {slot}: 크리처 ID {creatureId}");
                }

                // 시스템 트레이 설정
                SetupTrayIcon(desktop);

                // 미니 위젯 바로 표시
                ShowMiniWidget();

                desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"초기화 오류: {ex.Message}");
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

        // 트레이 아이콘 설정
        try
        {
            var iconUri = new Uri("avares://TypingTamagotchi/Assets/avalonia-logo.ico");
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
            Console.WriteLine($"트레이 아이콘 설정 오류: {ex.Message}");
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
            Console.WriteLine($"위젯 표시 오류: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
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
            Console.WriteLine($"도감 열기 오류: {ex.Message}");
        }
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
}
