namespace TypingTamagotchi.Models;

public class Creature
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Rarity Rarity { get; set; }
    public Element Element { get; set; }
    public string SpritePath { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // 상세 정보
    public string Age { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public string FavoriteFood { get; set; } = string.Empty;
    public string Dislikes { get; set; } = string.Empty;
    public string Background { get; set; } = string.Empty;

    // 속성 한글 이름
    public string ElementName => Element switch
    {
        Element.Fire => "불꽃",
        Element.Water => "물",
        Element.Wind => "바람",
        Element.Earth => "대지",
        Element.Lightning => "번개",
        _ => "???"
    };

    // 속성 이모지
    public string ElementEmoji => Element switch
    {
        Element.Fire => "🔥",
        Element.Water => "💧",
        Element.Wind => "🌿",
        Element.Earth => "🪨",
        Element.Lightning => "⚡",
        _ => "❓"
    };
}
