using Godot;

/// <summary>
/// Displays player stats, doom counter, and current location
/// </summary>
public partial class HUD : CanvasLayer
{
    private Label? _staminaLabel;
    private Label? _reasonLabel;
    private Label? _doomLabel;
    private Label? _turnLabel;
    private Label? _locationLabel;
    
    private GameManager? _gameManager;
    private int _currentTurn = 1;

    public override void _Ready()
    {
        _staminaLabel = GetNode<Label>("%StaminaLabel");
        _reasonLabel = GetNode<Label>("%ReasonLabel");
        _doomLabel = GetNode<Label>("%DoomLabel");
        _turnLabel = GetNode<Label>("%TurnLabel");
        _locationLabel = GetNode<Label>("%LocationLabel");
        
        _gameManager = GetNode<GameManager>("/root/GameManager");
        
        // Connect to stat changes
        _gameManager.StatChanged += OnStatChanged;
        
        UpdateAllStats();
    }

    private void OnStatChanged(string statName, int oldValue, int newValue)
    {
        UpdateAllStats();
    }

    private void UpdateAllStats()
    {
        if (_gameManager == null)
            return;
        
        if (_staminaLabel != null)
            _staminaLabel.Text = $"STAMINA: {_gameManager.Stamina}/{_gameManager.MaxStamina}";
        
        if (_reasonLabel != null)
            _reasonLabel.Text = $"REASON: {_gameManager.Reason}/{_gameManager.MaxReason}";
        
        if (_doomLabel != null)
        {
            _doomLabel.Text = $"DOOM: {_gameManager.Doom}/100";
            
            // Change color based on doom level
            if (_gameManager.Doom >= 75)
                _doomLabel.Modulate = new Color(1, 0, 0); // Red
            else if (_gameManager.Doom >= 50)
                _doomLabel.Modulate = new Color(1, 0.5f, 0); // Orange
            else if (_gameManager.Doom >= 25)
                _doomLabel.Modulate = new Color(1, 1, 0); // Yellow
            else
                _doomLabel.Modulate = new Color(1, 1, 1); // White
        }
    }

    public void UpdateTurn(int turn)
    {
        _currentTurn = turn;
        if (_turnLabel != null)
            _turnLabel.Text = $"TURN: {turn}";
    }

    public void UpdateLocation(string locationName)
    {
        if (_locationLabel != null)
            _locationLabel.Text = $"Location: {locationName}";
    }
}
