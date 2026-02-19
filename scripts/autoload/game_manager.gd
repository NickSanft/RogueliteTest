extends Node

## Global state manager (Autoload Singleton)

## Player Stats
var stamina: int = 10
var reason: int = 10
var max_stamina: int = 10
var max_reason: int = 10

## Global Doom (loss condition at 100)
var doom: int = 0

## Active Mysteries
var active_mysteries: Array[String] = []  ## Array of mystery IDs
var mystery_progress: Dictionary = {}  ## { mystery_id: progress_int }

## Inventory
var inventory: Array[String] = []

## Event Queue (for chained events)
var event_queue: Array[String] = []

## Signals
signal stat_changed(stat_name: String, old_value: int, new_value: int)
signal doom_changed(old_doom: int, new_doom: int)
signal game_over(reason: String)

## Modify a stat (stamina, reason, doom)
func modify_stat(stat_name: String, amount: int) -> void:
	var old_value = get(stat_name)
	var new_value = clamp(old_value + amount, 0, get("max_" + stat_name) if has("max_" + stat_name) else 999)
	
	set(stat_name, new_value)
	stat_changed.emit(stat_name, old_value, new_value)
	
	## Check game over conditions
	if stat_name == "doom" and new_value >= 100:
		game_over.emit("The Old Ones have awakened.")
	elif stat_name == "stamina" and new_value <= 0:
		game_over.emit("You collapsed from exhaustion.")
	elif stat_name == "reason" and new_value <= 0:
		game_over.emit("Your mind shattered.")

## Add item to inventory
func add_item(item_id: String) -> void:
	inventory.append(item_id)

## Queue event for later trigger
func queue_event(event_id: String) -> void:
	event_queue.append(event_id)

## Advance mystery progress
func advance_mystery(amount: int) -> void:
	if active_mysteries.size() == 0:
		return
	
	var current_mystery = active_mysteries[0]
	mystery_progress[current_mystery] = mystery_progress.get(current_mystery, 0) + amount

## Get player stats as dictionary (for stat checks)
func get_player_stats() -> Dictionary:
	return {
		"stamina": stamina,
		"reason": reason,
		"doom": doom
	}

## Reset game state (for new run)
func reset_game() -> void:
	stamina = max_stamina
	reason = max_reason
	doom = 0
	active_mysteries.clear()
	mystery_progress.clear()
	inventory.clear()
	event_queue.clear()
