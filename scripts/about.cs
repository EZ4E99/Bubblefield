using Godot;
using System;

public partial class about : Control
{
	private Button button;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		button = GetNode<Button>("Button");

		Callable OnButtonPressedCallable = new(this, MethodName.OnButtonPressed);
        button.Connect("pressed", OnButtonPressedCallable, 0); 
	}

	public void OnButtonPressed()
	{
		GetTree().ChangeSceneToFile("res://scenes/world.tscn");
	}
}
