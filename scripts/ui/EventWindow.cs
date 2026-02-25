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
	private EventManager? _eventManager;

	public override void _Ready()
	{
		_eventText = GetNode<RichTextLabel>("%EventText");
		_eventImage = GetNode<TextureRect>("%EventImage");
		_optionsContainer = GetNode<VBoxContainer>("%OptionsContainer");
		_closeButton = GetNode<Button>("%CloseButton");

		_gameManager = GetNode<GameManager>("/root/GameManager");
		_eventManager = GetNode<EventManager>("/root/EventManager");

		_closeButton.Pressed += OnClosePressed;

		// Clear state whenever the window is hidden; also drain the event queue
		VisibilityChanged += () =>
		{
			if (!Visible)
			{
				_currentEvent = null;
				ProcessEventQueue();
			}
		};

		Visible = false;
	}

	/// <summary>
	/// Display an event with options
	/// </summary>
	public void ShowEvent(EventResource eventResource)
	{
		_currentEvent = eventResource;

		if (_eventText != null)
			_eventText.Text = eventResource.EventText;

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

		ClearOptions();

		for (int i = 0; i < eventResource.Options.Count; i++)
		{
			var option = eventResource.Options[i];
			var button = new Button();
			button.Text = FormatOptionText(option, i);
			button.SizeFlagsHorizontal = SizeFlags.Fill;
			ApplyButtonTextStyle(button);

			int optionIndex = i;
			button.Pressed += () => OnOptionSelected(optionIndex);

			_optionsContainer?.AddChild(button);
		}

		if (_gameManager != null)
		{
			foreach (var consequence in eventResource.AutoConsequences)
				consequence.Apply(_gameManager);
		}

		WindowManager.Instance?.Push(this);
		if (WindowManager.Instance == null)
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
				StatCheck.StatType.Reason  => "Reason",
				StatCheck.StatType.Doom    => "Doom",
				_                          => "Unknown"
			};

			string checkType = option.StatCheck.Type switch
			{
				StatCheck.CheckType.FixedThreshold => $"≥{option.StatCheck.Threshold}",
				StatCheck.CheckType.DiceRoll       => $"d{option.StatCheck.DiceSides}",
				_                                  => ""
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

		bool passed = option.EvaluateStatCheck(_gameManager.GetPlayerStats());

		foreach (var consequence in option.Consequences)
			consequence.Apply(_gameManager);

		var outcomeConsequences = passed ? option.SuccessConsequences : option.FailureConsequences;
		foreach (var consequence in outcomeConsequences)
			consequence.Apply(_gameManager);

		ShowResult(option, passed);
	}

	private void ShowResult(EventOption option, bool passed)
	{
		string resultText = passed ? option.SuccessText : option.FailureText;

		if (string.IsNullOrEmpty(resultText))
			resultText = passed ? "Success!" : "You proceed cautiously...";

		if (_eventText != null)
			_eventText.Text = resultText;

		ClearOptions();

		var continueButton = new Button();
		continueButton.Text = "[SPACE] Continue";
		continueButton.SizeFlagsHorizontal = SizeFlags.Fill;
		continueButton.Pressed += HideEvent;
		ApplyButtonTextStyle(continueButton);

		_optionsContainer?.AddChild(continueButton);
	}

	private void ClearOptions()
	{
		if (_optionsContainer == null) return;
		foreach (Node child in _optionsContainer.GetChildren())
			child.QueueFree();
	}

	private void OnClosePressed() => HideEvent();

	public void HideEvent()
	{
		if (WindowManager.Instance != null)
			WindowManager.Instance.Pop();
		else
			Visible = false;
	}

	/// <summary>
	/// If consequences queued a follow-up event, show it after a frame
	/// so the current close animation finishes first.
	/// </summary>
	private void ProcessEventQueue()
	{
		if (_gameManager == null || _eventManager == null) return;
		if (_gameManager.EventQueue.Count == 0) return;

		string nextId = _gameManager.EventQueue[0];
		_gameManager.EventQueue.RemoveAt(0);

		var nextEvent = _eventManager.LoadEventById(nextId);
		if (nextEvent != null)
			CallDeferred("ShowEvent", nextEvent);
	}

	private static void ApplyButtonTextStyle(Button button)
	{
		button.AddThemeColorOverride("font_color", Colors.White);
		button.AddThemeColorOverride("font_outline_color", Colors.Black);
		button.AddThemeConstantOverride("outline_size", 2);
	}

	public override void _Input(InputEvent @event)
	{
		if (!Visible) return;

		// Space closes the result screen (single continue button)
		if (@event.IsActionPressed("ui_accept") && _optionsContainer?.GetChildCount() == 1)
		{
			HideEvent();
			GetViewport().SetInputAsHandled();
			return;
		}

		// Number keys 1–9 select options
		if (_currentEvent != null && @event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
		{
			for (int i = 0; i < Math.Min(_currentEvent.Options.Count, 9); i++)
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
