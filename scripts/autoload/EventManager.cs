using Godot;
using System.Collections.Generic;

/// <summary>
/// Manages loading and caching of event resources
/// Add this to Project Settings > Autoload as "EventManager"
/// </summary>
public partial class EventManager : Node
{
	private Dictionary<string, EventResource> _eventCache = new();
	
	/// <summary>
	/// Load an event by its file path
	/// </summary>
	public EventResource? LoadEvent(string eventPath)
	{
		// Check cache first
		if (_eventCache.ContainsKey(eventPath))
			return _eventCache[eventPath];
		
		// Load from disk
		var eventResource = GD.Load<EventResource>(eventPath);
		
		if (eventResource != null)
		{
			_eventCache[eventPath] = eventResource;
			GD.Print($"Loaded event: {eventPath} (ID: {eventResource.EventId})");
		}
		else
		{
			GD.PrintErr($"Failed to load event: {eventPath}");
		}
		
		return eventResource;
	}
	
	/// <summary>
	/// Load an event by its ID (searches in data/events/)
	/// </summary>
	public EventResource? LoadEventById(string eventId)
	{
		string path = $"res://data/events/{eventId}.tres";
		return LoadEvent(path);
	}
	
	/// <summary>
	/// Preload multiple events (useful for loading screens)
	/// </summary>
	public void PreloadEvents(string[] eventPaths)
	{
		foreach (var path in eventPaths)
		{
			LoadEvent(path);
		}
	}
	
	/// <summary>
	/// Clear the event cache
	/// </summary>
	public void ClearCache()
	{
		_eventCache.Clear();
	}
}
