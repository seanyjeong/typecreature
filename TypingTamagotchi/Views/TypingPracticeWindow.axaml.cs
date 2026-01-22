using System;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Interactivity;
using TypingTamagotchi.ViewModels;

namespace TypingTamagotchi.Views;

public partial class TypingPracticeWindow : Window
{
    private TypingPracticeViewModel? _viewModel;
    private TextBox? _inputTextBox;
    private bool _isClearing = false; // 초기화 중 플래그

    // Windows API for Korean IME
    [DllImport("user32.dll")]
    private static extern IntPtr LoadKeyboardLayout(string pwszKLID, uint Flags);

    [DllImport("user32.dll")]
    private static extern IntPtr ActivateKeyboardLayout(IntPtr hkl, uint Flags);

    private const uint KLF_ACTIVATE = 1;
    private const string KOREAN_KEYBOARD = "00000412"; // Korean keyboard layout

    public TypingPracticeWindow()
    {
        InitializeComponent();

        _inputTextBox = this.FindControl<TextBox>("InputTextBox");

        // 창이 열리면 입력 필드에 포커스 + 한글 전환
        Opened += (s, e) =>
        {
            _inputTextBox?.Focus();
            TrySwitchToKorean();
        };

        // TextChanged 이벤트 연결
        if (_inputTextBox != null)
        {
            _inputTextBox.TextChanged += OnInputTextChanged;
        }

        // DataContext 변경 시 ViewModel 이벤트 구독
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
        // 초기화 중이면 무시
        if (_isClearing) return;

        if (_inputTextBox != null && _viewModel != null)
        {
            var text = _inputTextBox.Text ?? "";
            _viewModel.OnTextChanged(text);
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

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void TrySwitchToKorean()
    {
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
