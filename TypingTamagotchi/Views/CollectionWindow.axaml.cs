using System;
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

    private void OnToggleDisplayClick(object? sender, RoutedEventArgs e)
    {
        Console.WriteLine("[CollectionWindow] 버튼 클릭됨!");

        if (sender is Button button && button.Tag is CollectionItem item)
        {
            Console.WriteLine($"[CollectionWindow] 크리처: {item.Creature.Name}");

            if (DataContext is CollectionViewModel vm)
            {
                vm.ToggleDisplayCommand.Execute(item);
            }
        }
    }
}
