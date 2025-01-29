using Godot;
using System;

public partial class drop : RigidBody2D
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		TimeoutRemove();
	}

	public async void TimeoutRemove()
	{
		await ToSignal(GetTree().CreateTimer(GD.RandRange(0.5f, 0.8f)), SceneTreeTimer.SignalName.Timeout);
		QueueFree();
	}
}
