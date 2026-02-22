using Godot;
using System;

/// <summary>
/// Displays event text, image, and player choices
/// </summary>
public partial class EventWindow : Panel
{
	private RichTextLabel? _eventText;
	private TextureRect? _eventImage;
	private VBoxContainer? _optionsContainer;
	private Button? _closeButton;
	
	private EventResource? _currentEvent;
	private GameManager? _gameManager;

	public override void _Ready()
	{
		_eventText = GetNode<RichTextLabel>("%EventText");
		_eventImage = GetNode<TextureRect>("%EventImage");
		_optionsContainer = GetNode<VBoxContainer>("%OptionsContainer");
		_closeButton = GetNode<Button>("%CloseButton");
		
		_gameManager = GetNode<GameManager>("/root/GameManager");
		
		_closeButton.Pressed += OnClosePressed;
		
		Visible = false;
	}

	/// <summary>
	/// Display an event with options
	/// </summary>
	public void ShowEvent(EventResource eventResource)
	{
		_currentEvent = eventResource;
		
		// Set event text
		if (_eventText != null)
			_eventText.Text = eventResource.EventText;
		
		// Load and display event image
		if (_eventImage != null && !string.IsNullOrEmpty(eventResource.EventImagePath))
		{
			var texture = GD.Load<Texture2D>(eventResource.EventImagePath);
			if (texture != null)
			{
				_eventImage.Texture = texture;
				_eventImage.Visible = true;
			}
			else
			{
				_eventImage.Visible = false;
			}
		}
		else if (_eventImage != null)
		{
			_eventImage.Visible = false;
		}
		
		// Clear existing option buttons
		ClearOptions();
		
		// Create option buttons
		for (int i = 0; i < eventResource.Options.Count; i++)
		{
			var option = eventResource.Options[i];
			var button = new Button();
			button.Text = FormatOptionText(option, i);
			button.SizeFlagsHorizontal = SizeFlags.Fill;
			
			int optionIndex = i; // Capture for closure
			button.Pressed += () => OnOptionSelected(optionIndex);
			
			_optionsContainer?.AddChild(button);
		}
		
		// Apply auto-consequences (immediate effects)
		if (_gameManager != null)
		{
			foreach (var consequence in eventResource.AutoConsequences)
			{
				consequence.Apply(_gameManager);
			}
		}
		
		Visible = true;
	}

	private string FormatOptionText(EventOption option, int index)
	{
		string text = $"[{index + 1}] {option.OptionText}";
		
		if (option.StatCheck != null)
		{
			string statName = option.StatCheck.Stat switch
			{
				StatCheck.StatType.Stamina => "Stamina",
				StatCheck.StatType.Reason => "Reason",
				StatCheck.StatType.Doom => "Doom",
				_ => "Unknown"
			};
			
			string checkType = option.StatCheck.Type switch
			{
				StatCheck.CheckType.FixedThreshold => $"≥{option.StatCheck.Threshold}",
				StatCheck.CheckType.DiceRoll => $"d{option.StatCheck.DiceSides}",
				_ => ""
			};
			
			text += $" [{statName} {checkType}]";
		}
		
		return text;
	}

	private void OnOptionSelected(int optionIndex)
	{
		if (_currentEvent == null || _gameManager == null)
			return;

		var option = _currentEvent.GetOption(optionIndex);
		if (option == null)
			return;

		// Evaluate stat check
		bool passed = option.EvaluateStatCheck(_gameManager.GetPlayerStats());

		GD.Print($"Option {optionIndex + 1} selected - Check {(passed ? "PASSED" : "FAILED")}");

		// Apply consequences
		foreach (var consequence in option.Consequences)
		{
			consequence.Apply(_gameManager);
		}

		// Show result text
		ShowResult(option, passed);
	}

	private void ShowResult(EventOption option, bool passed)
	{
		// Display the result text
		string resultText = passed ? option.SuccessText : option.FailureText;

		// If no result text is defined, provide a default message
		if (string.IsNullOrEmpty(resultText))
		{
			resultText = passed ? "Success!" : "You proceed cautiously...";
		}

		if (_eventText != null)
			_eventText.Text = resultText;

		// Clear options and show continue button
		ClearOptions();

		var continueButton = new Button();
		continueButton.Text = "[SPACE] Continue";
		continueButton.SizeFlagsHorizontal = SizeFlags.Fill;
		continueButton.Pressed += HideEvent;

		_optionsContainer?.AddChild(continueButton);
	}

	private void ClearOptions()
	{
		if (_optionsContainer == null)
			return;
		
		foreach (Node child in _optionsContainer.GetChildren())
		{
			child.QueueFree();
		}
	}

	private void OnClosePressed()
	{
		HideEvent();
	}

	public void HideEvent()
	{
		Visible = false;
		_currentEvent = null;
	}

	public override void _Input(InputEvent @event)
	{
		if (!Visible)
			return;

		// Close on Escape or Space (for result screen)
		if (@event.IsActionPressed("ui_cancel") || @event.IsActionPressed("ui_accept"))
		{
			// If showing result (no current event options), close
			if (_optionsContainer?.GetChildCount() == 1)
			{
				HideEvent();
				GetViewport().SetInputAsHandled();
				return;
			}
		}

		// Close on Escape
		if (@event.IsActionPressed("ui_cancel"))
		{
			HideEvent();
			GetViewport().SetInputAsHandled();
		}

		// Number key shortcuts for options (1-9)
		if (_currentEvent != null)
		{
			for (int i = 0; i < Math.Min(_currentEvent.Options.Count, 9); i++)
			{
				if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
				{
					if (keyEvent.Keycode == (Key)((int)Key.Key1 + i))
					{
						OnOptionSelected(i);
						GetViewport().SetInputAsHandled();
						break;
					}
				}
			}
		}
	}
}
