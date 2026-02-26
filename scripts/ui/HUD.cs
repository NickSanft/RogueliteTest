using Godot;

/// <summary>
/// Displays player stats, doom counter, current location, and inventory.
/// </summary>
public partial class HUD : CanvasLayer
{
	private Label? _staminaLabel;
	private Label? _reasonLabel;
	private Label? _doomLabel;
	private Label? _turnLabel;
	private Label? _locationLabel;
	private Label? _inventoryLabel;
	private Control? _floatRoot;

	private GameManager? _gameManager;

	public override void _Ready()
	{
		_staminaLabel = GetNode<Label>("%StaminaLabel");
		_reasonLabel  = GetNode<Label>("%ReasonLabel");
		_doomLabel    = GetNode<Label>("%DoomLabel");
		_turnLabel    = GetNode<Label>("%TurnLabel");
		_locationLabel = GetNode<Label>("%LocationLabel");

		// Inventory label — inserted into the bottom bar between location and the spacer
		var bottomBar = GetNode<HBoxContainer>("MarginContainer/VBoxContainer/BottomBar");
		_inventoryLabel = new Label();
		ApplyLabelStyle(_inventoryLabel);
		bottomBar.AddChild(_inventoryLabel);
		bottomBar.MoveChild(_inventoryLabel, 1); // After LocationLabel

		// Overlay for floating delta numbers
		_floatRoot = new Control();
		_floatRoot.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		_floatRoot.MouseFilter = Control.MouseFilterEnum.Ignore;
		AddChild(_floatRoot);

		_gameManager = GetNode<GameManager>("/root/GameManager");
		_gameManager.StatChanged += OnStatChanged;
		_gameManager.ItemGained  += _ => RefreshInventory();

		UpdateAllStats();
		RefreshInventory();
	}

	// ── Public update methods ─────────────────────────────────────────────────

	public void UpdateTurn(int turn)
	{
		if (_turnLabel != null)
			_turnLabel.Text = $"TURN: {turn}";
	}

	public void UpdateLocation(string locationName)
	{
		if (_locationLabel != null)
			_locationLabel.Text = $"Location: {locationName}";
	}

	public void RefreshInventory()
	{
		if (_inventoryLabel == null || _gameManager == null) return;
		_inventoryLabel.Text        = _gameManager.GetInventoryDisplay();
		_inventoryLabel.TooltipText = _gameManager.GetInventoryTooltip();
	}

	// ── Internal ──────────────────────────────────────────────────────────────

	private void OnStatChanged(string statName, int oldValue, int newValue)
	{
		UpdateAllStats();
		ShowStatChange(statName, newValue - oldValue);
	}

	private void ShowStatChange(string statName, int delta)
	{
		if (delta == 0) return;
		var label = GetLabelForStat(statName);
		if (label == null) return;

		bool positive = delta > 0;
		Color flashColor = positive ? new Color(0.3f, 1f, 0.3f) : new Color(1f, 0.3f, 0.3f);

		// Flash the label
		label.SelfModulate = flashColor;
		var flashTween = CreateTween();
		flashTween.TweenProperty(label, "self_modulate", Colors.White, 0.6f).SetDelay(0.05f);

		// Floating "+N" / "-N"
		var floatLabel = new Label();
		floatLabel.Text = delta > 0 ? $"+{delta}" : delta.ToString();
		floatLabel.AddThemeColorOverride("font_color", flashColor);
		floatLabel.AddThemeColorOverride("font_outline_color", Colors.Black);
		floatLabel.AddThemeConstantOverride("outline_size", 2);
		floatLabel.Position = label.GlobalPosition + new Vector2(label.Size.X + 6, 0);
		_floatRoot?.AddChild(floatLabel);

		var tween = CreateTween();
		tween.SetParallel(true);
		tween.TweenProperty(floatLabel, "position:y", floatLabel.Position.Y + 36f, 1.1f)
			 .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
		tween.TweenProperty(floatLabel, "modulate:a", 0.0f, 0.85f).SetDelay(0.25f);
		tween.Finished += () => floatLabel.QueueFree();
	}

	private Label? GetLabelForStat(string statName) => statName.ToLower() switch
	{
		"stamina" => _staminaLabel,
		"reason"  => _reasonLabel,
		"doom"    => _doomLabel,
		_         => null
	};

	private void UpdateAllStats()
	{
		if (_gameManager == null) return;

		if (_staminaLabel != null)
			_staminaLabel.Text = $"STAMINA: {_gameManager.Stamina}/{_gameManager.MaxStamina}";

		if (_reasonLabel != null)
			_reasonLabel.Text = $"REASON: {_gameManager.Reason}/{_gameManager.MaxReason}";

		if (_doomLabel != null)
		{
			_doomLabel.Text = $"DOOM: {_gameManager.Doom}/100";

			_doomLabel.Modulate = _gameManager.Doom switch
			{
				>= 75 => new Color(1, 0,    0),    // Red
				>= 50 => new Color(1, 0.5f, 0),    // Orange
				>= 25 => new Color(1, 1,    0),    // Yellow
				_     => new Color(1, 1,    1)     // White
			};
		}
	}

	private static void ApplyLabelStyle(Label label)
	{
		label.AddThemeColorOverride("font_color", Colors.White);
		label.AddThemeColorOverride("font_outline_color", Colors.Black);
		label.AddThemeConstantOverride("outline_size", 2);
	}
}
