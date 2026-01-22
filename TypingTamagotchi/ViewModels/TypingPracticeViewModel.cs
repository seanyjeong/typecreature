using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Timers;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TypingTamagotchi.Services;

namespace TypingTamagotchi.ViewModels;

public partial class TypingPracticeViewModel : ViewModelBase
{
    private readonly DatabaseService _db;
    private readonly HatchingService _hatching;
    private readonly List<string> _sentences = new();
    private readonly Random _random = new();
    private readonly Timer _cpmTimer;

    private string _currentSentence = "";
    private DateTime _startTime;
    private int _totalCharsTyped = 0;
    private int _correctChars = 0;
    private int _totalChars = 0;
    private bool _isTyping = false;

    private const double PROGRESS_BAR_MAX_WIDTH = 540.0;

    [ObservableProperty]
    private string _userInput = "";

    [ObservableProperty]
    private ObservableCollection<CharDisplay> _displayChars = new();

    [ObservableProperty]
    private int _currentCPM = 0;

    [ObservableProperty]
    private string _accuracyText = "100%";

    [ObservableProperty]
    private int _completedCount = 0;

    [ObservableProperty]
    private string _hatchContribution = "+0";

    [ObservableProperty]
    private double _progressWidth = 0;

    // 부화 기여도 (타이핑 연습에서 입력한 글자 수)
    private int _hatchContributionCount = 0;

    // 문장 완료 이벤트
    public event Action? SentenceCompleted;

    public TypingPracticeViewModel()
    {
        _db = new DatabaseService();
        _hatching = new HatchingService(_db);

        LoadSentences();
        NextSentence();

        // CPM 업데이트 타이머 (1초마다)
        _cpmTimer = new Timer(1000);
        _cpmTimer.Elapsed += (s, e) => Dispatcher.UIThread.Post(UpdateCPM);
        _cpmTimer.Start();
    }

    private void LoadSentences()
    {
        try
        {
            var uri = new Uri("avares://TypingTamagotchi/Assets/typing_sentences.json");
            using var stream = AssetLoader.Open(uri);
            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();

            var data = JsonSerializer.Deserialize<SentenceData>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (data?.Sentences != null)
            {
                _sentences.AddRange(data.Sentences);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load sentences: {ex.Message}");
            // 기본 문장 추가
            _sentences.Add("가는 말이 고와야 오는 말이 곱다");
            _sentences.Add("천 리 길도 한 걸음부터");
        }
    }

    private void NextSentence()
    {
        if (_sentences.Count == 0) return;

        _currentSentence = _sentences[_random.Next(_sentences.Count)];
        UserInput = "";
        _startTime = DateTime.Now;
        _isTyping = false;

        UpdateDisplayChars();
    }

    private void UpdateDisplayChars()
    {
        DisplayChars.Clear();

        for (int i = 0; i < _currentSentence.Length; i++)
        {
            var charDisplay = new CharDisplay
            {
                Char = _currentSentence[i].ToString(),
                Color = new SolidColorBrush(Color.Parse("#666666")) // 미입력: 회색
            };

            if (i < UserInput.Length)
            {
                if (UserInput[i] == _currentSentence[i])
                {
                    charDisplay.Color = new SolidColorBrush(Color.Parse("#4CAF50")); // 정확: 녹색
                }
                else
                {
                    charDisplay.Color = new SolidColorBrush(Color.Parse("#E57373")); // 오타: 빨강
                }
            }
            else if (i == UserInput.Length)
            {
                charDisplay.Color = new SolidColorBrush(Color.Parse("#FFFFFF")); // 현재 위치: 흰색
            }

            DisplayChars.Add(charDisplay);
        }

        // 진행률 업데이트
        var progress = _currentSentence.Length > 0
            ? (double)UserInput.Length / _currentSentence.Length
            : 0;
        ProgressWidth = progress * PROGRESS_BAR_MAX_WIDTH;
    }

    partial void OnUserInputChanged(string value)
    {
        if (!_isTyping && value.Length > 0)
        {
            _isTyping = true;
            _startTime = DateTime.Now;
        }

        UpdateDisplayChars();

        // 정확도 계산
        int correct = 0;
        int total = Math.Min(value.Length, _currentSentence.Length);
        for (int i = 0; i < total; i++)
        {
            if (value[i] == _currentSentence[i])
                correct++;
        }
        _correctChars += (value.Length > 0 ? 1 : 0);
        _totalChars += (value.Length > 0 ? 1 : 0);

        if (value.Length > 0)
        {
            var accuracy = (double)correct / value.Length * 100;
            AccuracyText = $"{accuracy:F0}%";
        }

        // 문장 완료 체크
        if (value == _currentSentence)
        {
            OnSentenceComplete();
        }
    }

    private void OnSentenceComplete()
    {
        CompletedCount++;
        _totalCharsTyped += _currentSentence.Length;

        // 부화 기여도 증가 (타이핑한 글자 수만큼)
        _hatchContributionCount += _currentSentence.Length;
        HatchContribution = $"+{_hatchContributionCount}";

        // 실제 부화 게이지에 반영 (글자당 1 입력으로 계산)
        for (int i = 0; i < _currentSentence.Length; i++)
        {
            _hatching.RecordInput(isClick: false);
        }

        SentenceCompleted?.Invoke();
        NextSentence();
    }

    private void UpdateCPM()
    {
        if (!_isTyping || UserInput.Length == 0)
        {
            CurrentCPM = 0;
            return;
        }

        var elapsed = (DateTime.Now - _startTime).TotalMinutes;
        if (elapsed > 0)
        {
            // 현재 문장에서의 CPM + 완료한 문장들의 CPM
            var totalChars = _totalCharsTyped + UserInput.Length;
            CurrentCPM = (int)(totalChars / elapsed);
        }
    }

    [RelayCommand]
    private void Skip()
    {
        NextSentence();
    }

    public void Cleanup()
    {
        _cpmTimer.Stop();
        _cpmTimer.Dispose();
    }
}

public class CharDisplay
{
    public string Char { get; set; } = "";
    public IBrush Color { get; set; } = new SolidColorBrush(Colors.White);
}

public class SentenceData
{
    public List<string>? Sentences { get; set; }
}
