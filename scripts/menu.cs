using Godot;
using System;

public partial class menu : Control
{
	//主菜单按钮
	private Button startButton;
	private Button aboutButton;
	private Button quitButton;

	private AnimationPlayer animationPlayer;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		startButton = GetNode<Button>("StartButton");
		aboutButton = GetNode<Button>("AboutButton");
		quitButton = GetNode<Button>("QuitButton");
		animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");

		Callable OnStartButtonPressedCallable = new(this, MethodName.OnStartButtonPressed);
        startButton.Connect("pressed", OnStartButtonPressedCallable, 0); 
		Callable OnAboutButtonPressedCallable = new(this, MethodName.OnAboutButtonPressed);
        aboutButton.Connect("pressed", OnAboutButtonPressedCallable, 0); 
		Callable OnQuitButtonPressedCallable = new(this, MethodName.OnQuitButtonPressed);
        quitButton.Connect("pressed", OnQuitButtonPressedCallable, 0); 
		Callable OnAnimationPlayerFinishedCallable = new(this,MethodName.OnAnimationPlayerFinished);
		animationPlayer.Connect("animation_finished", OnAnimationPlayerFinishedCallable, 0);
	}

	public void OnStartButtonPressed()
	{
		animationPlayer.Play("start");
	}

	public void OnAboutButtonPressed()
	{
		animationPlayer.Play("about");
	}

	public void OnQuitButtonPressed()
	{
		animationPlayer.Play("quit");
	}

	public void OnAnimationPlayerFinished(string name)
	{
		switch (name)
		{
			case "start":
				animationPlayer.Play("outro");
				GetTree().Paused = false;
				Hide();
				break;
			case "about":
				GetTree().ChangeSceneToFile("res://scenes/about.tscn");
				break;
			case "quit":
				GetTree().Quit();
				break;
			default:
				animationPlayer.Play("idle");
				break;
		}
	}
}
