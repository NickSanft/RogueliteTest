class_name StatCheck
extends Resource

## Defines a stat check (e.g., "Stamina >= 5" or "Roll d20 vs Reason")

enum CheckType { FIXED_THRESHOLD, DICE_ROLL }
enum Stat { STAMINA, REASON, DOOM }

@export var check_type: CheckType = CheckType.FIXED_THRESHOLD
@export var stat: Stat = Stat.STAMINA
@export var threshold: int = 5  ## For FIXED_THRESHOLD
@export var dice_sides: int = 20  ## For DICE_ROLL (d20, d6, etc.)

func evaluate(player_stats: Dictionary) -> bool:
	var stat_value = _get_stat_value(player_stats)
	
	match check_type:
		CheckType.FIXED_THRESHOLD:
			return stat_value >= threshold
		CheckType.DICE_ROLL:
			var roll = randi_range(1, dice_sides)
			return roll + stat_value >= threshold
	
	return false

func _get_stat_value(player_stats: Dictionary) -> int:
	match stat:
		Stat.STAMINA:
			return player_stats.get("stamina", 0)
		Stat.REASON:
			return player_stats.get("reason", 0)
		Stat.DOOM:
			return player_stats.get("doom", 0)
	return 0
