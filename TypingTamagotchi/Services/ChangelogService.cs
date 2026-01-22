using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using Avalonia.Platform;

namespace TypingTamagotchi.Services;

public class ChangelogService
{
    private readonly DatabaseService _db;
    private ChangelogData? _changelog;

    public ChangelogService(DatabaseService db)
    {
        _db = db;
        LoadChangelog();
    }

    private void LoadChangelog()
    {
        try
        {
            var uri = new Uri("avares://TypingTamagotchi/Assets/changelog.json");
            using var stream = AssetLoader.Open(uri);
            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();
            _changelog = JsonSerializer.Deserialize<ChangelogData>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load changelog: {ex.Message}");
        }
    }

    public string GetCurrentVersion()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        return version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "1.0.0";
    }

    public string? GetLastSeenVersion()
    {
        using var connection = _db.GetConnection();
        var command = connection.CreateCommand();
        command.CommandText = "SELECT value FROM settings WHERE key = 'last_seen_version'";
        var result = command.ExecuteScalar();
        return result?.ToString();
    }

    public void SetLastSeenVersion(string version)
    {
        using var connection = _db.GetConnection();
        var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT OR REPLACE INTO settings (key, value)
            VALUES ('last_seen_version', @version)
        ";
        command.Parameters.AddWithValue("@version", version);
        command.ExecuteNonQuery();
    }

    public bool HasNewChangelog()
    {
        var currentVersion = GetCurrentVersion();
        var lastSeenVersion = GetLastSeenVersion();

        // 처음 실행이거나 버전이 다르면 true
        return lastSeenVersion == null || lastSeenVersion != currentVersion;
    }

    public VersionInfo? GetCurrentVersionInfo()
    {
        if (_changelog == null) return null;

        var currentVersion = GetCurrentVersion();
        return _changelog.Versions?.Find(v => v.Version == currentVersion);
    }

    public List<VersionInfo> GetNewVersions()
    {
        var result = new List<VersionInfo>();
        if (_changelog?.Versions == null) return result;

        var lastSeenVersion = GetLastSeenVersion();

        foreach (var version in _changelog.Versions)
        {
            // 마지막으로 본 버전보다 새로운 버전들만
            if (lastSeenVersion == null || CompareVersions(version.Version, lastSeenVersion) > 0)
            {
                result.Add(version);
            }
        }

        return result;
    }

    private int CompareVersions(string? v1, string? v2)
    {
        if (v1 == null && v2 == null) return 0;
        if (v1 == null) return -1;
        if (v2 == null) return 1;

        var parts1 = v1.Split('.');
        var parts2 = v2.Split('.');

        for (int i = 0; i < Math.Max(parts1.Length, parts2.Length); i++)
        {
            int p1 = i < parts1.Length && int.TryParse(parts1[i], out var n1) ? n1 : 0;
            int p2 = i < parts2.Length && int.TryParse(parts2[i], out var n2) ? n2 : 0;

            if (p1 != p2) return p1.CompareTo(p2);
        }
        return 0;
    }

    public void MarkChangelogSeen()
    {
        SetLastSeenVersion(GetCurrentVersion());
    }
}

public class ChangelogData
{
    public List<VersionInfo>? Versions { get; set; }
}

public class VersionInfo
{
    public string? Version { get; set; }
    public string? Date { get; set; }
    public string? Title { get; set; }
    public List<ChangeItem>? Changes { get; set; }
}

public class ChangeItem
{
    public string? Type { get; set; }  // feature, fix, improvement
    public string? Text { get; set; }
}
