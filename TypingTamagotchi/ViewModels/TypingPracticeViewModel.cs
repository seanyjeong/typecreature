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
    private DateTime _sessionStartTime;
    private int _totalCharsTyped = 0;
    private int _totalCorrectChars = 0;
    private int _totalTypedChars = 0;

    private const double PROGRESS_BAR_MAX_WIDTH = 540.0;

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

    [ObservableProperty]
    private string _currentSentenceText = "";

    // 부화 기여도 (타이핑 연습에서 입력한 글자 수)
    private int _hatchContributionCount = 0;

    // 문장 완료 이벤트
    public event Action? SentenceCompleted;

    // 입력 필드 초기화 요청 이벤트
    public event Action? ClearInputRequested;

    public TypingPracticeViewModel()
    {
        _db = new DatabaseService();
        _hatching = new HatchingService(_db);

        LoadSentences();
        _sessionStartTime = DateTime.Now;
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
        CurrentSentenceText = _currentSentence;

        // 입력 필드 초기화 요청
        ClearInputRequested?.Invoke();

        UpdateDisplayChars("");
    }

    private void UpdateDisplayChars(string userInput)
    {
        DisplayChars.Clear();

        for (int i = 0; i < _currentSentence.Length; i++)
        {
            var charDisplay = new CharDisplay
            {
                Char = _currentSentence[i].ToString(),
                Color = new SolidColorBrush(Color.Parse("#666666")) // 미입력: 회색
            };

            if (i < userInput.Length)
            {
                if (i < _currentSentence.Length && userInput[i] == _currentSentence[i])
                {
                    charDisplay.Color = new SolidColorBrush(Color.Parse("#4CAF50")); // 정확: 녹색
                }
                else
                {
                    charDisplay.Color = new SolidColorBrush(Color.Parse("#E57373")); // 오타: 빨강
                }
            }
            else if (i == userInput.Length)
            {
                charDisplay.Color = new SolidColorBrush(Color.Parse("#FFFFFF")); // 현재 위치: 흰색
            }

            DisplayChars.Add(charDisplay);
        }

        // 진행률 업데이트
        var progress = _currentSentence.Length > 0
            ? Math.Min(1.0, (double)userInput.Length / _currentSentence.Length)
            : 0;
        ProgressWidth = progress * PROGRESS_BAR_MAX_WIDTH;
    }

    // View에서 호출 - 텍스트 변경 시
    public void OnTextChanged(string userInput)
    {
        UpdateDisplayChars(userInput);

        // 정확도 계산 (현재 입력 기준)
        if (userInput.Length > 0)
        {
            int correct = 0;
            int total = Math.Min(userInput.Length, _currentSentence.Length);
            for (int i = 0; i < total; i++)
            {
                if (userInput[i] == _currentSentence[i])
                    correct++;
            }
            var accuracy = total > 0 ? (double)correct / total * 100 : 100;
            AccuracyText = $"{accuracy:F0}%";

            // 전체 통계 업데이트
            _totalTypedChars = _totalCharsTyped + userInput.Length;
        }

        // 문장 완료 체크 (정확히 일치할 때만)
        if (userInput == _currentSentence)
        {
            OnSentenceComplete(userInput);
        }
    }

    private void OnSentenceComplete(string userInput)
    {
        CompletedCount++;

        // 정확하게 입력한 글자 수 계산
        int correctInSentence = 0;
        for (int i = 0; i < _currentSentence.Length; i++)
        {
            if (i < userInput.Length && userInput[i] == _currentSentence[i])
                correctInSentence++;
        }

        _totalCharsTyped += _currentSentence.Length;
        _totalCorrectChars += correctInSentence;

        // 부화 기여도 증가 (타이핑한 글자 수만큼)
        _hatchContributionCount += _currentSentence.Length;
        HatchContribution = $"+{_hatchContributionCount}";

        // 실제 부화 게이지에 반영 (글자당 1 입력으로 계산)
        for (int i = 0; i < _currentSentence.Length; i++)
        {
            _hatching.RecordInput(isClick: false);
        }

        // 전체 정확도 업데이트
        if (_totalCharsTyped > 0)
        {
            var totalAccuracy = (double)_totalCorrectChars / _totalCharsTyped * 100;
            AccuracyText = $"{totalAccuracy:F0}%";
        }

        SentenceCompleted?.Invoke();
        NextSentence();
    }

    private void UpdateCPM()
    {
        var elapsed = (DateTime.Now - _sessionStartTime).TotalMinutes;
        if (elapsed > 0.01 && _totalCharsTyped > 0) // 최소 0.6초
        {
            CurrentCPM = (int)(_totalCharsTyped / elapsed);
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
