using System;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using TypingTamagotchi.ViewModels;

namespace TypingTamagotchi.Views;

public partial class TypingPracticeWindow : Window
{
    private TypingPracticeViewModel? _viewModel;
    private TextBox? _inputTextBox;
    private bool _isClearing = false;

    // Windows API for Korean IME
    [DllImport("user32.dll")]
    private static extern IntPtr LoadKeyboardLayout(string pwszKLID, uint Flags);

    [DllImport("user32.dll")]
    private static extern IntPtr ActivateKeyboardLayout(IntPtr hkl, uint Flags);

    private const uint KLF_ACTIVATE = 1;
    private const string KOREAN_KEYBOARD = "00000412";

    public TypingPracticeWindow()
    {
        InitializeComponent();

        _inputTextBox = this.FindControl<TextBox>("InputTextBox");

        Opened += (s, e) =>
        {
            _inputTextBox?.Focus();
            TrySwitchToKorean();
        };

        if (_inputTextBox != null)
        {
            _inputTextBox.TextChanged += OnInputTextChanged;
            _inputTextBox.KeyDown += OnInputKeyDown;
        }

        DataContextChanged += (s, e) =>
        {
            if (DataContext is TypingPracticeViewModel vm)
            {
                _viewModel = vm;
                _viewModel.ClearInputRequested += OnClearInputRequested;
            }
        };

        Closed += (s, e) =>
        {
            if (_viewModel != null)
            {
                _viewModel.ClearInputRequested -= OnClearInputRequested;
            }
        };
    }

    private void OnInputTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_isClearing) return;

        if (_inputTextBox != null && _viewModel != null)
        {
            var text = _inputTextBox.Text ?? "";
            _viewModel.OnTextChanged(text);
        }
    }

    private void OnInputKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && _viewModel != null)
        {
            e.Handled = true;
            _viewModel.OnEnterPressed();
        }
    }

    private void OnClearInputRequested()
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            if (_inputTextBox != null)
            {
                _isClearing = true;
                _inputTextBox.Text = "";
                _isClearing = false;
                _inputTextBox.Focus();
            }
        });
    }

    private async void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        if (_viewModel != null)
        {
            var (avgCPM, completed, accuracy) = _viewModel.GetSessionSummary();

            if (completed > 0)
            {
                // 결과 팝업
                var summaryWindow = new Window
                {
                    Title = "⌨️ 타이핑 연습 결과",
                    Width = 300,
                    Height = 200,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Background = Avalonia.Media.Brushes.White,
                    CanResize = false
                };

                var panel = new StackPanel
                {
                    Margin = new Avalonia.Thickness(20),
                    Spacing = 10
                };

                panel.Children.Add(new TextBlock
                {
                    Text = "🎉 수고하셨습니다!",
                    FontSize = 18,
                    FontWeight = Avalonia.Media.FontWeight.Bold,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
                });

                panel.Children.Add(new TextBlock
                {
                    Text = $"📊 평균 타수: {avgCPM} CPM",
                    FontSize = 14
                });

                panel.Children.Add(new TextBlock
                {
                    Text = $"✅ 완료한 문장: {completed}개",
                    FontSize = 14
                });

                panel.Children.Add(new TextBlock
                {
                    Text = $"🎯 정확도: {accuracy}",
                    FontSize = 14
                });

                var closeButton = new Button
                {
                    Content = "확인",
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    Padding = new Avalonia.Thickness(20, 8),
                    Margin = new Avalonia.Thickness(0, 10, 0, 0)
                };
                closeButton.Click += (s, args) => summaryWindow.Close();
                panel.Children.Add(closeButton);

                summaryWindow.Content = panel;
                await summaryWindow.ShowDialog(this);
            }
        }

        Close();
    }

    private void TrySwitchToKorean()
    {
        // 영어 모드면 한글 전환 안함
        if (_viewModel?.IsEnglishMode == true) return;

        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var hkl = LoadKeyboardLayout(KOREAN_KEYBOARD, KLF_ACTIVATE);
                if (hkl != IntPtr.Zero)
                {
                    ActivateKeyboardLayout(hkl, 0);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to switch to Korean: {ex.Message}");
        }
    }
}
