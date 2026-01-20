using System;

namespace TypingTamagotchi.Models;

public class CollectionEntry
{
    public int Id { get; set; }
    public int CreatureId { get; set; }
    public DateTime ObtainedAt { get; set; }
}
