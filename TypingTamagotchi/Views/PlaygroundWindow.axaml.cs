using System;
using Avalonia.Controls;
using Avalonia.Input;
using TypingTamagotchi.ViewModels;

namespace TypingTamagotchi.Views;

public partial class PlaygroundWindow : Window
{
    private readonly PlaygroundViewModel _viewModel;

    public PlaygroundWindow()
    {
        InitializeComponent();
        _viewModel = new PlaygroundViewModel();
        DataContext = _viewModel;
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // 드래그로 윈도우 이동
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }

    private void OnCloseClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _viewModel.Stop();
        Close();
    }

    private void OnRefreshClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _viewModel.Refresh();
    }

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.Stop();
        base.OnClosed(e);
    }
}
