using Godot;
using System;

public partial class environment_meshinstance2d : MeshInstance2D
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		//设置ZIndex，表现前后遮挡的效果
		ZIndex = (int)(Position.Y / 24);
	}
}
