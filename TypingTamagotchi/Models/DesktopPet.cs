namespace TypingTamagotchi.Models;

public class DesktopPet
{
    public int Id { get; set; }
    public Creature Creature { get; set; } = null!;
    public double X { get; set; }
    public double Y { get; set; }
    public PetState State { get; set; } = PetState.Idle;
    public bool FacingRight { get; set; } = true;
    public double StateTimer { get; set; }
    public double AnimationFrame { get; set; }
}
