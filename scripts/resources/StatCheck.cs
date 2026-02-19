using Godot;
using Godot.Collections;

/// <summary>
/// Defines a stat check (e.g., "Stamina >= 5" or "Roll d20 vs Reason")
/// </summary>
[GlobalClass]
public partial class StatCheck : Resource
{
    public enum CheckType { FixedThreshold, DiceRoll }
    public enum StatType { Stamina, Reason, Doom }

    [Export] public CheckType Type { get; set; } = CheckType.FixedThreshold;
    [Export] public StatType Stat { get; set; } = StatType.Stamina;
    [Export] public int Threshold { get; set; } = 5;
    [Export] public int DiceSides { get; set; } = 20; // For dice rolls (d20, d6, etc.)

    public bool Evaluate(Dictionary playerStats)
    {
        int statValue = GetStatValue(playerStats);

        switch (Type)
        {
            case CheckType.FixedThreshold:
                return statValue >= Threshold;
            
            case CheckType.DiceRoll:
                int roll = GD.RandRange(1, DiceSides);
                return roll + statValue >= Threshold;
        }

        return false;
    }

    private int GetStatValue(Dictionary playerStats)
    {
        string statKey = Stat switch
        {
            StatType.Stamina => "stamina",
            StatType.Reason => "reason",
            StatType.Doom => "doom",
            _ => "stamina"
        };

        return playerStats.ContainsKey(statKey) ? (int)playerStats[statKey] : 0;
    }
}
