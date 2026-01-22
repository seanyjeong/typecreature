namespace TypingTamagotchi.Models;

public class Egg
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SpritePath { get; set; } = string.Empty;
    public int RequiredCount { get; set; }  // 500~2000 랜덤
    public double CurrentCount { get; set; }  // 타이핑 1타 = 1, 시간 초당 = 0.1

    public double Progress => RequiredCount > 0
        ? (double)CurrentCount / RequiredCount
        : 0;

    public bool IsReady => CurrentCount >= RequiredCount;
}
