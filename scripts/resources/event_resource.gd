class_name EventResource
extends Resource

## Represents a single event in the game (investigation, encounter, etc.)

@export var event_id: String = ""
@export_multiline var event_text: String = ""
@export var event_image_path: String = ""  ## Path to image in res://
@export var options: Array[EventOption] = []
@export var auto_consequences: Array[EventConsequence] = []  ## Applied immediately on trigger

func get_option(index: int) -> EventOption:
	if index >= 0 and index < options.size():
		return options[index]
	return null
