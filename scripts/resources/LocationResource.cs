using Godot;
using Godot.Collections;
using System.Collections.Generic;

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
    /// Controls display order in the location list. Lower values appear first.
    /// </summary>
    [Export] public int SortOrder { get; set; } = 0;

    /// <summary>
    /// Is this location available from the start?
    /// </summary>
    [Export] public bool UnlockedByDefault { get; set; } = true;

    /// <summary>
    /// Get a random event from this location's pool, preferring unseen events.
    /// Falls back to the full pool if all events have been seen.
    /// </summary>
    public string? GetRandomEvent(HashSet<string>? seen = null)
    {
        if (EventPool.Count == 0)
            return null;

        if (seen != null)
        {
            var unseen = new System.Collections.Generic.List<string>();
            foreach (var id in EventPool)
                if (!seen.Contains(id)) unseen.Add(id);

            if (unseen.Count > 0)
                return unseen[GD.RandRange(0, unseen.Count - 1)];
        }

        return EventPool[GD.RandRange(0, EventPool.Count - 1)];
    }
}
