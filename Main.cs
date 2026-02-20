using Godot;
using System.Collections.Generic;

public partial class Main : Node2D
{
	private EventWindow? _eventWindow;
	private LocationWindow? _locationWindow;
	private HUD? _hud;
	private GameManager? _gameManager;
	private EventManager? _eventManager;

	private List<LocationResource> _availableLocations = new();
	private int _currentTurn = 1;

	public override void _Ready()
	{
		// Get managers
		_gameManager = GetNode<GameManager>("/root/GameManager");
		_eventManager = GetNode<EventManager>("/root/EventManager");

		GD.Print($"GameManager loaded: {_gameManager != null}");
		GD.Print($"EventManager loaded: {_eventManager != null}");

		// Create UI Layer
		var uiLayer = new CanvasLayer();
		uiLayer.Layer = 10;
		AddChild(uiLayer);

		// Load and instance EventWindow
		var eventWindowScene = GD.Load<PackedScene>("res://scenes/ui/EventWindow.tscn");
		if (eventWindowScene == null)
		{
			GD.PrintErr("Failed to load EventWindow.tscn!");
			return;
		}

		_eventWindow = eventWindowScene.Instantiate<EventWindow>();
		uiLayer.AddChild(_eventWindow);
		GD.Print($"EventWindow created: {_eventWindow != null}");

		// Create LocationWindow programmatically (avoiding .tscn parsing issues)
		_locationWindow = CreateLocationWindow();
		_locationWindow.LocationInvestigated += OnLocationInvestigated;
		uiLayer.AddChild(_locationWindow);
		GD.Print($"LocationWindow created: {_locationWindow != null}");

		// Load and instance HUD
		var hudScene = GD.Load<PackedScene>("res://scenes/ui/HUD.tscn");
		if (hudScene != null)
		{
			_hud = hudScene.Instantiate<HUD>();
			AddChild(_hud);
			GD.Print($"HUD created: {_hud != null}");
		}

		// Connect to GameManager signals
		_gameManager.StatChanged += OnStatChanged;
		_gameManager.GameOver += OnGameOver;

		// Load available locations
		LoadLocations();

		// Display initial stats
		GD.Print("=== Cosmic Horror RPG Started ===");
		GD.Print($"Stamina: {_gameManager.Stamina}/{_gameManager.MaxStamina}");
		GD.Print($"Reason: {_gameManager.Reason}/{_gameManager.MaxReason}");
		GD.Print($"Doom: {_gameManager.Doom}/100");
		GD.Print("\n[SPACE] Test Event  [TAB] Locations");

		if (_hud != null)
			_hud.UpdateLocation("Town Square");
	}

	private void LoadLocations()
	{
		// Load location resources
		GD.Print("Attempting to load locations...");

		var oldLibrary = GD.Load<LocationResource>("res://data/locations/old_library.tres");
		GD.Print($"Old Library loaded: {oldLibrary != null}");

		var abandonedShrine = GD.Load<LocationResource>("res://data/locations/abandoned_shrine.tres");
		GD.Print($"Abandoned Shrine loaded: {abandonedShrine != null}");

		var coastalCliff = GD.Load<LocationResource>("res://data/locations/coastal_cliff.tres");
		GD.Print($"Coastal Cliff loaded: {coastalCliff != null}");

		if (oldLibrary != null) _availableLocations.Add(oldLibrary);
		if (abandonedShrine != null) _availableLocations.Add(abandonedShrine);
		if (coastalCliff != null) _availableLocations.Add(coastalCliff);

		GD.Print($"Loaded {_availableLocations.Count} locations");
	}

	public override void _Input(InputEvent @event)
	{
		// Debug all key presses
		if (@event is InputEventKey key && key.Pressed && !key.Echo)
		{
			GD.Print($"Key pressed: {key.Keycode}");
		}

		// Press SPACE to show test event
		if (@event.IsActionPressed("ui_accept"))
		{
			GD.Print("SPACE pressed!");

			if (_eventWindow != null && _eventManager != null)
			{
				var testEvent = _eventManager.LoadEvent("res://data/events/test_dark_room.tres");

				if (testEvent != null)
				{
					_eventWindow.ShowEvent(testEvent);
				}
				else
				{
					GD.PrintErr("Failed to load event resource!");
				}
			}
			GetViewport().SetInputAsHandled();
		}

		// Press TAB to show location selection
		if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
		{
			GD.Print($"Checking TAB: Keycode={keyEvent.Keycode}, LocationWindow={_locationWindow != null}, AvailableLocations={_availableLocations.Count}");

			if (keyEvent.Keycode == Key.Tab)
			{
				GD.Print("TAB detected!");

				if (_locationWindow != null)
				{
					GD.Print($"Showing locations: {_availableLocations.Count}");
					_locationWindow.ShowLocations(_availableLocations);
				}
				else
				{
					GD.PrintErr("LocationWindow is null!");
				}

				GetViewport().SetInputAsHandled();
			}
		}
	}

	private void OnLocationInvestigated(LocationResource location)
	{
		GD.Print($"Investigating {location.LocationName}...");

		// Update HUD
		if (_hud != null)
			_hud.UpdateLocation(location.LocationName);

		// Advance turn
		_currentTurn += location.TurnCost;
		if (_hud != null)
			_hud.UpdateTurn(_currentTurn);

		// Increase doom slightly per turn
		_gameManager?.ModifyStat("doom", location.TurnCost * 2);

		// Trigger random event from location
		string? eventId = location.GetRandomEvent();
		if (eventId != null && _eventManager != null && _eventWindow != null)
		{
			var locationEvent = _eventManager.LoadEvent($"res://data/events/{eventId}.tres");
			if (locationEvent != null)
			{
				_eventWindow.ShowEvent(locationEvent);
			}
		}
	}

	private void OnStatChanged(string statName, int oldValue, int newValue)
	{
		GD.Print($"{statName.ToUpper()} changed: {oldValue} → {newValue}");
	}

	private void OnGameOver(string reason)
	{
		GD.Print($"\n!!! GAME OVER !!!\n{reason}");

		// Show game over screen (to be implemented)
		GetTree().Paused = true;
	}

	private LocationWindow CreateLocationWindow()
	{
		var script = GD.Load<CSharpScript>("res://scripts/ui/LocationWindow.cs");
		var window = new LocationWindow();
		window.SetScript(script);
		return window;
	}
}
