using Godot;
using System.Collections.Generic;

/// <summary>
/// Modal window stack. Prevents input pass-through between overlapping windows
/// and centralises ESC handling. Access via WindowManager.Instance.
/// </summary>
public partial class WindowManager : Node
{
	public static WindowManager? Instance { get; private set; }

	private readonly Stack<Control> _stack = new();

	/// <summary>True when at least one window is on the stack.</summary>
	public bool HasOpenWindow => _stack.Count > 0;

	public override void _Ready()
	{
		Instance = this;
	}

	public override void _ExitTree()
	{
		if (Instance == this)
			Instance = null;
	}

	/// <summary>
	/// Push a window onto the stack and make it visible.
	/// Silently ignores duplicate pushes of the same window.
	/// </summary>
	public void Push(Control window)
	{
		if (_stack.Count > 0 && _stack.Peek() == window)
			return;

		_stack.Push(window);
		window.Visible = true;
	}

	/// <summary>
	/// Hide and remove the topmost window from the stack.
	/// </summary>
	public void Pop()
	{
		if (!_stack.TryPop(out var top))
			return;

		top.Visible = false;
	}

	/// <summary>
	/// Hide and remove all windows from the stack.
	/// </summary>
	public void CloseAll()
	{
		while (_stack.TryPop(out var window))
			window.Visible = false;
	}

	public override void _Input(InputEvent @event)
	{
		if (!HasOpenWindow)
			return;

		// ESC always closes the topmost window; block the event from reaching anything else.
		if (@event.IsActionPressed("ui_cancel"))
		{
			Pop();
			GetViewport().SetInputAsHandled();
		}
	}
}
