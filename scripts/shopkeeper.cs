using Godot;
using System;

public partial class shopkeeper : StaticBody2D
{
	private int level; //小贩的等级
	private AnimatedSprite2D animatedSprite2D; //精灵图
	private GpuParticles2D gpuParticles2D; //粒子效果
	private Area2D area2D; //加泡泡水区域
	private AudioStreamPlayer audioStreamPlayer; //音效
	private Timer timer; //倒计时结束后加泡泡水
	private bool isAddingBubbleWater; //是否正在添加泡泡水
	public override void _Ready()
	{
		animatedSprite2D = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		gpuParticles2D = GetNode<GpuParticles2D>("GPUParticles2D");
		area2D = GetNode<Area2D>("Area2D");
		audioStreamPlayer = GetNode<AudioStreamPlayer>("CashierSound");
		timer = GetNode<Timer>("Timer");

		Callable OnTimerTimeoutCallable = new(this, MethodName.OnTimerTimeout);
		timer.Connect("timeout", OnTimerTimeoutCallable, 0);

		//连接碰撞信号
		Callable OnBodyEnteredCallable = new(this, MethodName.OnBodyEntered);
		area2D.Connect("body_entered", OnBodyEnteredCallable, 0);
		Callable OnBodyExitedCallable = new(this, MethodName.OnBodyExited);
		area2D.Connect("body_exited", OnBodyExitedCallable, 0);

		//设置ZIndex，表现前后遮挡的效果
		ZIndex = (int)(Position.Y / 24);
	}

	public void OnTimerTimeout()
	{
		if (isAddingBubbleWater)
		{
			player player = (player)GetTree().GetFirstNodeInGroup("player");

			if (player.bubbleWater < player.maxBubbleWater) player.AddBubbleWater();
			else return;

			gpuParticles2D.Emitting = true;
			audioStreamPlayer.Play();

			level += 5;
			if (level >= 0 && level < 25) animatedSprite2D.Play("level1");
			else if (level >= 25 && level < 50) animatedSprite2D.Play("level2");
			else if (level >= 50 && level < 75) animatedSprite2D.Play("level3");
			else if (level >= 75 && level < 100) animatedSprite2D.Play("level4");
			else return;
		}
	}

	public void OnBodyEntered(Node2D body)
	{
		if (body is player)
		{
			isAddingBubbleWater = true;
			timer.Start();
		}
		else return;
	}

	public void OnBodyExited(Node2D body)
	{
		if (body is player)
		{
			isAddingBubbleWater = false;
			timer.Stop();
			timer.WaitTime = 1;
		}
		else return;
	}
}
