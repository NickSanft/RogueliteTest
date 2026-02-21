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

        // Clear options to prevent double-clicking
        ClearOptions();

        // Build result message
        string resultText = $"\n[color=yellow]--- RESULT ---[/color]\n";

        // Evaluate stat check
        bool passed = option.EvaluateStatCheck(_gameManager.GetPlayerStats());

        if (option.StatCheck != null)
        {
            string statName = option.StatCheck.Stat switch
            {
                StatCheck.StatType.Stamina => "Stamina",
                StatCheck.StatType.Reason => "Reason",
                StatCheck.StatType.Doom => "Doom",
                _ => "Unknown"
            };

            if (passed)
            {
                resultText += $"[color=green]✓ {statName} check PASSED![/color]\n";
            }
            else
            {
                resultText += $"[color=red]✗ {statName} check FAILED![/color]\n";
            }
        }

        // Apply consequences and show them
        foreach (var consequence in option.Consequences)
        {
            string consequenceMsg = GetConsequenceMessage(consequence);
            if (!string.IsNullOrEmpty(consequenceMsg))
                resultText += consequenceMsg + "\n";

            consequence.Apply(_gameManager);
        }

        // Show result in event text
        if (_eventText != null)
        {
            _eventText.Text += resultText;
        }

        // Add close button
        var closeButton = new Button();
        closeButton.Text = "Continue [SPACE]";
        closeButton.Pressed += HideEvent;
        _optionsContainer?.AddChild(closeButton);

        GD.Print($"Option {optionIndex + 1} selected - Check {(passed ? "PASSED" : "FAILED")}");
    }

    private string GetConsequenceMessage(EventConsequence consequence)
    {
        return consequence.Type switch
        {
            EventConsequence.ConsequenceType.StatChange =>
                $"[color=cyan]{consequence.StatName.ToUpper()} {(consequence.Value >= 0 ? "+" : "")}{consequence.Value}[/color]",
            EventConsequence.ConsequenceType.ItemGain =>
                $"[color=yellow]Gained item: {consequence.ItemId}[/color]",
            EventConsequence.ConsequenceType.AdvanceMystery =>
                $"[color=magenta]Mystery progress +{consequence.MysteryProgress}[/color]",
            EventConsequence.ConsequenceType.TriggerEvent =>
                "[color=orange]Another event unfolds...[/color]",
            _ => ""
        };
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
