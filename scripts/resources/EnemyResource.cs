using Godot;

/// <summary>
/// Defines an enemy that can be encountered in turn-based combat.
/// </summary>
[GlobalClass]
public partial class EnemyResource : Resource
{
	[Export] public string EnemyId { get; set; } = "";
	[Export] public string EnemyName { get; set; } = "";
	[Export] public int MaxHp { get; set; } = 10;
	[Export] public int AttackDamage { get; set; } = 2;
	[Export] public int SanityDamage { get; set; } = 1;
	[Export] public int DoomPerRound { get; set; } = 2;
	[Export] public string VictoryEventId { get; set; } = "";
	[Export] public string FleeEventId { get; set; } = "";
	[Export(PropertyHint.MultilineText)] public string Description { get; set; } = "";
}
