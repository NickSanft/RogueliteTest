using Godot;
using Godot.Collections;
using System.Collections.Generic;
using System.Linq;

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
    /// Optional per-event weights, parallel to EventPool. Leave empty for uniform probability.
    /// A value of 2.0 makes that event twice as likely as one with weight 1.0.
    /// </summary>
    [Export] public Array<float> EventWeights { get; set; } = new();
    
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
    /// Optional filter removes events that don't meet preconditions (item/doom requirements).
    /// Falls back to the unfiltered pool if all eligible events have been seen.
    /// Respects EventWeights when set.
    /// </summary>
    public string? GetRandomEvent(HashSet<string>? seen = null, System.Func<string, bool>? filter = null)
    {
        if (EventPool.Count == 0)
            return null;

        // Apply precondition filter; fall back to full pool if everything is filtered out
        var eligible = filter != null
            ? EventPool.Where(filter).ToList()
            : EventPool.ToList();
        if (eligible.Count == 0)
            eligible = EventPool.ToList();

        // Prefer unseen events
        if (seen != null)
        {
            var unseen = eligible.Where(id => !seen.Contains(id)).ToList();
            if (unseen.Count > 0)
                return PickWeighted(unseen);
        }

        return PickWeighted(eligible);
    }

    private string PickWeighted(List<string> ids)
    {
        bool hasWeights = EventWeights.Count == EventPool.Count;
        if (!hasWeights)
            return ids[GD.RandRange(0, ids.Count - 1)];

        // Build a weight list parallel to ids, looking up each id's weight by its pool index
        var weights = ids.Select(id =>
        {
            int idx = EventPool.IndexOf(id);
            return idx >= 0 ? EventWeights[idx] : 1f;
        }).ToArray();

        int chosen = GameLogic.WeightedRandom(weights, GD.RandRange(0.0, 1.0));
        return ids[chosen];
    }
}
