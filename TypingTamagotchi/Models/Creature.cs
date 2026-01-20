namespace TypingTamagotchi.Models;

public class Creature
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Rarity Rarity { get; set; }
    public string SpritePath { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
