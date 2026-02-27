using Godot;
using Godot.Collections;

/// <summary>
/// Represents a single choice within an event
/// </summary>
[GlobalClass]
public partial class EventOption : Resource
{
	[Export] public string OptionText { get; set; } = "";
	[Export] public StatCheck? StatCheck { get; set; }
	/// <summary>Applied regardless of stat check outcome</summary>
	[Export] public Array<EventConsequence> Consequences { get; set; } = new();
	/// <summary>Applied only when the stat check passes (or there is no check)</summary>
	[Export] public Array<EventConsequence> SuccessConsequences { get; set; } = new();
	/// <summary>Applied only when the stat check fails</summary>
	[Export] public Array<EventConsequence> FailureConsequences { get; set; } = new();
	[Export(PropertyHint.MultilineText)] public string SuccessText { get; set; } = "";
	[Export(PropertyHint.MultilineText)] public string FailureText { get; set; } = "";

	/// <summary>Item ID required to see this option. Empty = always visible.</summary>
	[Export] public string RequiredItem { get; set; } = "";

	/// <summary>Minimum doom required to see this option. 0 = always visible.</summary>
	[Export] public int MinDoom { get; set; } = 0;

	/// <summary>
	/// If stat_check exists, returns true if player passes the check
	/// </summary>
	public bool EvaluateStatCheck(Dictionary playerStats)
	{
		if (StatCheck == null)
			return true; // No check = auto-pass
		
		return StatCheck.Evaluate(playerStats);
	}
}
