using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using TypingTamagotchi.ViewModels;
using TypingTamagotchi.Views;

namespace TypingTamagotchi;

public partial class App : Application
{
    private TrayIcon? _trayIcon;
    private MainWindow? _mainWindow;
    private MainWindowViewModel? _viewModel;

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
            var collectionWindow = new CollectionWindow();
            collectionWindow.Show();
        };

        var widgetMenuItem = new NativeMenuItem("미니 위젯");
        widgetMenuItem.Click += (s, e) =>
        {
            _viewModel?.ToggleWidgetCommand.Execute(null);
        };

        var exitMenuItem = new NativeMenuItem("종료");
        exitMenuItem.Click += (s, e) =>
        {
            _trayIcon?.Dispose();
            desktop.Shutdown();
        };

        var menu = new NativeMenu();
        menu.Items.Add(showMenuItem);
        menu.Items.Add(new NativeMenuItemSeparator());
        menu.Items.Add(collectionMenuItem);
        menu.Items.Add(widgetMenuItem);
        menu.Items.Add(new NativeMenuItemSeparator());
        menu.Items.Add(exitMenuItem);

        _trayIcon = new TrayIcon
        {
            ToolTipText = "Typing Tamagotchi",
            Menu = menu,
            IsVisible = true
        };

        // 트레이 아이콘 더블클릭 시 창 표시
        _trayIcon.Clicked += (s, e) =>
        {
            _mainWindow?.Show();
            _mainWindow?.Activate();
        };
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
