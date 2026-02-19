class_name EventOption
extends Resource

## Represents a single choice within an event

@export var option_text: String = ""
@export var stat_check: StatCheck = null  ## Optional stat check (e.g., "Roll Stamina")
@export var consequences: Array[EventConsequence] = []  ## Applied on selection

## If stat_check exists, returns true if player passes the check
func evaluate_stat_check(player_stats: Dictionary) -> bool:
	if stat_check == null:
		return true  ## No check = auto-pass
	return stat_check.evaluate(player_stats)
