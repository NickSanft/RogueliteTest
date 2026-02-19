using Godot;
using Godot.Collections;
using System.Collections.Generic;
using GodotDictionary = Godot.Collections.Dictionary;

/// <summary>
/// Global state manager (Autoload Singleton)
/// Add this to Project Settings > Autoload as "GameManager"
/// </summary>
public partial class GameManager : Node
{
    // Player Stats
    public int Stamina { get; set; } = 10;
    public int Reason { get; set; } = 10;
    public int MaxStamina { get; set; } = 10;
    public int MaxReason { get; set; } = 10;

    // Global Doom (loss condition at 100)
    public int Doom { get; set; } = 0;

    // Active Mysteries
    public List<string> ActiveMysteries { get; set; } = new();
    public System.Collections.Generic.Dictionary<string, int> MysteryProgress { get; set; } = new();

    // Inventory
    public List<string> Inventory { get; set; } = new();

    // Event Queue (for chained events)
    public List<string> EventQueue { get; set; } = new();

    // Signals
    [Signal] public delegate void StatChangedEventHandler(string statName, int oldValue, int newValue);
    [Signal] public delegate void DoomChangedEventHandler(int oldDoom, int newDoom);
    [Signal] public delegate void GameOverEventHandler(string reason);

    /// <summary>
    /// Modify a stat (stamina, reason, doom)
    /// </summary>
    public void ModifyStat(string statName, int amount)
    {
        int oldValue = GetStatValue(statName);
        int maxValue = GetMaxStatValue(statName);
        int newValue = Mathf.Clamp(oldValue + amount, 0, maxValue);

        SetStatValue(statName, newValue);
        EmitSignal(SignalName.StatChanged, statName, oldValue, newValue);

        // Check game over conditions
        if (statName == "doom" && newValue >= 100)
            EmitSignal(SignalName.GameOver, "The Old Ones have awakened.");
        else if (statName == "stamina" && newValue <= 0)
            EmitSignal(SignalName.GameOver, "You collapsed from exhaustion.");
        else if (statName == "reason" && newValue <= 0)
            EmitSignal(SignalName.GameOver, "Your mind shattered.");
    }

    /// <summary>
    /// Add item to inventory
    /// </summary>
    public void AddItem(string itemId)
    {
        Inventory.Add(itemId);
    }

    /// <summary>
    /// Queue event for later trigger
    /// </summary>
    public void QueueEvent(string eventId)
    {
        EventQueue.Add(eventId);
    }

    /// <summary>
    /// Advance mystery progress
    /// </summary>
    public void AdvanceMystery(int amount)
    {
        if (ActiveMysteries.Count == 0)
            return;

        string currentMystery = ActiveMysteries[0];
        if (!MysteryProgress.ContainsKey(currentMystery))
            MysteryProgress[currentMystery] = 0;
        
        MysteryProgress[currentMystery] += amount;
    }

    /// <summary>
    /// Get player stats as dictionary (for stat checks)
    /// </summary>
    public GodotDictionary GetPlayerStats()
    {
        return new GodotDictionary
        {
            { "stamina", Stamina },
            { "reason", Reason },
            { "doom", Doom }
        };
    }

    /// <summary>
    /// Reset game state (for new run)
    /// </summary>
    public void ResetGame()
    {
        Stamina = MaxStamina;
        Reason = MaxReason;
        Doom = 0;
        ActiveMysteries.Clear();
        MysteryProgress.Clear();
        Inventory.Clear();
        EventQueue.Clear();
    }

    // Helper methods for reflection-free stat access
    private int GetStatValue(string statName)
    {
        return statName.ToLower() switch
        {
            "stamina" => Stamina,
            "reason" => Reason,
            "doom" => Doom,
            _ => 0
        };
    }

    private int GetMaxStatValue(string statName)
    {
        return statName.ToLower() switch
        {
            "stamina" => MaxStamina,
            "reason" => MaxReason,
            "doom" => 999,
            _ => 999
        };
    }

    private void SetStatValue(string statName, int value)
    {
        switch (statName.ToLower())
        {
            case "stamina":
                Stamina = value;
                break;
            case "reason":
                Reason = value;
                break;
            case "doom":
                Doom = value;
                break;
        }
    }
}
