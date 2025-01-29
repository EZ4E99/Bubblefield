using Godot;
using System;

public partial class ending : TextureRect
{
	private AudioStreamPlayer BGMPlayer;
	private AnimationPlayer animationPlayer;
	private Button button;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		BGMPlayer = GetNode<AudioStreamPlayer>("BGMPlayer");
		animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
		button = GetNode<Button>("Button");

		Callable OnBGMPlayerFinishedCallable = new(this, MethodName.OnBGMPlayerFinished);
		BGMPlayer.Connect("finished", OnBGMPlayerFinishedCallable, 0);

		Callable OnAnimationPlayerFinishedCallable = new(this,MethodName.OnAnimationPlayerFinished);
		animationPlayer.Connect("animation_finished", OnAnimationPlayerFinishedCallable, 0);

		Callable OnButtonPressedCallable = new(this, MethodName.OnButtonPressed);
        button.Connect("pressed", OnButtonPressedCallable, 0); 
	}

	public void OnBGMPlayerFinished()
	{
		BGMPlayer.Play();
	}

	public void OnAnimationPlayerFinished(string name)
	{
		switch (name)
		{
			case "menu":
				GetTree().ChangeSceneToFile("res://scenes/world.tscn");
				break;
			default:
				break;
		}
	}

	public void OnButtonPressed()
	{
		animationPlayer.Play("menu");
	}
}
