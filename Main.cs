using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class Main : Node2D
{
	private EventWindow? _eventWindow;
	private LocationWindow? _locationWindow;
	private HUD? _hud;
	private GameManager? _gameManager;
	private EventManager? _eventManager;

	private List<LocationResource> _availableLocations = new();
	private int _currentTurn = 1;

	private ColorRect? _locationEffectRect;
	private Dictionary<string, ShaderMaterial> _locationMaterials = new();

	private ColorRect? _transitionRect;
	private WindowManager? _windowManager;

	public override void _Ready()
	{
		_gameManager = GetNode<GameManager>("/root/GameManager");
		_eventManager = GetNode<EventManager>("/root/EventManager");

		_windowManager = new WindowManager();
		AddChild(_windowManager);

		var uiLayer = new CanvasLayer();
		uiLayer.Layer = 200;
		AddChild(uiLayer);

		var eventWindowScene = GD.Load<PackedScene>("res://scenes/ui/EventWindow.tscn");
		if (eventWindowScene == null)
		{
			GD.PrintErr("Failed to load EventWindow.tscn!");
			return;
		}
		_eventWindow = eventWindowScene.Instantiate<EventWindow>();
		uiLayer.AddChild(_eventWindow);

		var locationWindowScene = GD.Load<PackedScene>("res://scenes/ui/LocationWindow.tscn");
		if (locationWindowScene == null)
		{
			GD.PrintErr("Failed to load LocationWindow.tscn!");
			return;
		}
		_locationWindow = locationWindowScene.Instantiate<LocationWindow>();
		_locationWindow.LocationInvestigated += OnLocationInvestigated;
		uiLayer.AddChild(_locationWindow);

		var hudScene = GD.Load<PackedScene>("res://scenes/ui/HUD.tscn");
		if (hudScene != null)
		{
			_hud = hudScene.Instantiate<HUD>();
			AddChild(_hud);
		}

		_gameManager.StatChanged         += OnStatChanged;
		_gameManager.GameOver            += OnGameOver;
		_gameManager.MysteryCompleted    += OnMysteryCompleted;
		_gameManager.RunWon              += OnRunWon;
		_gameManager.LocationUnlocked    += OnLocationUnlocked;

		LoadLocations();
		SetupLocationEffects();
		SetupTransitionOverlay();

		_hud?.UpdateLocation("Town Square");
	}

	private void LoadLocations()
	{
		var dir = DirAccess.Open("res://data/locations");
		if (dir == null)
		{
			GD.PrintErr("Failed to open res://data/locations/");
			return;
		}

		dir.ListDirBegin();
		string fileName = dir.GetNext();
		while (fileName != "")
		{
			if (!dir.CurrentIsDir() && fileName.EndsWith(".tres"))
			{
				string path = $"res://data/locations/{fileName}";
				var location = GD.Load<LocationResource>(path);
				if (location != null && location.UnlockedByDefault)
					_availableLocations.Add(location);
				else if (location == null)
					GD.PrintErr($"Failed to load location: {path}");
			}
			fileName = dir.GetNext();
		}
		dir.ListDirEnd();
	}

	public override void _Input(InputEvent @event)
	{
		if (_windowManager?.HasOpenWindow == true)
			return;

		// SPACE — show test event
		if (@event.IsActionPressed("ui_accept"))
		{
			if (_eventWindow != null && _eventManager != null)
			{
				var testEvent = _eventManager.LoadEvent("res://data/events/test_dark_room.tres");
				if (testEvent != null)
					_eventWindow.ShowEvent(testEvent);
				else
					GD.PrintErr("Failed to load test event resource.");
			}
			GetViewport().SetInputAsHandled();
		}

		// TAB — show location selection
		if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo && keyEvent.Keycode == Key.Tab)
		{
			_locationWindow?.ShowLocations(_availableLocations);
			GetViewport().SetInputAsHandled();
		}
	}

	private async void OnLocationInvestigated(LocationResource location)
	{
		await FadeOut();

		_hud?.UpdateLocation(location.LocationName);
		_currentTurn += location.TurnCost;
		_hud?.UpdateTurn(_currentTurn);
		_gameManager?.ModifyStat("doom", location.TurnCost * 2);
		ApplyLocationEffect(location.LocationId);
		_gameManager?.SaveGame();

		await FadeIn();

		string? eventId = location.GetRandomEvent();
		if (eventId != null && _eventManager != null && _eventWindow != null)
		{
			var locationEvent = _eventManager.LoadEvent($"res://data/events/{eventId}.tres");
			if (locationEvent != null)
				_eventWindow.ShowEvent(locationEvent);
		}
	}

	// ── Gameplay signal handlers ──────────────────────────────────────────────

	private void OnStatChanged(string statName, int oldValue, int newValue)
	{
		GD.Print($"{statName.ToUpper()} changed: {oldValue} → {newValue}");
	}

	private void OnGameOver(string reason)
	{
		GD.Print($"GAME OVER: {reason}");
		_gameManager?.IncrementRunCount();
		GetTree().Paused = true;
	}

	private void OnMysteryCompleted(string mysteryId, string completionText)
	{
		GD.Print($"Mystery completed: {mysteryId}");
	}

	private void OnRunWon(string winText)
	{
		if (_eventWindow == null) return;

		// Build a win event dynamically from the mystery's completion text
		var winEvent = new EventResource();
		winEvent.EventId   = "run_won";
		winEvent.EventText = winText;

		var endOption = new EventOption();
		endOption.OptionText   = "The investigation concludes.";
		endOption.SuccessText  = "You carry the knowledge out of the abyss. Some part of you has stayed behind.";
		winEvent.Options.Add(endOption);

		_eventWindow.ShowEvent(winEvent);

		// Pause and update meta once the window closes
		_eventWindow.VisibilityChanged += OnWinEventClosed;
	}

	private void OnWinEventClosed()
	{
		if (_eventWindow?.Visible != false) return;
		_eventWindow.VisibilityChanged -= OnWinEventClosed;

		_gameManager?.ResetGame();
		GetTree().Paused = true;
	}

	private void OnLocationUnlocked(string locationId)
	{
		string path = $"res://data/locations/{locationId}.tres";
		var location = GD.Load<LocationResource>(path);
		if (location == null || _availableLocations.Contains(location))
			return;

		_availableLocations.Add(location);

		// Preload the location's shader so there's no stutter on first visit
		string shaderPath = $"res://shaders/location_effects/{locationId}.gdshader";
		var shader = GD.Load<Shader>(shaderPath);
		if (shader != null)
			_locationMaterials[locationId] = new ShaderMaterial { Shader = shader };

		GD.Print($"Location unlocked: {location.LocationName}");
	}

	// ── Location effects ─────────────────────────────────────────────────────

	private void SetupLocationEffects()
	{
		var effectLayer = new CanvasLayer();
		effectLayer.Layer = 64;
		AddChild(effectLayer);

		_locationEffectRect = new ColorRect();
		_locationEffectRect.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		_locationEffectRect.MouseFilter = Control.MouseFilterEnum.Ignore;
		effectLayer.AddChild(_locationEffectRect);

		foreach (var location in _availableLocations)
		{
			string path = $"res://shaders/location_effects/{location.LocationId}.gdshader";
			var shader = GD.Load<Shader>(path);
			if (shader != null)
				_locationMaterials[location.LocationId] = new ShaderMaterial { Shader = shader };
		}
	}

	private void ApplyLocationEffect(string locationId)
	{
		if (_locationEffectRect == null) return;
		_locationMaterials.TryGetValue(locationId, out var material);
		_locationEffectRect.Material = material;
	}

	// ── Transition overlay ───────────────────────────────────────────────────

	private void SetupTransitionOverlay()
	{
		var overlayLayer = new CanvasLayer();
		overlayLayer.Layer = 210;
		AddChild(overlayLayer);

		_transitionRect = new ColorRect();
		_transitionRect.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		_transitionRect.Color = Colors.Black;
		_transitionRect.MouseFilter = Control.MouseFilterEnum.Ignore;
		_transitionRect.Modulate = new Color(1, 1, 1, 0);
		overlayLayer.AddChild(_transitionRect);
	}

	private async Task FadeOut(float duration = 0.4f)
	{
		if (_transitionRect == null) return;
		var tween = CreateTween();
		tween.TweenProperty(_transitionRect, "modulate:a", 1.0f, duration)
			 .SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Quad);
		await ToSignal(tween, Tween.SignalName.Finished);
	}

	private async Task FadeIn(float duration = 0.4f)
	{
		if (_transitionRect == null) return;
		var tween = CreateTween();
		tween.TweenProperty(_transitionRect, "modulate:a", 0.0f, duration)
			 .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);
		await ToSignal(tween, Tween.SignalName.Finished);
	}
}
