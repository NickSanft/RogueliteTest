using System;
using System.Collections.Generic;

/// <summary>
/// Pure C# helper functions with no Godot engine dependency.
/// These can be referenced by both the game project and the test project.
/// </summary>
public static class GameLogic
{
    /// <summary>
    /// Clamps old+delta into [0, max]. The core of ModifyStat's arithmetic.
    /// </summary>
    public static int ClampStat(int current, int delta, int max)
        => Math.Clamp(current + delta, 0, max);

    /// <summary>
    /// Returns true when statValue meets or exceeds threshold.
    /// </summary>
    public static bool EvaluateFixedThreshold(int statValue, int threshold)
        => statValue >= threshold;

    /// <summary>
    /// Returns the asymmetric dice bonus for a given stat.
    /// Reason below half capacity grants +3 (madness insight).
    /// Doom above 50 grants +2 (dark attunement).
    /// </summary>
    public static int GetDiceBonus(string statName, int statValue, int maxReason, int doom)
    {
        int bonus = 0;
        if (statName == "reason" && statValue < maxReason / 2)
            bonus += 3;
        if (statName == "doom" && doom > 50)
            bonus += 2;
        return bonus;
    }

    /// <summary>
    /// Performs weighted random selection over a parallel list of weights.
    /// Returns the index of the selected item. Falls back to index 0 on empty input.
    /// </summary>
    public static int WeightedRandom(IReadOnlyList<float> weights, double roll01)
    {
        if (weights.Count == 0) return 0;
        float total = 0;
        foreach (float w in weights) if (w > 0) total += w;
        if (total <= 0) return 0;
        float cursor = 0;
        float target = (float)(roll01 * total);
        int lastValid = 0;
        for (int i = 0; i < weights.Count; i++)
        {
            if (weights[i] <= 0) continue;
            lastValid = i;
            cursor += weights[i];
            if (target < cursor) return i;
        }
        return lastValid;
    }
}
