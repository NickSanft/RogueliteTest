using Godot;
using Godot.Collections;

/// <summary>
/// Represents a single event in the game (investigation, encounter, etc.)
/// </summary>
[GlobalClass]
public partial class EventResource : Resource
{
	[Export] public string EventId { get; set; } = "";
	[Export(PropertyHint.MultilineText)] public string EventText { get; set; } = "";
	[Export] public string EventImagePath { get; set; } = "";
	[Export] public Array<EventOption> Options { get; set; } = new();
	[Export] public Array<EventConsequence> AutoConsequences { get; set; } = new();

	/// <summary>Item the player must hold for this event to appear in a location pool.</summary>
	[Export] public string RequiredItem { get; set; } = "";

	/// <summary>Minimum doom level for this event to appear in a location pool. 0 = always eligible.</summary>
	[Export] public int MinDoom { get; set; } = 0;

	/// <summary>If true, this event triggers a combat encounter rather than showing in the EventWindow.</summary>
	[Export] public bool IsCombatEvent { get; set; } = false;

	/// <summary>Enemy ID to load from res://data/enemies/ when IsCombatEvent is true.</summary>
	[Export] public string EnemyId { get; set; } = "";

	public EventOption? GetOption(int index)
	{
		if (index >= 0 && index < Options.Count)
			return Options[index];
		return null;
	}
}
