using System.Runtime.InteropServices;

namespace TypingTamagotchi.Services;

/// <summary>
/// 플랫폼에 맞는 InputService를 생성하는 팩토리
/// </summary>
public static class InputServiceFactory
{
    /// <summary>
    /// 현재 플랫폼에 맞는 InputService를 생성
    /// </summary>
    /// <param name="useSimulated">true면 시뮬레이션 모드 (개발/테스트용)</param>
    public static IInputService Create(bool useSimulated = false)
    {
        if (useSimulated)
        {
            return new SimulatedInputService();
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return new WindowsInputService();
        }

        // Linux, macOS 등은 시뮬레이션 모드로 폴백
        // TODO: Linux용 X11 후킹 구현
        return new SimulatedInputService();
    }
}
