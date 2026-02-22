extends GutTest

## Unit test for TitleScreen
## Tests that the title screen initializes correctly and menu buttons work

var title_screen: Control
var start_button: Button
var options_button: Button
var quit_button: Button

func before_each():
	# Load and instance the title screen
	var title_screen_scene = load("res://scenes/ui/title_screen.tscn")
	assert_not_null(title_screen_scene, "Title screen scene should load")

	title_screen = title_screen_scene.instantiate()
	add_child_autofree(title_screen)

	# Get references to buttons
	var vbox = title_screen.get_node_or_null("VBoxContainer")
	assert_not_null(vbox, "VBoxContainer should exist in before_each")

	start_button = vbox.get_node_or_null("StartButton")
	options_button = vbox.get_node_or_null("OptionsButton")
	quit_button = vbox.get_node_or_null("QuitButton")

func test_title_screen_loads():
	assert_not_null(title_screen, "Title screen should load")
	assert_true(title_screen is Control, "Title screen should be a Control node")

func test_start_button_exists():
	assert_not_null(start_button, "Start button should exist")
	assert_eq(start_button.text, "Start Game (Spooky)", "Start button should have correct text")

func test_options_button_exists():
	assert_not_null(options_button, "Options button should exist")
	assert_eq(options_button.text, "Options", "Options button should have correct text")

func test_quit_button_exists():
	assert_not_null(quit_button, "Quit button should exist")
	assert_eq(quit_button.text, "Quit", "Quit button should have correct text")

func test_all_buttons_are_pressable():
	# Verify all buttons are not disabled
	assert_false(start_button.disabled, "Start button should not be disabled")
	assert_false(options_button.disabled, "Options button should not be disabled")
	assert_false(quit_button.disabled, "Quit button should not be disabled")

func test_start_button_connected():
	# Check if button is connected to the title screen's method
	var is_connected = start_button.is_connected("pressed", Callable(title_screen, "_on_start_button_pressed"))
	assert_true(is_connected, "Start button should be connected to _on_start_button_pressed")

func test_options_button_connected():
	# Check if button is connected to the title screen's method
	var is_connected = options_button.is_connected("pressed", Callable(title_screen, "_on_options_button_pressed"))
	assert_true(is_connected, "Options button should be connected to _on_options_button_pressed")

func test_quit_button_connected():
	# Check if button is connected to the title screen's method
	var is_connected = quit_button.is_connected("pressed", Callable(title_screen, "_on_quit_button_pressed"))
	assert_true(is_connected, "Quit button should be connected to _on_quit_button_pressed")

func test_title_label_exists():
	var vbox = title_screen.get_node_or_null("VBoxContainer")
	assert_not_null(vbox, "VBoxContainer should exist")

	var title_label = vbox.get_node_or_null("Title")
	assert_not_null(title_label, "Title label should exist")
	assert_eq(title_label.text, "Roguelite Game", "Title should display 'Roguelite Game'")

func test_background_exists():
	var background = title_screen.get_node_or_null("ColorRect")
	assert_not_null(background, "Background ColorRect should exist")

func test_vbox_container_has_all_children():
	var vbox = title_screen.get_node_or_null("VBoxContainer")
	assert_not_null(vbox, "VBoxContainer should exist")

	# VBox should have: Title, Spacer, StartButton, OptionsButton, QuitButton = 5 children
	assert_eq(vbox.get_child_count(), 5, "VBoxContainer should have 5 children")

func test_button_hierarchy():
	# Test that all buttons are in the scene tree
	assert_true(start_button.is_inside_tree(), "Start button should be in scene tree")
	assert_true(options_button.is_inside_tree(), "Options button should be in scene tree")
	assert_true(quit_button.is_inside_tree(), "Quit button should be in scene tree")
