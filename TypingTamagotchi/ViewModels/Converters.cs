using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
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

public class WidgetButtonTextConverter : IValueConverter
{
    public static readonly WidgetButtonTextConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isVisible)
        {
            return isVisible ? "위젯 숨기기" : "미니 위젯";
        }
        return "미니 위젯";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class SpritePathToImageConverter : IValueConverter
{
    public static readonly SpritePathToImageConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string spritePath && !string.IsNullOrEmpty(spritePath))
        {
            // 썸네일 캐시 사용 (메모리 최적화)
            return Services.ImageCacheService.Instance.GetThumbnail(spritePath);
        }
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 원본 이미지용 컨버터 (상세보기에서 사용)
/// </summary>
public class SpritePathToOriginalImageConverter : IValueConverter
{
    public static readonly SpritePathToOriginalImageConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string spritePath && !string.IsNullOrEmpty(spritePath))
        {
            return Services.ImageCacheService.Instance.GetOriginal(spritePath);
        }
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class BoolToColorConverter : IValueConverter
{
    public static readonly BoolToColorConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isActive && isActive)
        {
            return new SolidColorBrush(Color.Parse("#FFD700")); // 금색 (활성화)
        }
        return new SolidColorBrush(Color.Parse("#E0E0E0")); // 회색 (비활성화)
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class BoolToPlaygroundColorConverter : IValueConverter
{
    public static readonly BoolToPlaygroundColorConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isActive && isActive)
        {
            return new SolidColorBrush(Color.Parse("#90EE90")); // 연두색 (활성화)
        }
        return new SolidColorBrush(Color.Parse("#E0E0E0")); // 회색 (비활성화)
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class ChangeTypeToColorConverter : IMultiValueConverter
{
    public static readonly ChangeTypeToColorConverter Instance = new();

    public object? Convert(System.Collections.Generic.IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count > 0 && values[0] is string type)
        {
            return type switch
            {
                "feature" => new SolidColorBrush(Color.Parse("#4CAF50")),  // 녹색
                "fix" => new SolidColorBrush(Color.Parse("#FF9800")),      // 주황색
                "improvement" => new SolidColorBrush(Color.Parse("#2196F3")), // 파란색
                _ => new SolidColorBrush(Color.Parse("#9E9E9E"))           // 회색
            };
        }
        return new SolidColorBrush(Color.Parse("#9E9E9E"));
    }
}

public class ChangeTypeToIconConverter : IValueConverter
{
    public static readonly ChangeTypeToIconConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string type)
        {
            return type switch
            {
                "feature" => "+",
                "fix" => "!",
                "improvement" => "^",
                _ => "•"
            };
        }
        return "•";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
