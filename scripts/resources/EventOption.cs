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
	[Export] public Array<EventConsequence> Consequences { get; set; } = new();
	[Export(PropertyHint.MultilineText)] public string SuccessText { get; set; } = "";
	[Export(PropertyHint.MultilineText)] public string FailureText { get; set; } = "";

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
