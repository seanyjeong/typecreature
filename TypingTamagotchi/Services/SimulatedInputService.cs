using System;
using System.Timers;
using Timer = System.Timers.Timer;

namespace TypingTamagotchi.Services;

public class SimulatedInputService : IInputService
{
    private Timer? _timer;

    public event Action? InputDetected;

    public void Start()
    {
        _timer = new Timer(100); // 0.1초마다 입력 시뮬레이션
        _timer.Elapsed += OnTimerElapsed;
        _timer.Start();
    }

    private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        InputDetected?.Invoke();
    }

    public void Stop()
    {
        _timer?.Stop();
        _timer?.Dispose();
        _timer = null;
    }
}
