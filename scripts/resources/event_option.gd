class_name EventOption
extends Resource

## Represents a single choice within an event

@export var option_text: String = ""
@export var stat_check: Resource = null # Using Resource or 'StatCheck' if defined
@export var consequences: Array[Resource] = [] # Using Resource or 'EventConsequence'

## If stat_check exists, returns true if player passes the check
func evaluate_stat_check(player_stats: Dictionary) -> bool:
	if stat_check == null:
		return true # No check = auto-pass
	
	# Safety check to ensure the resource has the method
	if stat_check.has_method("evaluate"):
		return stat_check.evaluate(player_stats)
		
	return false
