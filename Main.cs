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
		_gameManager = GetNode<GameManager>("/root/GameManager");
		_eventManager = GetNode<EventManager>("/root/EventManager");

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

		_gameManager.StatChanged += OnStatChanged;
		_gameManager.GameOver += OnGameOver;

		LoadLocations();

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
		// Press SPACE to show test event
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

		// Press TAB to show location selection
		if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo && keyEvent.Keycode == Key.Tab)
		{
			_locationWindow?.ShowLocations(_availableLocations);
			GetViewport().SetInputAsHandled();
		}
	}

	private void OnLocationInvestigated(LocationResource location)
	{
		_hud?.UpdateLocation(location.LocationName);

		_currentTurn += location.TurnCost;
		_hud?.UpdateTurn(_currentTurn);

		_gameManager?.ModifyStat("doom", location.TurnCost * 2);

		string? eventId = location.GetRandomEvent();
		if (eventId != null && _eventManager != null && _eventWindow != null)
		{
			var locationEvent = _eventManager.LoadEvent($"res://data/events/{eventId}.tres");
			if (locationEvent != null)
				_eventWindow.ShowEvent(locationEvent);
		}
	}

	private void OnStatChanged(string statName, int oldValue, int newValue)
	{
		GD.Print($"{statName.ToUpper()} changed: {oldValue} → {newValue}");
	}

	private void OnGameOver(string reason)
	{
		GD.Print($"GAME OVER: {reason}");
		GetTree().Paused = true;
	}
}