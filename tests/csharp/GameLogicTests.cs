using Xunit;

public class ClampStatTests
{
    [Fact] public void Clamp_WithinBounds_ReturnsSum()      => Assert.Equal(7,  GameLogic.ClampStat(5,  2,  10));
    [Fact] public void Clamp_ExceedsMax_ReturnsMax()        => Assert.Equal(10, GameLogic.ClampStat(8,  5,  10));
    [Fact] public void Clamp_BelowZero_ReturnsZero()        => Assert.Equal(0,  GameLogic.ClampStat(3,  -9, 10));
    [Fact] public void Clamp_NegativeDelta_WorksCorrectly() => Assert.Equal(3,  GameLogic.ClampStat(7,  -4, 10));
    [Fact] public void Clamp_DoomCap_ClampedAt100()         => Assert.Equal(100, GameLogic.ClampStat(95, 10, 100));
}

public class EvaluateFixedThresholdTests
{
    [Fact] public void AtThreshold_ReturnsTrue()  => Assert.True(GameLogic.EvaluateFixedThreshold(5, 5));
    [Fact] public void AboveThreshold_ReturnsTrue() => Assert.True(GameLogic.EvaluateFixedThreshold(8, 5));
    [Fact] public void BelowThreshold_ReturnsFalse() => Assert.False(GameLogic.EvaluateFixedThreshold(4, 5));
    [Fact] public void ZeroThreshold_AlwaysTrue()  => Assert.True(GameLogic.EvaluateFixedThreshold(0, 0));
}

public class GetDiceBonusTests
{
    [Fact]
    public void Reason_BelowHalf_GrantsBonus()
    {
        // maxReason=10, statValue=4 (below 5) → +3
        int bonus = GameLogic.GetDiceBonus("reason", 4, 10, 0);
        Assert.Equal(3, bonus);
    }

    [Fact]
    public void Reason_AtHalf_NoBonus()
    {
        int bonus = GameLogic.GetDiceBonus("reason", 5, 10, 0);
        Assert.Equal(0, bonus);
    }

    [Fact]
    public void Doom_Above50_GrantsBonus()
    {
        int bonus = GameLogic.GetDiceBonus("doom", 60, 10, 60);
        Assert.Equal(2, bonus);
    }

    [Fact]
    public void Doom_AtOrBelow50_NoBonus()
    {
        int bonus = GameLogic.GetDiceBonus("doom", 50, 10, 50);
        Assert.Equal(0, bonus);
    }

    [Fact]
    public void Stamina_NeverGrantsBonus()
    {
        int bonus = GameLogic.GetDiceBonus("stamina", 1, 10, 99);
        Assert.Equal(0, bonus);
    }

    [Fact]
    public void BothConditions_StackBonuses()
    {
        // reason below half AND doom above 50 when stat is doom shouldn't stack on reason check
        // reason check: statValue=3, maxReason=10 → +3
        int bonus = GameLogic.GetDiceBonus("reason", 3, 10, 80);
        Assert.Equal(3, bonus);
    }
}

public class WeightedRandomTests
{
    [Fact]
    public void EqualWeights_AllIndicesReachable()
    {
        var weights = new float[] { 1f, 1f, 1f };
        var seen = new System.Collections.Generic.HashSet<int>();
        for (int i = 0; i < 300; i++)
            seen.Add(GameLogic.WeightedRandom(weights, i / 300.0));
        Assert.Equal(3, seen.Count);
    }

    [Fact]
    public void SingleWeight_AlwaysReturnsZero()
    {
        var weights = new float[] { 1f };
        Assert.Equal(0, GameLogic.WeightedRandom(weights, 0.0));
        Assert.Equal(0, GameLogic.WeightedRandom(weights, 0.99));
    }

    [Fact]
    public void ZeroWeightEntry_NeverSelected()
    {
        // Weight [0, 1, 0] — only index 1 should ever be chosen
        var weights = new float[] { 0f, 1f, 0f };
        for (double roll = 0.0; roll < 1.0; roll += 0.1)
            Assert.Equal(1, GameLogic.WeightedRandom(weights, roll));
    }

    [Fact]
    public void EmptyWeights_ReturnsZero()
    {
        Assert.Equal(0, GameLogic.WeightedRandom(System.Array.Empty<float>(), 0.5));
    }
}
