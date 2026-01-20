using Avalonia.Controls;
using Avalonia.Interactivity;
using TypingTamagotchi.ViewModels;

namespace TypingTamagotchi.Views;

public partial class CollectionWindow : Window
{
    public CollectionWindow()
    {
        InitializeComponent();
        // DataContext is set by App.axaml.cs
    }

    private async void OnToggleDisplayClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is CollectionItem item)
        {
            if (DataContext is CollectionViewModel vm)
            {
                var wasInDisplay = item.IsInDisplay;
                vm.ToggleDisplayCommand.Execute(item);

                // 결과 메시지 표시
                var message = wasInDisplay
                    ? $"'{item.Creature.Name}'을(를) 진열장에서 제거했습니다."
                    : $"'{item.Creature.Name}'을(를) 진열장에 추가했습니다!";

                var msgBox = new Window
                {
                    Title = "알림",
                    Width = 300,
                    Height = 100,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Content = new TextBlock
                    {
                        Text = message,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                        FontSize = 14
                    }
                };
                await msgBox.ShowDialog(this);
            }
        }
    }
}
