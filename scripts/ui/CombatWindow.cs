using Godot;

/// <summary>
/// Turn-based combat UI. Triggered by IsCombatEvent EventResources routed via EventWindow.
/// </summary>
public partial class CombatWindow : Panel
{
	private Label? _enemyNameLabel;
	private ProgressBar? _hpBar;
	private Label? _hpValueLabel;
	private RichTextLabel? _combatLog;
	private Button? _attackButton;
	private Button? _defendButton;
	private Button? _fleeButton;

	private GameManager? _gameManager;
	private EnemyResource? _currentEnemy;
	private bool _defendingThisRound = false;
	private bool _combatActive = false;

	[Signal] public delegate void CombatEndedEventHandler(string outcome);

	public override void _Ready()
	{
		_enemyNameLabel = GetNode<Label>("%EnemyNameLabel");
		_hpBar          = GetNode<ProgressBar>("%HPBar");
		_hpValueLabel   = GetNode<Label>("%HPValueLabel");
		_combatLog      = GetNode<RichTextLabel>("%CombatLog");
		_attackButton   = GetNode<Button>("%AttackButton");
		_defendButton   = GetNode<Button>("%DefendButton");
		_fleeButton     = GetNode<Button>("%FleeButton");

		_gameManager = GetNode<GameManager>("/root/GameManager");

		_attackButton!.Pressed += OnAttack;
		_defendButton!.Pressed += OnDefend;
		_fleeButton!.Pressed   += OnFlee;

		// If a stat hits a limit mid-combat, disable buttons so no further input is processed
		_gameManager.GameOver += _ => _combatActive = false;

		SetButtonsEnabled(false);
		Visible = false;
	}

	public void StartCombat(EnemyResource enemy)
	{
		_currentEnemy       = enemy;
		_defendingThisRound = false;
		_combatActive       = true;

		if (_enemyNameLabel != null)
			_enemyNameLabel.Text = enemy.EnemyName;

		UpdateHpDisplay(enemy.MaxHp, enemy.MaxHp);

		if (_combatLog != null)
			_combatLog.Text = "";

		AppendLog(enemy.Description);
		AppendLog($"HP: {enemy.MaxHp}  Attack: {enemy.AttackDamage}  Sanity drain: {enemy.SanityDamage}/round  Doom: +{enemy.DoomPerRound}/round");
		AppendLog("Press [1] Attack  [2] Defend  [3] Flee");

		SetButtonsEnabled(true);
	}

	private void OnAttack() => ExecuteRound("attack");
	private void OnDefend() => ExecuteRound("defend");
	private void OnFlee()   => ExecuteRound("flee");

	private void ExecuteRound(string playerAction)
	{
		if (!_combatActive || _currentEnemy == null || _gameManager == null) return;

		SetButtonsEnabled(false);
		_defendingThisRound = false;

		AppendLog($"\n--- {playerAction.ToUpper()} ---");

		switch (playerAction)
		{
			case "attack":
				int roll = GD.RandRange(1, 6) + _gameManager.Stamina;
				if (roll >= 5)
				{
					int dmg = GD.RandRange(2, 4);
					_gameManager.DamageEnemy(dmg);
					AppendLog($"You strike true! Dealt {dmg} damage. Enemy HP: {_gameManager.CurrentEnemyHp}/{_currentEnemy.MaxHp}");
				}
				else
				{
					_gameManager.DamageEnemy(1);
					AppendLog($"A glancing blow. Dealt 1 damage. Enemy HP: {_gameManager.CurrentEnemyHp}/{_currentEnemy.MaxHp}");
				}
				UpdateHpDisplay(_gameManager.CurrentEnemyHp, _currentEnemy.MaxHp);
				if (_gameManager.IsEnemyDefeated)
				{
					EndCombat("victory");
					return;
				}
				break;

			case "defend":
				_defendingThisRound = true;
				AppendLog("You brace for the assault, halving incoming damage.");
				break;

			case "flee":
				int fleeRoll = GD.RandRange(1, 6) + _gameManager.Reason;
				if (fleeRoll >= 4)
				{
					AppendLog("You manage to escape!");
					EndCombat("flee");
					return;
				}
				else
				{
					_gameManager.ModifyStat("doom", 5);
					AppendLog("You panic and fail to escape! Doom +5.");
				}
				break;
		}

		// Enemy counterattack
		int attackDmg = _currentEnemy.AttackDamage;
		if (_defendingThisRound)
			attackDmg = Mathf.Max(0, attackDmg / 2);

		if (attackDmg > 0)
		{
			_gameManager.ModifyStat("stamina", -attackDmg);
			AppendLog($"The {_currentEnemy.EnemyName} attacks! You lose {attackDmg} Stamina.");
		}
		else
		{
			AppendLog($"Your defence absorbs the {_currentEnemy.EnemyName}'s strike.");
		}

		// Sanity drain per round
		if (_currentEnemy.SanityDamage > 0)
		{
			_gameManager.ModifyStat("reason", -_currentEnemy.SanityDamage);
			AppendLog($"The horror erodes your mind. Reason -{_currentEnemy.SanityDamage}.");
		}

		// Doom pressure per round
		if (_currentEnemy.DoomPerRound > 0)
		{
			_gameManager.ModifyStat("doom", _currentEnemy.DoomPerRound);
			AppendLog($"Tension rises. Doom +{_currentEnemy.DoomPerRound}.");
		}

		// Don't re-enable buttons if a stat-hit triggered game over
		if (!_combatActive) return;

		SetButtonsEnabled(true);
	}

	private void EndCombat(string outcome)
	{
		_combatActive = false;
		SetButtonsEnabled(false);

		if (_currentEnemy == null || _gameManager == null) return;

		string aftermathId = outcome == "victory"
			? _currentEnemy.VictoryEventId
			: _currentEnemy.FleeEventId;

		if (!string.IsNullOrEmpty(aftermathId))
			_gameManager.QueueEvent(aftermathId);

		AppendLog(outcome == "victory"
			? "\nVictory! The threat is neutralised."
			: "\nYou retreat into the dark.");

		WindowManager.Instance?.Pop();
		EmitSignal(SignalName.CombatEnded, outcome);
	}

	private void AppendLog(string text)
	{
		if (_combatLog == null) return;
		_combatLog.Text += text + "\n";
	}

	private void UpdateHpDisplay(int current, int max)
	{
		if (_hpBar != null)
		{
			_hpBar.MaxValue = max;
			_hpBar.Value    = current;
		}
		if (_hpValueLabel != null)
			_hpValueLabel.Text = $"{current}/{max}";
	}

	private void SetButtonsEnabled(bool enabled)
	{
		if (_attackButton != null) _attackButton.Disabled = !enabled;
		if (_defendButton != null) _defendButton.Disabled = !enabled;
		if (_fleeButton   != null) _fleeButton.Disabled   = !enabled;
	}

	public override void _Input(InputEvent @event)
	{
		if (!Visible || !_combatActive) return;

		if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
		{
			if (keyEvent.Keycode == Key.Key1)
			{
				OnAttack();
				GetViewport().SetInputAsHandled();
			}
			else if (keyEvent.Keycode == Key.Key2)
			{
				OnDefend();
				GetViewport().SetInputAsHandled();
			}
			else if (keyEvent.Keycode == Key.Key3)
			{
				OnFlee();
				GetViewport().SetInputAsHandled();
			}
		}
	}
}
