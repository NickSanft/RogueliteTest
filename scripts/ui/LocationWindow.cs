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
		_locationList = GetNode<VBoxContainer>("%LocationList");
		_locationName = GetNode<Label>("%LocationName");
		_locationImage = GetNode<TextureRect>("%LocationImage");
		_locationDescription = GetNode<RichTextLabel>("%LocationDescription");
		_investigateButton = GetNode<Button>("%InvestigateButton");
		
		var closeButton = GetNode<Button>("MarginContainer/VBoxContainer/CloseButton");
		closeButton.Pressed += OnClosePressed;
		
		_investigateButton.Pressed += OnInvestigatePressed;
		
		Visible = false;
	}

	public void ShowLocations(List<LocationResource> locations)
	{
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
			OnLocationSelected(locations[0]);
			if (_locationList?.GetChild(0) is Button firstButton)
				firstButton.ButtonPressed = true;
		}
		
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
		if (_selectedLocation != null)
		{
			EmitSignal(SignalName.LocationInvestigated, _selectedLocation);
			Hide();
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
