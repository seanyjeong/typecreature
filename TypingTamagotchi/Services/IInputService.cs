using System;

namespace TypingTamagotchi.Services;

public interface IInputService
{
    event Action<int>? InputDetected; // int = virtual key code (0 for mouse clicks)
    void Start();
    void Stop();
}
