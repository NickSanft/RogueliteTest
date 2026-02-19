using Godot;

public partial class Main : Node2D
{
	private EventWindow? _eventWindow;
	private GameManager? _gameManager;
	private EventManager? _eventManager;

	public override void _Ready()
	{
		// Get managers
		_gameManager = GetNode<GameManager>("/root/GameManager");
		_eventManager = GetNode<EventManager>("/root/EventManager");

		GD.Print($"GameManager loaded: {_gameManager != null}");
		GD.Print($"EventManager loaded: {_eventManager != null}");

		// Load and instance EventWindow
		var eventWindowScene = GD.Load<PackedScene>("res://scenes/ui/EventWindow.tscn");
		if (eventWindowScene == null)
		{
			GD.PrintErr("Failed to load EventWindow.tscn!");
			return;
		}

		// Create CanvasLayer for UI
		var uiLayer = new CanvasLayer();
		uiLayer.Layer = 10; // Render above game content
		AddChild(uiLayer);

		_eventWindow = eventWindowScene.Instantiate<EventWindow>();
		uiLayer.AddChild(_eventWindow);
		GD.Print($"EventWindow created: {_eventWindow != null}");

		// Connect to GameManager signals
		_gameManager.StatChanged += OnStatChanged;
		_gameManager.GameOver += OnGameOver;

		// Display initial stats
		GD.Print("=== Cosmic Horror RPG Started ===");
		GD.Print($"Stamina: {_gameManager.Stamina}/{_gameManager.MaxStamina}");
		GD.Print($"Reason: {_gameManager.Reason}/{_gameManager.MaxReason}");
		GD.Print($"Doom: {_gameManager.Doom}/100");
		GD.Print("\nPress SPACE to trigger test event");
	}

	public override void _Input(InputEvent @event)
	{
		// Press SPACE to show test event
		if (@event.IsActionPressed("ui_accept"))
		{
			GD.Print("SPACE pressed!");
			GD.Print($"EventWindow exists: {_eventWindow != null}");
			GD.Print($"EventManager exists: {_eventManager != null}");

			if (_eventWindow != null && _eventManager != null)
			{
				GD.Print("Attempting to load event...");
				var testEvent = _eventManager.LoadEvent("res://data/events/test_dark_room.tres");
				GD.Print($"Event loaded: {testEvent != null}");

				if (testEvent != null)
				{
					GD.Print("Showing event window...");
					_eventWindow.ShowEvent(testEvent);
				}
				else
				{
					GD.PrintErr("Failed to load event resource!");
				}
			}
			GetViewport().SetInputAsHandled();
		}
	}

	private void OnStatChanged(string statName, int oldValue, int newValue)
	{
		GD.Print($"{statName.ToUpper()} changed: {oldValue} → {newValue}");
	}

	private void OnGameOver(string reason)
	{
		GD.Print($"\n!!! GAME OVER !!!\n{reason}");
	}
}
