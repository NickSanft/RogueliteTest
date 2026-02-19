class_name EventConsequence
extends Resource

## Defines an outcome (stat change, doom increase, item gain, etc.)

enum ConsequenceType { STAT_CHANGE, ITEM_GAIN, TRIGGER_EVENT, ADVANCE_MYSTERY }

@export var consequence_type: ConsequenceType = ConsequenceType.STAT_CHANGE
@export var stat_name: String = "stamina"  ## For STAT_CHANGE
@export var value: int = 0  ## Positive or negative
@export var item_id: String = ""  ## For ITEM_GAIN
@export var next_event_id: String = ""  ## For TRIGGER_EVENT
@export var mystery_progress: int = 1  ## For ADVANCE_MYSTERY

## Apply this consequence to the game state
func apply(game_manager) -> void:
	match consequence_type:
		ConsequenceType.STAT_CHANGE:
			game_manager.modify_stat(stat_name, value)
		ConsequenceType.ITEM_GAIN:
			game_manager.add_item(item_id)
		ConsequenceType.TRIGGER_EVENT:
			game_manager.queue_event(next_event_id)
		ConsequenceType.ADVANCE_MYSTERY:
			game_manager.advance_mystery(mystery_progress)
