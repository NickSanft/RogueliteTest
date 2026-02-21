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

		// Manually initialize after adding to tree (since _Ready may not have been called yet)
		CallDeferred("InitializeLocationWindow");

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
		// Create locations programmatically (avoiding .tres parsing issues)
		GD.Print("Creating locations...");

		var oldLibrary = new LocationResource
		{
			LocationId = "old_library",
			LocationName = "Old Library",
			Description = "Dusty tomes line crumbling shelves. The air smells of decay and forgotten knowledge. Strange whispers echo between the stacks.",
			EventPool = new Godot.Collections.Array<string> { "test_dark_room" },
			TurnCost = 1,
			UnlockedByDefault = true
		};
		_availableLocations.Add(oldLibrary);

		var abandonedShrine = new LocationResource
		{
			LocationId = "abandoned_shrine",
			LocationName = "Abandoned Shrine",
			Description = "A forgotten temple to nameless gods. Blood stains mar the altar, and the shadows seem to move with malevolent purpose.",
			EventPool = new Godot.Collections.Array<string> { "test_dark_room" },
			TurnCost = 2,
			UnlockedByDefault = true
		};
		_availableLocations.Add(abandonedShrine);

		var coastalCliff = new LocationResource
		{
			LocationId = "coastal_cliff",
			LocationName = "Coastal Cliff",
			Description = "Jagged rocks overlook a churning black sea. Strange lights pulse beneath the waves. The wind carries inhuman songs.",
			EventPool = new Godot.Collections.Array<string> { "test_dark_room" },
			TurnCost = 1,
			UnlockedByDefault = true
		};
		_availableLocations.Add(coastalCliff);

		GD.Print($"Created {_availableLocations.Count} locations");
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
		var window = new LocationWindow();

		// Set anchors to fill screen
		window.AnchorRight = 1.0f;
		window.AnchorBottom = 1.0f;
		window.MouseFilter = Control.MouseFilterEnum.Stop; // Ensure panel captures mouse

		// Create UI structure
		var margin = new MarginContainer();
		margin.AnchorRight = 1.0f;
		margin.AnchorBottom = 1.0f;
		margin.MouseFilter = Control.MouseFilterEnum.Ignore; // Let clicks pass through
		margin.AddThemeConstantOverride("margin_left", 80);
		margin.AddThemeConstantOverride("margin_top", 60);
		margin.AddThemeConstantOverride("margin_right", 80);
		margin.AddThemeConstantOverride("margin_bottom", 60);
		window.AddChild(margin);

		var vbox = new VBoxContainer();
		vbox.AddThemeConstantOverride("separation", 16);
		margin.AddChild(vbox);

		// Title
		var title = new Label();
		title.Text = "SELECT LOCATION TO INVESTIGATE";
		title.HorizontalAlignment = HorizontalAlignment.Center;
		vbox.AddChild(title);

		vbox.AddChild(new HSeparator());

		// Main content
		var hbox = new HBoxContainer();
		hbox.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		hbox.AddThemeConstantOverride("separation", 24);
		vbox.AddChild(hbox);

		// Location list
		var locationList = new VBoxContainer();
		locationList.Name = "LocationList";
		locationList.UniqueNameInOwner = true;
		locationList.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		locationList.AddThemeConstantOverride("separation", 8);
		hbox.AddChild(locationList);

		hbox.AddChild(new VSeparator());

		// Details panel
		var details = new VBoxContainer();
		details.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		details.AddThemeConstantOverride("separation", 12);
		hbox.AddChild(details);

		var locationName = new Label();
		locationName.Name = "LocationName";
		locationName.UniqueNameInOwner = true;
		locationName.Text = "Location Name";
		locationName.HorizontalAlignment = HorizontalAlignment.Center;
		details.AddChild(locationName);

		var locationImage = new TextureRect();
		locationImage.Name = "LocationImage";
		locationImage.UniqueNameInOwner = true;
		locationImage.CustomMinimumSize = new Vector2(0, 200);
		locationImage.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
		locationImage.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
		details.AddChild(locationImage);

		var locationDesc = new RichTextLabel();
		locationDesc.Name = "LocationDescription";
		locationDesc.UniqueNameInOwner = true;
		locationDesc.CustomMinimumSize = new Vector2(0, 100);
		locationDesc.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		locationDesc.BbcodeEnabled = true;
		locationDesc.Text = "Location description...";
		locationDesc.FitContent = true;
		details.AddChild(locationDesc);

		var investigateBtn = new Button();
		investigateBtn.Name = "InvestigateButton";
		investigateBtn.UniqueNameInOwner = true;
		investigateBtn.Text = "Investigate (1 Turn)";
		investigateBtn.Disabled = false;
		investigateBtn.MouseFilter = Control.MouseFilterEnum.Stop;
		details.AddChild(investigateBtn);

		vbox.AddChild(new HSeparator());

		// Close button
		var closeBtn = new Button();
		closeBtn.Name = "CloseButton";
		closeBtn.Text = "Close [ESC]";
		closeBtn.MouseFilter = Control.MouseFilterEnum.Stop;
		vbox.AddChild(closeBtn);

		window.Visible = false;

		return window;
	}

	private void InitializeLocationWindow()
	{
		GD.Print("Manually initializing LocationWindow...");
		_locationWindow?.Initialize();
	}
}
