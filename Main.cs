using Godot;

public partial class Main : Node2D
{
	public override void _Ready()
	{
		var gm = GetNode<GameManager>("/root/GameManager");
		GD.Print($"=== Game Started ===");
		GD.Print($"Initial Stamina: {gm.Stamina}");
		GD.Print($"Initial Doom: {gm.Doom}");
		
		gm.ModifyStat("stamina", -3);
		GD.Print($"Stamina after -3: {gm.Stamina}");
		
		gm.ModifyStat("doom", 10);
		GD.Print($"Doom after +10: {gm.Doom}");
	}
}
