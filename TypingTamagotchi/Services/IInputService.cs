using System;

namespace TypingTamagotchi.Services;

public interface IInputService
{
    event Action? InputDetected;
    void Start();
    void Stop();
}
