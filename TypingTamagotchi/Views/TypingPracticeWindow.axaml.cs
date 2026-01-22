using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using TypingTamagotchi.ViewModels;

namespace TypingTamagotchi.Views;

public partial class TypingPracticeWindow : Window
{
    public TypingPracticeWindow()
    {
        InitializeComponent();

        // 창이 열리면 입력 필드에 포커스
        Opened += (s, e) =>
        {
            var textBox = this.FindControl<TextBox>("InputTextBox");
            textBox?.Focus();
        };
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
