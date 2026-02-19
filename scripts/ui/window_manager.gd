extends Control

## Manages overlapping UI windows with focus stack (prevents Z-index hell)

var window_stack: Array[Control] = []
var base_z_index: int = 0

func _ready() -> void:
	# Register all child windows
	for child in get_children():
		if child is Panel or child is Window:
			child.visible = false
			child.z_index = base_z_index

func open_window(window_name: String) -> void:
	var window = get_node_or_null(window_name)
	if window == null or window in window_stack:
		return
	
	# Add to stack and bring to front
	window_stack.append(window)
	_update_window_z_indices()
	window.visible = true

func close_window(window_name: String) -> void:
	var window = get_node_or_null(window_name)
	if window == null or window not in window_stack:
		return
	
	window_stack.erase(window)
	_update_window_z_indices()
	window.visible = false

func close_top_window() -> void:
	if window_stack.size() == 0:
		return
	
	var top_window = window_stack.pop_back()
	top_window.visible = false
	_update_window_z_indices()

func _update_window_z_indices() -> void:
	for i in range(window_stack.size()):
		window_stack[i].z_index = base_z_index + i

func _input(event: InputEvent) -> void:
	# Close top window on Escape
	if event.is_action_pressed("ui_cancel"):
		close_top_window()
		get_viewport().set_input_as_handled()
