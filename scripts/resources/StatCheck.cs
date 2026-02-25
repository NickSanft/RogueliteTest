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
    [Export] public int DiceSides { get; set; } = 20;

    public bool Evaluate(Dictionary playerStats)
    {
        int statValue = GetStatValue(playerStats);

        switch (Type)
        {
            case CheckType.FixedThreshold:
                return statValue >= Threshold;

            case CheckType.DiceRoll:
                int roll = GD.RandRange(1, DiceSides);
                int bonus = GetDiceBonus(playerStats, statValue);
                return roll + statValue + bonus >= Threshold;
        }

        return false;
    }

    /// <summary>
    /// Asymmetric dice bonuses:
    /// - Reason: when sanity is below half, madness opens perceptual shortcuts (+3).
    /// - Doom: when doom is high, dark attunement sharpens dread-sense (+2).
    /// </summary>
    private int GetDiceBonus(Dictionary playerStats, int statValue)
    {
        int bonus = 0;

        if (Stat == StatType.Reason && playerStats.ContainsKey("max_reason"))
        {
            int maxReason = (int)playerStats["max_reason"];
            if (statValue < maxReason / 2)
                bonus += 3; // Madness insight
        }

        if (Stat == StatType.Doom && playerStats.ContainsKey("doom"))
        {
            if (statValue > 50)
                bonus += 2; // Dark attunement
        }

        return bonus;
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
