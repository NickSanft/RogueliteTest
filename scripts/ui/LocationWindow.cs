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

	private LocationResource? _selectedLocation;
	private ButtonGroup _buttonGroup = new();

	public override void _Ready()
	{
		_locationList = GetNode<VBoxContainer>("%LocationList");
		_locationName = GetNode<Label>("%LocationName");
		_locationImage = GetNode<TextureRect>("%LocationImage");
		_locationDescription = GetNode<RichTextLabel>("%LocationDescription");
		_investigateButton = GetNode<Button>("%InvestigateButton");

		// Close button routes through WindowManager so the stack stays consistent
		GetNode<Button>("%CloseButton").Pressed += () => WindowManager.Instance?.Pop();
		_investigateButton.Pressed += OnInvestigatePressed;

		Visible = false;
	}

	public void ShowLocations(List<LocationResource> locations)
	{
		foreach (Node child in _locationList!.GetChildren())
			child.QueueFree();

		foreach (var location in locations)
		{
			var button = new Button();
			button.Text = $"{location.LocationName} ({location.TurnCost} turn{(location.TurnCost > 1 ? "s" : "")})";
			button.ToggleMode = true;
			button.ButtonGroup = _buttonGroup;
			button.SizeFlagsHorizontal = SizeFlags.Fill;
			button.Pressed += () => OnLocationSelected(location);
			ApplyButtonTextStyle(button);
			_locationList.AddChild(button);
		}

		if (locations.Count > 0)
		{
			OnLocationSelected(locations[0]);
			if (_locationList.GetChild(0) is Button firstButton)
				firstButton.ButtonPressed = true;
		}

		// Route through WindowManager so it can track the open window
		WindowManager.Instance?.Push(this);
		if (WindowManager.Instance == null)
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

		if (_locationImage != null)
		{
			if (!string.IsNullOrEmpty(location.ImagePath))
			{
				var texture = GD.Load<Texture2D>(location.ImagePath);
				_locationImage.Texture = texture;
				_locationImage.Visible = texture != null;
			}
			else
			{
				_locationImage.Visible = false;
			}
		}
	}

	private void OnInvestigatePressed()
	{
		if (_selectedLocation == null)
			return;

		// Pop before emitting so the window is gone before the transition begins
		WindowManager.Instance?.Pop();
		if (WindowManager.Instance == null)
			Visible = false;

		EmitSignal(SignalName.LocationInvestigated, _selectedLocation);
	}

	private static void ApplyButtonTextStyle(Button button)
	{
		button.AddThemeColorOverride("font_color", Colors.White);
		button.AddThemeColorOverride("font_outline_color", Colors.Black);
		button.AddThemeConstantOverride("outline_size", 2);
	}

	// ESC is now handled centrally by WindowManager; no _Input override needed
}
