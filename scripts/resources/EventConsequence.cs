using Godot;

/// <summary>
/// Defines an outcome (stat change, doom increase, item gain, etc.)
/// </summary>
[GlobalClass]
public partial class EventConsequence : Resource
{
	public enum ConsequenceType { StatChange, ItemGain, TriggerEvent, AdvanceMystery, UnlockLocation }

	[Export] public ConsequenceType Type { get; set; } = ConsequenceType.StatChange;
	[Export] public string StatName { get; set; } = "stamina";
	[Export] public int Value { get; set; } = 0;
	[Export] public string ItemId { get; set; } = "";
	[Export] public string NextEventId { get; set; } = "";
	[Export] public int MysteryProgress { get; set; } = 1;
	[Export] public string LocationIdToUnlock { get; set; } = "";

	/// <summary>Optional stat check; if set the consequence only fires when the check passes.</summary>
	[Export] public StatCheck? Condition { get; set; }

	/// <summary>
	/// Apply this consequence to the game state
	/// </summary>
	public void Apply(GameManager gameManager)
	{
		if (Condition != null && !Condition.Evaluate(gameManager.GetPlayerStats()))
			return;

		switch (Type)
		{
			case ConsequenceType.StatChange:
				gameManager.ModifyStat(StatName, Value);
				break;

			case ConsequenceType.ItemGain:
				gameManager.AddItem(ItemId);
				break;

			case ConsequenceType.TriggerEvent:
				gameManager.QueueEvent(NextEventId);
				break;

			case ConsequenceType.AdvanceMystery:
				gameManager.AdvanceMystery(MysteryProgress);
				break;

			case ConsequenceType.UnlockLocation:
				if (!string.IsNullOrEmpty(LocationIdToUnlock))
					gameManager.UnlockLocation(LocationIdToUnlock);
				break;
		}
	}
}
