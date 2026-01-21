namespace TypingTamagotchi.Models;

public class Creature
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Rarity Rarity { get; set; }
    public string SpritePath { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // 상세 정보
    public string Age { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public string FavoriteFood { get; set; } = string.Empty;
    public string Dislikes { get; set; } = string.Empty;
    public string Background { get; set; } = string.Empty;
}
