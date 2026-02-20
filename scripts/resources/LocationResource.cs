using Godot;
using Godot.Collections;

/// <summary>
/// Represents a location the player can visit
/// </summary>
[GlobalClass]
public partial class LocationResource : Resource
{
    [Export] public string LocationId { get; set; } = "";
    [Export] public string LocationName { get; set; } = "";
    [Export(PropertyHint.MultilineText)] public string Description { get; set; } = "";
    [Export] public string ImagePath { get; set; } = "";
    
    /// <summary>
    /// Pool of event IDs that can trigger at this location
    /// </summary>
    [Export] public Array<string> EventPool { get; set; } = new();
    
    /// <summary>
    /// Cost in turns to investigate this location
    /// </summary>
    [Export] public int TurnCost { get; set; } = 1;
    
    /// <summary>
    /// Is this location available from the start?
    /// </summary>
    [Export] public bool UnlockedByDefault { get; set; } = true;

    /// <summary>
    /// Get a random event from this location's pool
    /// </summary>
    public string? GetRandomEvent()
    {
        if (EventPool.Count == 0)
            return null;
        
        int randomIndex = GD.RandRange(0, EventPool.Count - 1);
        return EventPool[randomIndex];
    }
}
