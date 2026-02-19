extends Control

func _ready():
	pass


func _on_start_button_pressed():
	# Load the main game scene
	get_tree().change_scene_to_file("res://Main.tscn")


func _on_options_button_pressed():
	# TODO: Implement options menu
	print("Options button pressed")


func _on_quit_button_pressed():
	get_tree().quit()
