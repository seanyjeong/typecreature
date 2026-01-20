using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using TypingTamagotchi.Models;

namespace TypingTamagotchi.ViewModels;

public class RarityToColorConverter : IValueConverter
{
    public static readonly RarityToColorConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Rarity rarity)
        {
            return rarity switch
            {
                Rarity.Common => new SolidColorBrush(Color.Parse("#E8F5E9")),
                Rarity.Rare => new SolidColorBrush(Color.Parse("#E3F2FD")),
                Rarity.Epic => new SolidColorBrush(Color.Parse("#F3E5F5")),
                Rarity.Legendary => new SolidColorBrush(Color.Parse("#FFF8E1")),
                _ => new SolidColorBrush(Colors.White)
            };
        }
        return new SolidColorBrush(Colors.White);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class OwnedToEmojiConverter : IValueConverter
{
    public static readonly OwnedToEmojiConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isOwned)
        {
            return isOwned ? "🐣" : "❓";
        }
        return "❓";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
