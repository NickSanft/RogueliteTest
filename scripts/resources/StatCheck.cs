using Godot;
using Godot.Collections;

/// <summary>
/// Defines a stat check (e.g., "Stamina >= 5" or "Roll d20 vs Reason")
/// </summary>
[GlobalClass]
public partial class StatCheck : Resource
{
    public enum CheckType { FixedThreshold, DiceRoll, DoomScaled }
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
                return GameLogic.EvaluateFixedThreshold(statValue, Threshold);

            case CheckType.DiceRoll:
                int roll = GD.RandRange(1, DiceSides);
                int maxReason = playerStats.ContainsKey("max_reason") ? (int)playerStats["max_reason"] : 10;
                int doom = playerStats.ContainsKey("doom") ? (int)playerStats["doom"] : 0;
                string statKey = Stat switch
                {
                    StatType.Reason => "reason",
                    StatType.Doom   => "doom",
                    _               => "stamina"
                };
                int bonus = GameLogic.GetDiceBonus(statKey, statValue, maxReason, doom);
                return roll + statValue + bonus >= Threshold;

            case CheckType.DoomScaled:
                // Threshold increases as doom rises: +1 per 20 doom
                int currentDoom = playerStats.ContainsKey("doom") ? (int)playerStats["doom"] : 0;
                int scaledThreshold = Threshold + Mathf.FloorToInt(currentDoom / 20f);
                return GameLogic.EvaluateFixedThreshold(statValue, scaledThreshold);
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
