using Godot;
using System;

public partial class map_edge : Area2D
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		//连接碰撞信号
		Callable OnBodyEnteredCallable = new(this, MethodName.OnBodyEntered);
		Connect("body_entered", OnBodyEnteredCallable, 0);
	}

	public void OnBodyEntered(Node2D body)
	{
		if (body is player) body.Call("Escaped");
		else if (body is enemy) body.Call("DisableEnemy");
		else return;
	}
}
