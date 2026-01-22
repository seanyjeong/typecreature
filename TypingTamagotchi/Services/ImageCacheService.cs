using System;
using System.Collections.Generic;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace TypingTamagotchi.Services;

public class ImageCacheService
{
    private static readonly Lazy<ImageCacheService> _instance = new(() => new ImageCacheService());
    public static ImageCacheService Instance => _instance.Value;

    private readonly Dictionary<string, Bitmap> _thumbnailCache = new();
    private readonly object _lock = new();

    private ImageCacheService() { }

    /// <summary>
    /// 썸네일 이미지 로드 (캐시 사용)
    /// </summary>
    public Bitmap? GetThumbnail(string spritePath)
    {
        if (string.IsNullOrEmpty(spritePath)) return null;

        // Creatures/1.png → Creatures/thumbs/1.png
        var thumbPath = spritePath.Replace("Creatures/", "Creatures/thumbs/");

        lock (_lock)
        {
            if (_thumbnailCache.TryGetValue(thumbPath, out var cached))
            {
                return cached;
            }
        }

        try
        {
            var uri = new Uri($"avares://TypingTamagotchi/Assets/{thumbPath}");
            var bitmap = new Bitmap(AssetLoader.Open(uri));

            lock (_lock)
            {
                _thumbnailCache[thumbPath] = bitmap;
            }

            return bitmap;
        }
        catch
        {
            // 썸네일 없으면 원본 시도
            return GetOriginal(spritePath);
        }
    }

    /// <summary>
    /// 원본 이미지 로드 (캐시 안 함 - 상세보기용)
    /// </summary>
    public Bitmap? GetOriginal(string spritePath)
    {
        if (string.IsNullOrEmpty(spritePath)) return null;

        try
        {
            var uri = new Uri($"avares://TypingTamagotchi/Assets/{spritePath}");
            return new Bitmap(AssetLoader.Open(uri));
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 캐시 초기화 (메모리 해제)
    /// </summary>
    public void ClearCache()
    {
        lock (_lock)
        {
            foreach (var bitmap in _thumbnailCache.Values)
            {
                bitmap.Dispose();
            }
            _thumbnailCache.Clear();
        }
    }
}
