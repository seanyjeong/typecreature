using System;
using System.Timers;
using Timer = System.Timers.Timer;

namespace TypingTamagotchi.Services;

public class SimulatedInputService : IInputService
{
    private Timer? _timer;

    public event Action<int>? InputDetected;
    private Random _random = new();

    public void Start()
    {
        _timer = new Timer(100); // 0.1초마다 입력 시뮬레이션
        _timer.Elapsed += OnTimerElapsed;
        _timer.Start();
    }

    private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        // 랜덤 키 코드 (A-Z: 65-90)
        InputDetected?.Invoke(_random.Next(65, 91));
    }

    public void Stop()
    {
        _timer?.Stop();
        _timer?.Dispose();
        _timer = null;
    }
}
