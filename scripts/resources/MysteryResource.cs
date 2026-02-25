using Godot;

/// <summary>
/// Defines a mystery arc — the long-form goal that gives the run a win condition.
/// Progress is accumulated via AdvanceMystery consequences on event options.
/// </summary>
[GlobalClass]
public partial class MysteryResource : Resource
{
	[Export] public string MysteryId { get; set; } = "";
	[Export] public string Name { get; set; } = "";
	[Export(PropertyHint.MultilineText)] public string Description { get; set; } = "";

	/// <summary>Total AdvanceMystery points needed to complete this mystery.</summary>
	[Export] public int RequiredProgress { get; set; } = 5;

	/// <summary>Text shown to the player when the mystery is solved.</summary>
	[Export(PropertyHint.MultilineText)] public string CompletionText { get; set; } = "";
}
