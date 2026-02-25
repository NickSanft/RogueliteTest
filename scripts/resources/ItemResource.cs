using Godot;

/// <summary>
/// A passive item carried in the player's inventory.
/// Bonuses are applied to effective stat values during all stat checks.
/// The raw stats displayed on the HUD are not affected.
/// </summary>
[GlobalClass]
public partial class ItemResource : Resource
{
	[Export] public string ItemId { get; set; } = "";
	[Export] public string ItemName { get; set; } = "";
	[Export(PropertyHint.MultilineText)] public string Description { get; set; } = "";

	/// <summary>Added to Stamina during all stat checks.</summary>
	[Export] public int StaminaBonus { get; set; } = 0;

	/// <summary>Added to Reason during all stat checks.</summary>
	[Export] public int ReasonBonus { get; set; } = 0;
}
