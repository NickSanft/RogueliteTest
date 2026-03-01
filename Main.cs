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

	private ColorRect? _locationEffectRect;
	private Dictionary<string, ShaderMaterial> _locationMaterials = new();

	private ColorRect? _transitionRect;
	private WindowManager? _windowManager;

	// End screen (game-over and win)
	private Control? _endScreen;
	private Label? _endScreenTitle;
	private Label? _endScreenBody;

	// Deferred win text — stored so it can be shown after the current event closes
	private string _pendingWinText = "";

	// Deferred game-over — defers end screen until any open EventWindow closes
	private string _pendingGameOverStats = "";
	private bool _gameOverTriggered = false;

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
		SetupEndScreen();

		_hud?.UpdateLocation(_gameManager.LastLocationName);
		_hud?.UpdateTurn(_gameManager.CurrentTurn);
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

		// TAB — show location selection
		if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo && keyEvent.Keycode == Key.Tab)
		{
			_locationWindow?.ShowLocations(_availableLocations);
			GetViewport().SetInputAsHandled();
		}
	}

	private async void OnLocationInvestigated(LocationResource location)
	{
		if (_gameOverTriggered || _endScreen?.Visible == true) return;

		await FadeOut();

		_hud?.UpdateLocation(location.LocationName);
		_gameManager!.LastLocationName = location.LocationName;
		_gameManager!.CurrentTurn += location.TurnCost;
		_hud?.UpdateTurn(_gameManager.CurrentTurn);
		// Doom = location cost + passive pressure per turn elapsed
		_gameManager.ModifyStat("doom", location.TurnCost * (2 + GameManager.PassiveDoomPerTurn));
		ApplyLocationEffect(location.LocationId);

		// Filter events by precondition (RequiredItem, MinDoom on EventResource)
		string? eventId = location.GetRandomEvent(
			_gameManager.SeenEvents,
			id =>
			{
				var ev = _eventManager?.LoadEventById(id);
				if (ev == null) return true;
				if (!string.IsNullOrEmpty(ev.RequiredItem) && !_gameManager.Inventory.Contains(ev.RequiredItem)) return false;
				if (ev.MinDoom > 0 && _gameManager.Doom < ev.MinDoom) return false;
				return true;
			});
		if (eventId != null)
			_gameManager.MarkEventSeen(eventId);

		_gameManager.SaveGame();

		await FadeIn();

		if (eventId != null && _eventManager != null && _eventWindow != null)
		{
			var locationEvent = _eventManager.LoadEvent($"res://data/events/{eventId}.tres");
			if (locationEvent != null)
			{
				_eventWindow.SetLocationContext(location.LocationName);
				_eventWindow.ShowEvent(locationEvent);
			}
		}
	}

	// ── Gameplay signal handlers ──────────────────────────────────────────────

	private void OnStatChanged(string statName, int oldValue, int newValue)
	{
		GD.Print($"{statName.ToUpper()} changed: {oldValue} → {newValue}");
	}

	private void OnGameOver(string reason)
	{
		if (_gameOverTriggered || _endScreen?.Visible == true) return;
		_gameOverTriggered = true;

		var gm = _gameManager;
		_pendingGameOverStats = gm != null
			? $"{reason}\n\n{gm.FormatRunSummary()}"
			: reason;

		if (_eventWindow?.Visible == true)
			_eventWindow.VisibilityChanged += ShowGameOverWhenReady;
		else
			ShowEndScreen("INVESTIGATION FAILED", _pendingGameOverStats);
	}

	private void ShowGameOverWhenReady()
	{
		if (_eventWindow?.Visible != false) return;
		_eventWindow.VisibilityChanged -= ShowGameOverWhenReady;
		ShowEndScreen("INVESTIGATION FAILED", _pendingGameOverStats);
	}

	private void OnMysteryCompleted(string mysteryId, string completionText)
	{
		GD.Print($"Mystery completed: {mysteryId}");

		// If more mysteries remain, show the completion text as a queued event.
		// The last mystery's completion is handled by OnRunWon instead.
		if (_gameManager?.ActiveMysteries.Count > 0)
		{
			var notice = new EventResource();
			notice.EventId   = "mystery_complete_notice";
			notice.EventText = completionText;

			var cont = new EventOption();
			cont.OptionText  = "Continue the investigation.";
			cont.SuccessText = "The work is not finished. There is always more to learn — and always a cost for learning it.";
			notice.Options.Add(cont);

			_gameManager.QueueEventResource(notice);
		}
	}

	private void OnRunWon(string winText)
	{
		_pendingWinText = winText;

		// If an event is currently showing, wait for it to close before displaying
		// the win event — pushing EventWindow while it's already on the stack would
		// be silently ignored by WindowManager's deduplication check.
		if (_eventWindow?.Visible == true)
			_eventWindow.VisibilityChanged += ShowWinEventWhenReady;
		else
			ShowWinEvent();
	}

	private void ShowWinEventWhenReady()
	{
		if (_eventWindow?.Visible != false) return;
		_eventWindow.VisibilityChanged -= ShowWinEventWhenReady;
		ShowWinEvent();
	}

	private void ShowWinEvent()
	{
		if (_eventWindow == null) return;

		var winEvent = new EventResource();
		winEvent.EventId   = "run_won";
		winEvent.EventText = _pendingWinText;

		var endOption = new EventOption();
		endOption.OptionText  = "The investigation concludes.";
		endOption.SuccessText = "You carry the knowledge out of the abyss. Some part of you has stayed behind.";
		winEvent.Options.Add(endOption);

		_eventWindow.ShowEvent(winEvent);
		_eventWindow.VisibilityChanged += OnWinEventClosed;
	}

	private void OnWinEventClosed()
	{
		if (_eventWindow?.Visible != false) return;
		_eventWindow.VisibilityChanged -= OnWinEventClosed;

		string stats = _gameManager?.FormatRunSummary() ?? "";
		ShowEndScreen("INVESTIGATION COMPLETE", stats);
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

	// ── End screen (game-over / win) ─────────────────────────────────────────

	private void SetupEndScreen()
	{
		var layer = new CanvasLayer();
		layer.Layer = 220;
		AddChild(layer);

		_endScreen = new Panel();
		_endScreen.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		_endScreen.MouseFilter = Control.MouseFilterEnum.Stop;
		var bg = new StyleBoxFlat();
		bg.BgColor = new Color(0.03f, 0.03f, 0.07f, 0.95f);
		((Panel)_endScreen).AddThemeStyleboxOverride("panel", bg);
		layer.AddChild(_endScreen);

		var center = new CenterContainer();
		center.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		_endScreen.AddChild(center);

		var vbox = new VBoxContainer();
		vbox.CustomMinimumSize = new Vector2(560, 0);
		vbox.AddThemeConstantOverride("separation", 20);
		center.AddChild(vbox);

		_endScreenTitle = new Label();
		_endScreenTitle.HorizontalAlignment = HorizontalAlignment.Center;
		_endScreenTitle.AddThemeColorOverride("font_color", Colors.White);
		_endScreenTitle.AddThemeColorOverride("font_outline_color", Colors.Black);
		_endScreenTitle.AddThemeConstantOverride("outline_size", 2);
		vbox.AddChild(_endScreenTitle);

		vbox.AddChild(new HSeparator());

		_endScreenBody = new Label();
		_endScreenBody.AutowrapMode = TextServer.AutowrapMode.Word;
		_endScreenBody.CustomMinimumSize = new Vector2(560, 0);
		_endScreenBody.HorizontalAlignment = HorizontalAlignment.Center;
		_endScreenBody.AddThemeColorOverride("font_color", Colors.White);
		_endScreenBody.AddThemeColorOverride("font_outline_color", Colors.Black);
		_endScreenBody.AddThemeConstantOverride("outline_size", 2);
		vbox.AddChild(_endScreenBody);

		vbox.AddChild(new HSeparator());

		var button = new Button();
		button.Text = "Begin New Investigation";
		button.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
		button.AddThemeColorOverride("font_color", Colors.White);
		button.AddThemeColorOverride("font_outline_color", Colors.Black);
		button.AddThemeConstantOverride("outline_size", 2);
		button.Pressed += OnEndScreenContinue;
		vbox.AddChild(button);

		_endScreen.Visible = false;
	}

	private void ShowEndScreen(string title, string body)
	{
		if (_endScreenTitle != null) _endScreenTitle.Text = title;
		if (_endScreenBody  != null) _endScreenBody.Text  = body;
		if (_endScreen      != null) _endScreen.Visible   = true;
	}

	private void OnEndScreenContinue()
	{
		_gameOverTriggered = false;
		_gameManager?.ResetGame();
		Callable.From(() => GetTree().ReloadCurrentScene()).CallDeferred();
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
