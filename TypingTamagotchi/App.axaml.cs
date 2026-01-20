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
    private MainWindow? _mainWindow;
    private MainWindowViewModel? _viewModel;
    private DesktopPetManager? _petManager;
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

            // 데이터베이스 및 펫 매니저 초기화
            _db = new DatabaseService();
            _petManager = new DesktopPetManager(_db);

            _viewModel = new MainWindowViewModel();
            _mainWindow = new MainWindow
            {
                DataContext = _viewModel,
            };

            desktop.MainWindow = _mainWindow;
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            // 메인 창 닫기 버튼 클릭 시 숨기기 (트레이로)
            _mainWindow.Closing += (s, e) =>
            {
                e.Cancel = true;
                _mainWindow.Hide();
            };

            // 시스템 트레이 설정
            SetupTrayIcon(desktop);

            // 바탕화면 펫 시작
            _petManager.Start();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void SetupTrayIcon(IClassicDesktopStyleApplicationLifetime desktop)
    {
        var showMenuItem = new NativeMenuItem("창 보이기");
        showMenuItem.Click += (s, e) =>
        {
            _mainWindow?.Show();
            _mainWindow?.Activate();
        };

        var collectionMenuItem = new NativeMenuItem("도감 열기");
        collectionMenuItem.Click += (s, e) =>
        {
            OpenCollectionWindow();
        };

        var widgetMenuItem = new NativeMenuItem("미니 위젯");
        widgetMenuItem.Click += (s, e) =>
        {
            _viewModel?.ToggleWidgetCommand.Execute(null);
            // 미니 위젯 활성화 시 메인 창 숨기기
            if (_viewModel?.IsWidgetVisible == true)
            {
                _mainWindow?.Hide();
            }
        };

        var petVisibilityMenuItem = new NativeMenuItem("바탕화면 펫 숨기기/보이기");
        petVisibilityMenuItem.Click += (s, e) =>
        {
            _petManager?.ToggleVisibility();
        };

        var exitMenuItem = new NativeMenuItem("종료");
        exitMenuItem.Click += (s, e) =>
        {
            _petManager?.Stop();
            _trayIcon?.Dispose();
            desktop.Shutdown();
        };

        var menu = new NativeMenu();
        menu.Items.Add(showMenuItem);
        menu.Items.Add(new NativeMenuItemSeparator());
        menu.Items.Add(collectionMenuItem);
        menu.Items.Add(widgetMenuItem);
        menu.Items.Add(petVisibilityMenuItem);
        menu.Items.Add(new NativeMenuItemSeparator());
        menu.Items.Add(exitMenuItem);

        // 트레이 아이콘 설정
        var iconUri = new Uri("avares://TypingTamagotchi/Assets/avalonia-logo.ico");
        using var iconStream = Avalonia.Platform.AssetLoader.Open(iconUri);
        var icon = new WindowIcon(iconStream);

        _trayIcon = new TrayIcon
        {
            ToolTipText = "Typing Tamagotchi",
            Menu = menu,
            Icon = icon,
            IsVisible = true
        };

        // 트레이 아이콘 더블클릭 시 창 표시
        _trayIcon.Clicked += (s, e) =>
        {
            _mainWindow?.Show();
            _mainWindow?.Activate();
        };
    }

    private void OpenCollectionWindow()
    {
        var viewModel = new CollectionViewModel();
        if (_petManager != null)
        {
            viewModel.SetPetManager(_petManager);
        }
        var collectionWindow = new CollectionWindow
        {
            DataContext = viewModel
        };
        collectionWindow.Show();
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
