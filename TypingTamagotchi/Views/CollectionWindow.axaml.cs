using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
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
                    : item.IsInDisplay
                        ? $"'{item.Creature.Name}'을(를) 진열장에 추가했습니다!"
                        : "진열장이 가득 찼습니다!";

                var msgBox = new Window
                {
                    Title = "알림",
                    Width = 320,
                    Height = 140,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    SystemDecorations = SystemDecorations.None,
                    Background = Brushes.Transparent
                };

                var border = new Border
                {
                    CornerRadius = new Avalonia.CornerRadius(12),
                    Padding = new Avalonia.Thickness(20),
                    Background = new LinearGradientBrush
                    {
                        StartPoint = new Avalonia.RelativePoint(0, 0, Avalonia.RelativeUnit.Relative),
                        EndPoint = new Avalonia.RelativePoint(0, 1, Avalonia.RelativeUnit.Relative),
                        GradientStops = new GradientStops
                        {
                            new GradientStop(Color.Parse("#2C3E50"), 0),
                            new GradientStop(Color.Parse("#1A252F"), 1)
                        }
                    },
                    BorderBrush = new SolidColorBrush(Color.Parse("#4A5568")),
                    BorderThickness = new Avalonia.Thickness(2)
                };

                var stack = new StackPanel { Spacing = 15 };

                stack.Children.Add(new TextBlock
                {
                    Text = message,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    FontSize = 14,
                    Foreground = Brushes.White,
                    TextWrapping = TextWrapping.Wrap,
                    TextAlignment = TextAlignment.Center
                });

                var closeBtn = new Button
                {
                    Content = "확인",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Padding = new Avalonia.Thickness(30, 8),
                    Background = new SolidColorBrush(Color.Parse("#4A6FA5")),
                    Foreground = Brushes.White,
                    FontWeight = FontWeight.Bold
                };
                closeBtn.Click += (s, args) => msgBox.Close();
                stack.Children.Add(closeBtn);

                border.Child = stack;
                msgBox.Content = border;
                await msgBox.ShowDialog(this);
            }
        }
    }

    private async void OnTogglePlaygroundClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is CollectionItem item)
        {
            if (DataContext is CollectionViewModel vm)
            {
                var wasInPlayground = item.IsInPlayground;
                vm.TogglePlaygroundCommand.Execute(item);

                // 결과 메시지 표시
                var message = wasInPlayground
                    ? $"'{item.Creature.Name}'을(를) 놀이터에서 데려왔습니다."
                    : item.IsInPlayground
                        ? $"'{item.Creature.Name}'을(를) 놀이터로 보냈습니다!"
                        : "놀이터가 가득 찼습니다! (최대 4마리)";

                var msgBox = new Window
                {
                    Title = "알림",
                    Width = 320,
                    Height = 140,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    SystemDecorations = SystemDecorations.None,
                    Background = Brushes.Transparent
                };

                var border = new Border
                {
                    CornerRadius = new Avalonia.CornerRadius(12),
                    Padding = new Avalonia.Thickness(20),
                    Background = new LinearGradientBrush
                    {
                        StartPoint = new Avalonia.RelativePoint(0, 0, Avalonia.RelativeUnit.Relative),
                        EndPoint = new Avalonia.RelativePoint(0, 1, Avalonia.RelativeUnit.Relative),
                        GradientStops = new GradientStops
                        {
                            new GradientStop(Color.Parse("#2C3E50"), 0),
                            new GradientStop(Color.Parse("#1A252F"), 1)
                        }
                    },
                    BorderBrush = new SolidColorBrush(Color.Parse("#4A5568")),
                    BorderThickness = new Avalonia.Thickness(2)
                };

                var stack = new StackPanel { Spacing = 15 };

                stack.Children.Add(new TextBlock
                {
                    Text = message,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    FontSize = 14,
                    Foreground = Brushes.White,
                    TextWrapping = TextWrapping.Wrap,
                    TextAlignment = TextAlignment.Center
                });

                var closeBtn = new Button
                {
                    Content = "확인",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Padding = new Avalonia.Thickness(30, 8),
                    Background = new SolidColorBrush(Color.Parse("#90EE90")),
                    Foreground = Brushes.Black,
                    FontWeight = FontWeight.Bold
                };
                closeBtn.Click += (s, args) => msgBox.Close();
                stack.Children.Add(closeBtn);

                border.Child = stack;
                msgBox.Content = border;
                await msgBox.ShowDialog(this);
            }
        }
    }
}
