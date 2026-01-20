namespace TypingTamagotchi.Models;

public class Egg
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SpritePath { get; set; } = string.Empty;
    public int RequiredCount { get; set; }  // 500~2000 랜덤
    public int CurrentCount { get; set; }

    public double Progress => RequiredCount > 0
        ? (double)CurrentCount / RequiredCount
        : 0;

    public bool IsReady => CurrentCount >= RequiredCount;
}
