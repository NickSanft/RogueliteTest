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
	// Player stats
	public int Stamina { get; set; } = 10;
	public int Reason { get; set; } = 10;
	public int MaxStamina { get; set; } = 10;
	public int MaxReason { get; set; } = 10;
	public int Doom { get; set; } = 0;

	// Run state
	public List<string> Inventory { get; set; } = new();
	public List<string> EventQueue { get; set; } = new();
	public List<string> ActiveMysteries { get; set; } = new();
	public System.Collections.Generic.Dictionary<string, int> MysteryProgress { get; set; } = new();

	// Meta-progression (persists across runs)
	public int TotalRuns { get; private set; } = 0;
	public int MysteriesTotalSolved { get; private set; } = 0;

	// Signals
	[Signal] public delegate void StatChangedEventHandler(string statName, int oldValue, int newValue);
	[Signal] public delegate void GameOverEventHandler(string reason);
	[Signal] public delegate void MysteryCompletedEventHandler(string mysteryId, string completionText);
	[Signal] public delegate void RunWonEventHandler(string winText);
	[Signal] public delegate void LocationUnlockedEventHandler(string locationId);
	[Signal] public delegate void ItemGainedEventHandler(string itemId);

	private const string SavePath = "user://roguelite_save.cfg";
	private const string MetaSavePath = "user://roguelite_meta.cfg";

	private readonly System.Collections.Generic.Dictionary<string, ItemResource> _loadedItems = new();
	private readonly System.Collections.Generic.Dictionary<string, MysteryResource> _loadedMysteries = new();

	public override void _Ready()
	{
		LoadMeta();
		LoadItems();
		LoadMysteries();

		if (!LoadGame())
		{
			GD.Print("New run — fresh start.");
			InitialiseMysteries();
			ApplyMetaBonuses();
		}
	}

	// ── Stat modification ────────────────────────────────────────────────────

	public void ModifyStat(string statName, int amount)
	{
		int oldValue = GetStatValue(statName);
		int maxValue = GetMaxStatValue(statName);
		int newValue = Mathf.Clamp(oldValue + amount, 0, maxValue);

		SetStatValue(statName, newValue);
		EmitSignal(SignalName.StatChanged, statName, oldValue, newValue);

		if (statName == "doom" && newValue >= 100)
			EmitSignal(SignalName.GameOver, "The Old Ones have awakened.");
		else if (statName == "stamina" && newValue <= 0)
			EmitSignal(SignalName.GameOver, "You collapsed from exhaustion.");
		else if (statName == "reason" && newValue <= 0)
			EmitSignal(SignalName.GameOver, "Your mind shattered.");
	}

	/// <summary>
	/// Returns effective stats used by stat checks.
	/// Includes passive item bonuses; raw values shown in the HUD are unaffected.
	/// </summary>
	public GodotDictionary GetPlayerStats()
	{
		int staminaBonus = 0, reasonBonus = 0;
		foreach (var itemId in Inventory)
		{
			if (_loadedItems.TryGetValue(itemId, out var item))
			{
				staminaBonus += item.StaminaBonus;
				reasonBonus  += item.ReasonBonus;
			}
		}

		return new GodotDictionary
		{
			{ "stamina",    Stamina + staminaBonus },
			{ "reason",     Reason  + reasonBonus  },
			{ "doom",       Doom },
			{ "max_reason", MaxReason }
		};
	}

	// ── Inventory ────────────────────────────────────────────────────────────

	public void AddItem(string itemId)
	{
		if (Inventory.Contains(itemId))
			return;

		Inventory.Add(itemId);
		EmitSignal(SignalName.ItemGained, itemId);
		SaveGame();
	}

	public string GetInventoryDisplay()
	{
		if (Inventory.Count == 0)
			return "";

		var names = new List<string>();
		foreach (var id in Inventory)
			names.Add(_loadedItems.TryGetValue(id, out var item) ? item.ItemName : id);

		return "Items: " + string.Join(", ", names);
	}

	// ── Event queue ──────────────────────────────────────────────────────────

	public void QueueEvent(string eventId)
	{
		EventQueue.Add(eventId);
	}

	// ── Location unlocking ───────────────────────────────────────────────────

	public void UnlockLocation(string locationId)
	{
		EmitSignal(SignalName.LocationUnlocked, locationId);
	}

	// ── Mystery system ───────────────────────────────────────────────────────

	public void AdvanceMystery(int amount)
	{
		if (ActiveMysteries.Count == 0)
			return;

		string currentMystery = ActiveMysteries[0];
		if (!MysteryProgress.ContainsKey(currentMystery))
			MysteryProgress[currentMystery] = 0;

		MysteryProgress[currentMystery] += amount;

		if (!_loadedMysteries.TryGetValue(currentMystery, out var mystery))
			return;

		if (MysteryProgress[currentMystery] < mystery.RequiredProgress)
			return;

		// Mystery completed
		ActiveMysteries.RemoveAt(0);
		MysteriesTotalSolved++;
		SaveMeta();

		EmitSignal(SignalName.MysteryCompleted, currentMystery, mystery.CompletionText);

		if (ActiveMysteries.Count == 0)
			EmitSignal(SignalName.RunWon, mystery.CompletionText);
	}

	private void InitialiseMysteries()
	{
		ActiveMysteries.Clear();
		MysteryProgress.Clear();
		foreach (var id in _loadedMysteries.Keys)
			ActiveMysteries.Add(id);
	}

	// ── Meta-progression ─────────────────────────────────────────────────────

	/// <summary>
	/// Increments the run counter and removes the run save. Called on game-over.
	/// </summary>
	public void IncrementRunCount()
	{
		TotalRuns++;
		SaveMeta();
		DeleteSave();
	}

	private void ApplyMetaBonuses()
	{
		// After 1 mystery solved across all runs: start with the Worn Journal
		if (MysteriesTotalSolved >= 1 && !Inventory.Contains("worn_journal"))
			Inventory.Add("worn_journal");

		// After 3 runs attempted: start with Rope & Torch
		if (TotalRuns >= 3 && !Inventory.Contains("rope_and_torch"))
			Inventory.Add("rope_and_torch");
	}

	// ── Save / load (current run) ────────────────────────────────────────────

	public void SaveGame()
	{
		var config = new ConfigFile();
		config.SetValue("stats", "stamina", Stamina);
		config.SetValue("stats", "reason",  Reason);
		config.SetValue("stats", "doom",    Doom);

		var invArr = new Godot.Collections.Array();
		foreach (var id in Inventory) invArr.Add(id);
		config.SetValue("inventory", "items", invArr);

		var mystArr = new Godot.Collections.Array();
		foreach (var id in ActiveMysteries) mystArr.Add(id);
		config.SetValue("mysteries", "active", mystArr);
		foreach (var kvp in MysteryProgress)
			config.SetValue("mysteries", $"progress_{kvp.Key}", kvp.Value);

		if (config.Save(SavePath) != Error.Ok)
			GD.PrintErr("SaveGame: write failed.");
	}

	public bool LoadGame()
	{
		var config = new ConfigFile();
		if (config.Load(SavePath) != Error.Ok)
			return false;

		Stamina = (int)config.GetValue("stats", "stamina", (Variant)MaxStamina);
		Reason  = (int)config.GetValue("stats", "reason",  (Variant)MaxReason);
		Doom    = (int)config.GetValue("stats", "doom",    (Variant)0);

		Inventory.Clear();
		var invArr = config.GetValue("inventory", "items", new Godot.Collections.Array()).AsGodotArray();
		foreach (var v in invArr) Inventory.Add(v.AsString());

		ActiveMysteries.Clear();
		MysteryProgress.Clear();
		var mystArr = config.GetValue("mysteries", "active", new Godot.Collections.Array()).AsGodotArray();
		foreach (var v in mystArr)
		{
			string id = v.AsString();
			ActiveMysteries.Add(id);
			string key = $"progress_{id}";
			if (config.HasSectionKey("mysteries", key))
				MysteryProgress[id] = (int)config.GetValue("mysteries", key, (Variant)0);
		}

		GD.Print($"Save loaded — Stamina:{Stamina} Reason:{Reason} Doom:{Doom}");
		return true;
	}

	public void DeleteSave()
	{
		if (!FileAccess.FileExists(SavePath)) return;
		DirAccess.Open("user://")?.Remove("roguelite_save.cfg");
	}

	// ── Save / load (meta, cross-run) ────────────────────────────────────────

	public void SaveMeta()
	{
		var config = new ConfigFile();
		config.SetValue("meta", "total_runs",             TotalRuns);
		config.SetValue("meta", "mysteries_solved_total", MysteriesTotalSolved);
		if (config.Save(MetaSavePath) != Error.Ok)
			GD.PrintErr("SaveMeta: write failed.");
	}

	public void LoadMeta()
	{
		var config = new ConfigFile();
		if (config.Load(MetaSavePath) != Error.Ok) return;
		TotalRuns             = (int)config.GetValue("meta", "total_runs",             (Variant)0);
		MysteriesTotalSolved  = (int)config.GetValue("meta", "mysteries_solved_total", (Variant)0);
	}

	// ── Reset (new run) ──────────────────────────────────────────────────────

	public void ResetGame()
	{
		TotalRuns++;
		SaveMeta();

		Stamina = MaxStamina;
		Reason  = MaxReason;
		Doom    = 0;
		Inventory.Clear();
		EventQueue.Clear();

		InitialiseMysteries();
		ApplyMetaBonuses();
		DeleteSave();
	}

	// ── Resource loading ─────────────────────────────────────────────────────

	private void LoadItems()
	{
		var dir = DirAccess.Open("res://data/items");
		if (dir == null) return;

		dir.ListDirBegin();
		string f = dir.GetNext();
		while (f != "")
		{
			if (!dir.CurrentIsDir() && f.EndsWith(".tres"))
			{
				var item = GD.Load<ItemResource>($"res://data/items/{f}");
				if (item != null) _loadedItems[item.ItemId] = item;
			}
			f = dir.GetNext();
		}
		dir.ListDirEnd();
	}

	private void LoadMysteries()
	{
		var dir = DirAccess.Open("res://data/mysteries");
		if (dir == null) return;

		dir.ListDirBegin();
		string f = dir.GetNext();
		while (f != "")
		{
			if (!dir.CurrentIsDir() && f.EndsWith(".tres"))
			{
				var mystery = GD.Load<MysteryResource>($"res://data/mysteries/{f}");
				if (mystery != null) _loadedMysteries[mystery.MysteryId] = mystery;
			}
			f = dir.GetNext();
		}
		dir.ListDirEnd();
	}

	// ── Helpers ──────────────────────────────────────────────────────────────

	private int GetStatValue(string statName) => statName.ToLower() switch
	{
		"stamina" => Stamina,
		"reason"  => Reason,
		"doom"    => Doom,
		_         => 0
	};

	private int GetMaxStatValue(string statName) => statName.ToLower() switch
	{
		"stamina" => MaxStamina,
		"reason"  => MaxReason,
		"doom"    => 100,
		_         => 999
	};

	private void SetStatValue(string statName, int value)
	{
		switch (statName.ToLower())
		{
			case "stamina": Stamina = value; break;
			case "reason":  Reason  = value; break;
			case "doom":    Doom    = value; break;
		}
	}
}
