using Avalonia.Controls;
using Avalonia.Interactivity;
using TypingTamagotchi.ViewModels;

namespace TypingTamagotchi.Views;

public partial class TypingPracticeWindow : Window
{
    private TypingPracticeViewModel? _viewModel;
    private TextBox? _inputTextBox;
    private bool _isClearing = false; // 초기화 중 플래그

    public TypingPracticeWindow()
    {
        InitializeComponent();

        _inputTextBox = this.FindControl<TextBox>("InputTextBox");

        // 창이 열리면 입력 필드에 포커스
        Opened += (s, e) =>
        {
            _inputTextBox?.Focus();
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
}
