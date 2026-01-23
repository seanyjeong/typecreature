using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
    private readonly List<string> _koreanSentences = new();
    private readonly List<EnglishSentence> _englishSentences = new();
    private readonly List<string> _codeSentences = new();
    private readonly Random _random = new();
    private readonly Timer _cpmTimer;
    private readonly Stopwatch _sentenceStopwatch = new();
    private readonly Stopwatch _activeTypingStopwatch = new(); // 실제 타이핑 시간만 측정

    private string _currentSentence = "";
    private string _currentTranslationText = "";
    private int _totalCharsTyped = 0;
    private int _totalCorrectChars = 0;
    private string _currentUserInput = "";
    private bool _sentenceCompleted = false;
    private bool _isPaused = false; // 부화 팝업 등으로 일시정지

    private const double PROGRESS_BAR_MAX_WIDTH = 540.0;

    [ObservableProperty]
    private ObservableCollection<CharDisplay> _displayChars = new();

    [ObservableProperty]
    private int _currentCPM = 0;

    [ObservableProperty]
    private int _averageCPM = 0;

    [ObservableProperty]
    private int _maxCPM = 0;

    [ObservableProperty]
    private string _accuracyText = "100%";

    [ObservableProperty]
    private int _completedCount = 0;

    [ObservableProperty]
    private string _hatchContribution = "+0 (0%)";

    [ObservableProperty]
    private double _progressWidth = 0;

    [ObservableProperty]
    private string _currentSentenceText = "";

    [ObservableProperty]
    private bool _isEnglishMode = false;

    [ObservableProperty]
    private string _languageButtonText = "🇺🇸 English";

    [ObservableProperty]
    private string _instructionText = "💡 문장을 입력하고 Enter를 누르면 다음 문장으로 넘어갑니다";

    [ObservableProperty]
    private bool _isReadyForNext = false;

    [ObservableProperty]
    private bool _isCodeMode = false;

    [ObservableProperty]
    private bool _showEnglishSubToggle = false;

    [ObservableProperty]
    private string _currentTranslation = "";

    [ObservableProperty]
    private bool _showTranslation = false;

    private int _hatchContributionCount = 0;
    private int _requiredForHatch = 1000; // 부화에 필요한 입력 수 (예시)

    public event Action? SentenceCompleted;
    public event Action? ClearInputRequested;
    public event Action<int, int, string>? SessionEnded; // avgCPM, completedCount, accuracy

    public TypingPracticeViewModel()
    {
        _db = new DatabaseService();
        _hatching = new HatchingService(_db);
        _hatching.CreatureHatched += OnCreatureHatched;

        LoadSentences();

        // 현재 알 상태에서 필요 입력 수 가져오기
        var currentEgg = _db.GetCurrentEgg();
        if (currentEgg != null)
        {
            _requiredForHatch = currentEgg.RequiredCount - (int)currentEgg.CurrentCount;
            if (_requiredForHatch < 100) _requiredForHatch = 100;
        }

        NextSentence();

        _cpmTimer = new Timer(100); // 100ms로 더 정확한 측정
        _cpmTimer.Elapsed += (s, e) => Dispatcher.UIThread.Post(UpdateCPM);
        _cpmTimer.Start();
    }

    private void LoadSentences()
    {
        // 한글 문장 로드
        try
        {
            var uri = new Uri("avares://TypingTamagotchi/Assets/typing_sentences.json");
            using var stream = AssetLoader.Open(uri);
            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();
            var data = JsonSerializer.Deserialize<SentenceData>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (data?.Sentences != null) _koreanSentences.AddRange(data.Sentences);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load Korean sentences: {ex.Message}");
            _koreanSentences.Add("가는 말이 고와야 오는 말이 곱다");
            _koreanSentences.Add("천 리 길도 한 걸음부터");
        }

        // 영어 문장 로드 (번역 포함)
        try
        {
            var uri = new Uri("avares://TypingTamagotchi/Assets/typing_sentences_en.json");
            using var stream = AssetLoader.Open(uri);
            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();
            var data = JsonSerializer.Deserialize<EnglishSentenceData>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (data?.Sentences != null) _englishSentences.AddRange(data.Sentences);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load English sentences: {ex.Message}");
            _englishSentences.Add(new EnglishSentence { En = "Actions speak louder than words.", Ko = "행동이 말보다 중요하다." });
            _englishSentences.Add(new EnglishSentence { En = "Practice makes perfect.", Ko = "연습이 완벽을 만든다." });
        }

        // 코드 문장 로드
        try
        {
            var uri = new Uri("avares://TypingTamagotchi/Assets/typing_sentences_code.json");
            using var stream = AssetLoader.Open(uri);
            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();
            var data = JsonSerializer.Deserialize<SentenceData>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (data?.Sentences != null) _codeSentences.AddRange(data.Sentences);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load code sentences: {ex.Message}");
            _codeSentences.Add("const sum = (a, b) => a + b;");
            _codeSentences.Add("function hello() { return 'world'; }");
        }
    }

    private void NextSentence()
    {
        if (IsEnglishMode && !IsCodeMode)
        {
            // 영어 격언 모드 (번역 포함)
            if (_englishSentences.Count == 0) return;
            var sentence = _englishSentences[_random.Next(_englishSentences.Count)];
            _currentSentence = sentence.En;
            _currentTranslationText = sentence.Ko;
            CurrentTranslation = sentence.Ko;
            ShowTranslation = true;
        }
        else if (IsEnglishMode && IsCodeMode)
        {
            // 코드 모드
            if (_codeSentences.Count == 0) return;
            _currentSentence = _codeSentences[_random.Next(_codeSentences.Count)];
            _currentTranslationText = "";
            CurrentTranslation = "";
            ShowTranslation = false;
        }
        else
        {
            // 한글 모드
            if (_koreanSentences.Count == 0) return;
            _currentSentence = _koreanSentences[_random.Next(_koreanSentences.Count)];
            _currentTranslationText = "";
            CurrentTranslation = "";
            ShowTranslation = false;
        }

        CurrentSentenceText = _currentSentence;
        _currentUserInput = "";
        _sentenceCompleted = false;
        IsReadyForNext = false;

        _sentenceStopwatch.Reset();

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
                Color = new SolidColorBrush(Color.Parse("#666666"))
            };

            if (i < userInput.Length)
            {
                if (i < _currentSentence.Length && userInput[i] == _currentSentence[i])
                {
                    charDisplay.Color = new SolidColorBrush(Color.Parse("#4CAF50"));
                }
                else
                {
                    charDisplay.Color = new SolidColorBrush(Color.Parse("#E57373"));
                }
            }
            else if (i == userInput.Length)
            {
                charDisplay.Color = new SolidColorBrush(Color.Parse("#FFFFFF"));
            }

            DisplayChars.Add(charDisplay);
        }

        var progress = _currentSentence.Length > 0
            ? Math.Min(1.0, (double)userInput.Length / _currentSentence.Length)
            : 0;
        ProgressWidth = progress * PROGRESS_BAR_MAX_WIDTH;
    }

    public void OnTextChanged(string userInput)
    {
        if (_sentenceCompleted || _isPaused) return;

        // 첫 입력 시 타이머 시작
        if (_currentUserInput.Length == 0 && userInput.Length > 0)
        {
            _sentenceStopwatch.Start();
            if (!_activeTypingStopwatch.IsRunning)
                _activeTypingStopwatch.Start();
            CurrentCPM = 0;
        }
        _currentUserInput = userInput;

        UpdateDisplayChars(userInput);

        // 정확도 계산 (실시간 누적 - 이전 문장들 + 현재 문장)
        if (userInput.Length > 0)
        {
            int correctInCurrent = 0;
            int totalInCurrent = Math.Min(userInput.Length, _currentSentence.Length);
            for (int i = 0; i < totalInCurrent; i++)
            {
                if (userInput[i] == _currentSentence[i])
                    correctInCurrent++;
            }

            // 누적 정확도: (이전 문장들의 정답 + 현재 문장 정답) / (이전 문장들 총 글자 + 현재 입력 글자)
            int totalCorrect = _totalCorrectChars + correctInCurrent;
            int totalTyped = _totalCharsTyped + userInput.Length;
            var accuracy = totalTyped > 0 ? (double)totalCorrect / totalTyped * 100 : 100;
            AccuracyText = $"{accuracy:F0}%";
        }
        else if (_totalCharsTyped > 0)
        {
            // 입력 없을 때는 이전 누적 정확도 유지
            var accuracy = (double)_totalCorrectChars / _totalCharsTyped * 100;
            AccuracyText = $"{accuracy:F0}%";
        }

        // 문장 완료 체크 - Enter 대기 안내 표시
        if (userInput == _currentSentence)
        {
            _sentenceCompleted = true;
            IsReadyForNext = true;
        }
    }

    public void OnEnterPressed()
    {
        // 입력이 있으면 엔터로 다음 문장 이동 (정확도는 떨어질 수 있음)
        if (_currentUserInput.Length > 0)
        {
            // 문장 완료 처리
            _sentenceStopwatch.Stop();
            _activeTypingStopwatch.Stop();

            // 완벽하게 맞춘 경우에만 최고 타수 업데이트
            if (_currentUserInput == _currentSentence && CurrentCPM > MaxCPM)
            {
                MaxCPM = CurrentCPM;
            }

            OnSentenceComplete(_currentUserInput);
        }
    }

    private void OnSentenceComplete(string userInput)
    {
        CompletedCount++;

        int correctInSentence = 0;
        for (int i = 0; i < _currentSentence.Length; i++)
        {
            if (i < userInput.Length && userInput[i] == _currentSentence[i])
                correctInSentence++;
        }

        _totalCharsTyped += _currentSentence.Length;
        _totalCorrectChars += correctInSentence;

        _hatchContributionCount += _currentSentence.Length;
        var xpPercent = Math.Min(100, (double)_hatchContributionCount / _requiredForHatch * 100);
        HatchContribution = $"+{_hatchContributionCount} ({xpPercent:F1}%)";

        // 부화 게이지에 반영
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
        if (_isPaused) return; // 부화 팝업 중에는 타수 업데이트 안 함

        // 현재 문장 타수 (Stopwatch로 더 정확하게)
        if (_currentUserInput.Length > 0 && _sentenceStopwatch.IsRunning)
        {
            var elapsedMinutes = _sentenceStopwatch.Elapsed.TotalMinutes;
            if (elapsedMinutes > 0.005) // 최소 0.3초
            {
                CurrentCPM = (int)(_currentUserInput.Length / elapsedMinutes);
            }
        }

        // 평균 타수 - 실제 타이핑 시간만 사용 (대기 시간 제외)
        var activeMinutes = _activeTypingStopwatch.Elapsed.TotalMinutes;
        if (activeMinutes > 0.01 && _totalCharsTyped > 0)
        {
            AverageCPM = (int)(_totalCharsTyped / activeMinutes);
        }
    }

    [RelayCommand]
    private void ToggleLanguage()
    {
        IsEnglishMode = !IsEnglishMode;
        LanguageButtonText = IsEnglishMode ? "🇰🇷 한글" : "🇺🇸 English";
        ShowEnglishSubToggle = IsEnglishMode;
        InstructionText = IsEnglishMode
            ? (IsCodeMode ? "💡 Type the code and press Enter to continue" : "💡 Type the sentence and press Enter to continue")
            : "💡 문장을 입력하고 Enter를 누르면 다음 문장으로 넘어갑니다";
        NextSentence();
    }

    [RelayCommand]
    private void ToggleCodeMode()
    {
        IsCodeMode = !IsCodeMode;
        InstructionText = IsCodeMode
            ? "💡 Type the code and press Enter to continue"
            : "💡 Type the sentence and press Enter to continue";
        NextSentence();
    }

    [RelayCommand]
    private void Skip()
    {
        NextSentence();
    }

    public (int avgCPM, int completed, string accuracy, string hatchContrib) GetSessionSummary()
    {
        return (AverageCPM, CompletedCount, AccuracyText, HatchContribution);
    }

    public void EndSession()
    {
        SessionEnded?.Invoke(AverageCPM, CompletedCount, AccuracyText);
    }

    private void OnCreatureHatched(TypingTamagotchi.Models.Creature creature)
    {
        // 부화 팝업 뜨면 타이머 멈춤
        _isPaused = true;
        _sentenceStopwatch.Stop();
        _activeTypingStopwatch.Stop();
    }

    public void Resume()
    {
        // 부화 팝업 닫으면 재개
        _isPaused = false;
        // 타이머는 다음 문장 첫 입력 시 자동으로 재시작됨
    }

    public void Cleanup()
    {
        _hatching.CreatureHatched -= OnCreatureHatched;
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

public class EnglishSentence
{
    public string En { get; set; } = "";
    public string Ko { get; set; } = "";
}

public class EnglishSentenceData
{
    public List<EnglishSentence>? Sentences { get; set; }
}
