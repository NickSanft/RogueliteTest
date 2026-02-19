using Godot;
using System.Collections.Generic;

/// <summary>
/// Manages overlapping UI windows with focus stack (prevents Z-index hell)
/// </summary>
public partial class WindowManager : Control
{
	private List<Control> _windowStack = new();
	private int _baseZIndex = 0;

	public override void _Ready()
	{
		// Register all child windows
		foreach (Node child in GetChildren())
		{
			if (child is Panel or Window)
			{
				var window = (Control)child;
				window.Visible = false;
				window.ZIndex = _baseZIndex;
			}
		}
	}

	public void OpenWindow(string windowName)
	{
		var window = GetNodeOrNull<Control>(windowName);
		if (window == null || _windowStack.Contains(window))
			return;

		// Add to stack and bring to front
		_windowStack.Add(window);
		UpdateWindowZIndices();
		window.Visible = true;
	}

	public void CloseWindow(string windowName)
	{
		var window = GetNodeOrNull<Control>(windowName);
		if (window == null || !_windowStack.Contains(window))
			return;

		_windowStack.Remove(window);
		UpdateWindowZIndices();
		window.Visible = false;
	}

	public void CloseTopWindow()
	{
		if (_windowStack.Count == 0)
			return;

		var topWindow = _windowStack[^1];
		_windowStack.RemoveAt(_windowStack.Count - 1);
		topWindow.Visible = false;
		UpdateWindowZIndices();
	}

	private void UpdateWindowZIndices()
	{
		for (int i = 0; i < _windowStack.Count; i++)
		{
			_windowStack[i].ZIndex = _baseZIndex + i;
		}
	}

	public override void _Input(InputEvent @event)
	{
		// Close top window on Escape
		if (@event.IsActionPressed("ui_cancel"))
		{
			CloseTopWindow();
			GetViewport().SetInputAsHandled();
		}
	}
}
