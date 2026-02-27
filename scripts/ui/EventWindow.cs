using Godot;
using System;
using System.Collections.Generic;

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
	private string _currentLocationName = "";
	private readonly List<int> _visibleOptionIndices = new();

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
	/// Sets the location name shown as a dim header above event text.
	/// Call before ShowEvent when the event originates from a location visit.
	/// </summary>
	public void SetLocationContext(string locationName)
	{
		_currentLocationName = locationName;
	}

	/// <summary>
	/// Display an event with options
	/// </summary>
	public void ShowEvent(EventResource eventResource)
	{
		_currentEvent = eventResource;
		_visibleOptionIndices.Clear();

		if (_eventText != null)
		{
			string header = string.IsNullOrEmpty(_currentLocationName)
				? ""
				: $"[color=#888888][{_currentLocationName}][/color]\n\n";
			_eventText.Text = header + eventResource.EventText;
		}

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

		int doom = _gameManager?.Doom ?? 0;
		int buttonIndex = 0;
		for (int i = 0; i < eventResource.Options.Count; i++)
		{
			var option = eventResource.Options[i];

			// Skip options the player doesn't qualify for
			if (!string.IsNullOrEmpty(option.RequiredItem) &&
				(_gameManager == null || !_gameManager.Inventory.Contains(option.RequiredItem)))
				continue;
			if (option.MinDoom > 0 && doom < option.MinDoom)
				continue;

			_visibleOptionIndices.Add(i);

			var button = new Button();
			button.Text = FormatOptionText(option, buttonIndex);
			button.SizeFlagsHorizontal = SizeFlags.Fill;
			ApplyButtonTextStyle(button);

			int realIndex = i;
			button.Pressed += () => OnOptionSelected(realIndex);

			_optionsContainer?.AddChild(button);
			buttonIndex++;
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

		_visibleOptionIndices.Clear();
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

		// Dynamic (code-built) events take priority over ID-based events
		if (_gameManager.EventResourceQueue.Count > 0)
		{
			var nextEvent = _gameManager.EventResourceQueue[0];
			_gameManager.EventResourceQueue.RemoveAt(0);
			CallDeferred("ShowEvent", nextEvent);
			return;
		}

		if (_gameManager.EventQueue.Count == 0) return;

		string nextId = _gameManager.EventQueue[0];
		_gameManager.EventQueue.RemoveAt(0);

		var ev = _eventManager.LoadEventById(nextId);
		if (ev != null)
			CallDeferred("ShowEvent", ev);
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

		// Number keys 1–9 select visible options
		if (_visibleOptionIndices.Count > 0 && @event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
		{
			for (int k = 0; k < Math.Min(_visibleOptionIndices.Count, 9); k++)
			{
				if (keyEvent.Keycode == (Key)((int)Key.Key1 + k))
				{
					OnOptionSelected(_visibleOptionIndices[k]);
					GetViewport().SetInputAsHandled();
					break;
				}
			}
		}
	}
}
