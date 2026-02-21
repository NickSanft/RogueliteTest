using Godot;
using System.Collections.Generic;

/// <summary>
/// Displays available locations and allows player to investigate
/// </summary>
public partial class LocationWindow : Panel
{
	[Signal] public delegate void LocationInvestigatedEventHandler(LocationResource location);

	private VBoxContainer? _locationList;
	private Label? _locationName;
	private TextureRect? _locationImage;
	private RichTextLabel? _locationDescription;
	private Button? _investigateButton;
	
	private List<LocationResource> _availableLocations = new();
	private LocationResource? _selectedLocation;
	private ButtonGroup _buttonGroup = new();

	public override void _Ready()
	{
		GD.Print("LocationWindow _Ready() called");
		Initialize();
	}

	public void Initialize()
	{
		GD.Print("LocationWindow Initialize() called");

		// Use FindChild instead of GetNode for programmatically created UI
		_locationList = FindChild("LocationList", true, false) as VBoxContainer;
		_locationName = FindChild("LocationName", true, false) as Label;
		_locationImage = FindChild("LocationImage", true, false) as TextureRect;
		_locationDescription = FindChild("LocationDescription", true, false) as RichTextLabel;
		_investigateButton = FindChild("InvestigateButton", true, false) as Button;

		GD.Print($"Found nodes: List={_locationList != null}, Name={_locationName != null}, Desc={_locationDescription != null}, Btn={_investigateButton != null}");

		// Find close button
		var closeButton = FindChild("CloseButton", true, false) as Button;
		if (closeButton != null)
		{
			closeButton.Pressed += OnClosePressed;
			GD.Print("Close button connected");
		}
		else
		{
			GD.PrintErr("Close button not found!");
		}

		if (_investigateButton != null)
		{
			// Add test handler first
			_investigateButton.Pressed += () => GD.Print("TEST: Investigate button raw Pressed signal!");

			// Then add the real handler
			_investigateButton.Pressed += OnInvestigatePressed;
			GD.Print("Investigate button connected");
		}
		else
		{
			GD.PrintErr("Investigate button not found!");
		}

		Visible = false;
	}

	public void ShowLocations(List<LocationResource> locations)
	{
		GD.Print($"ShowLocations called with {locations.Count} locations");
		_availableLocations = locations;

		// Clear existing buttons
		if (_locationList != null)
		{
			foreach (Node child in _locationList.GetChildren())
			{
				child.QueueFree();
			}
		}

		// Create location buttons
		foreach (var location in locations)
		{
			var button = new Button();
			button.Text = $"{location.LocationName} ({location.TurnCost} turn{(location.TurnCost > 1 ? "s" : "")})";
			button.ToggleMode = true;
			button.ButtonGroup = _buttonGroup;
			button.SizeFlagsHorizontal = SizeFlags.Fill;

			button.Pressed += () => OnLocationSelected(location);

			_locationList?.AddChild(button);
		}

		// Select first location by default
		if (locations.Count > 0)
		{
			GD.Print($"Auto-selecting first location: {locations[0].LocationName}");
			OnLocationSelected(locations[0]);
			if (_locationList?.GetChild(0) is Button firstButton)
				firstButton.ButtonPressed = true;
		}

		GD.Print($"Investigate button state: exists={_investigateButton != null}, disabled={_investigateButton?.Disabled ?? true}");

		Visible = true;
	}

	private void OnLocationSelected(LocationResource location)
	{
		_selectedLocation = location;
		
		if (_locationName != null)
			_locationName.Text = location.LocationName;
		
		if (_locationDescription != null)
			_locationDescription.Text = location.Description;
		
		if (_investigateButton != null)
			_investigateButton.Text = $"Investigate ({location.TurnCost} turn{(location.TurnCost > 1 ? "s" : "")})";
		
		// Load image if available
		if (_locationImage != null && !string.IsNullOrEmpty(location.ImagePath))
		{
			var texture = GD.Load<Texture2D>(location.ImagePath);
			if (texture != null)
			{
				_locationImage.Texture = texture;
				_locationImage.Visible = true;
			}
			else
			{
				_locationImage.Visible = false;
			}
		}
		else if (_locationImage != null)
		{
			_locationImage.Visible = false;
		}
	}

	private void OnInvestigatePressed()
	{
		GD.Print("Investigate button pressed!");
		GD.Print($"Selected location: {_selectedLocation?.LocationName ?? "null"}");

		if (_selectedLocation != null)
		{
			GD.Print($"Emitting LocationInvestigated signal for {_selectedLocation.LocationName}");
			EmitSignal(SignalName.LocationInvestigated, _selectedLocation);
			Hide();
		}
		else
		{
			GD.PrintErr("No location selected!");
		}
	}

	private void OnClosePressed()
	{
		Hide();
	}

	public override void _Input(InputEvent @event)
	{
		if (!Visible)
			return;
		
		if (@event.IsActionPressed("ui_cancel"))
		{
			Hide();
			GetViewport().SetInputAsHandled();
		}
	}
}
