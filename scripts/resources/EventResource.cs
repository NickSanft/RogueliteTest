using Godot;
using Godot.Collections;

/// <summary>
/// Represents a single event in the game (investigation, encounter, etc.)
/// </summary>
[GlobalClass]
public partial class EventResource : Resource
{
    [Export] public string EventId { get; set; } = "";
    [Export(PropertyHint.MultilineText)] public string EventText { get; set; } = "";
    [Export] public string EventImagePath { get; set; } = "";
    [Export] public Array<EventOption> Options { get; set; } = new();
    [Export] public Array<EventConsequence> AutoConsequences { get; set; } = new();

    public EventOption? GetOption(int index)
    {
        if (index >= 0 && index < Options.Count)
            return Options[index];
        return null;
    }
}
