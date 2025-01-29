using Godot;
using System;

enum Endings
{
	Angry,
	Wetness,
	Escaped,
	Bug,
}

public partial class player : CharacterBody2D
{
	public const float Speed = 200.0f; //角色移动速度
	public int wetness = 0; //潮湿度，当为100时游戏结束
	public int anger = 0; //愤怒值
	public int bubbleWater = 6; //剩余的泡泡水
	public int maxBubbleWater = 15; //泡泡水上限
	public int bugKilled = 0; //杀死的虫子数
	private AnimatedSprite2D characterSprite; //玩家的动画2D精灵图
	private TextureRect HUDBackground; //HUD背景
	private ColorRect wetnessBar; //HUD上显示的潮湿度
	private ColorRect angerBar; //HUD上显示的愤怒值
	private ColorRect bubbleWaterRemainBar; //HUD上显示的剩余泡泡水
	private TextureRect arrow; //指向鼠标位置的箭头
	private PackedScene bubble; //泡泡的场景
	private Timer fireRateTimer; //开火速率计时器，在这个倒计时结束之前不能进行下一次吹泡泡
	private bool canFire = true; //判断玩家此时是否可以开火
	private AudioStreamPlayer fireSound; //开火音效
	private GpuParticles2D gpuParticles2D; //潮湿度高时出现的水滴粒子效果
	private TextureRect arrowShopkeeper; //指向商店位置的箭头
	private Control gameOverScreen; //游戏结束屏幕
	private Label gameOverLabel; //游戏结束屏幕上的文字
	private AnimationPlayer animationPlayer; //游戏结束屏幕动画播放器
	private bool isInEnding = false; //判断是否进入结局
	private Endings ending; //进入的结局
	public bool isInIntro = true;
	public override void _Ready()
	{
		characterSprite = GetNode<AnimatedSprite2D>("CharacterSprite");
		HUDBackground = GetNode<TextureRect>("CanvasLayer/HUD/HUDBackground");
		wetnessBar = GetNode<ColorRect>("CanvasLayer/HUD/HUDBackground/WetBar");
		angerBar = GetNode<ColorRect>("CanvasLayer/HUD/HUDBackground/AngerBar");
		bubbleWaterRemainBar = GetNode<ColorRect>("CanvasLayer/HUD/HUDBackground/BubbleBar");
		arrow = GetNode<TextureRect>("Arrow");
		arrowShopkeeper = GetNode<TextureRect>("ArrowShopkeeper");
		fireRateTimer = GetNode<Timer>("FireRateTimer");
		fireSound = GetNode<AudioStreamPlayer>("FireSound");
		gpuParticles2D = GetNode<GpuParticles2D>("GPUParticles2D");
		gameOverScreen = GetNode<Control>("CanvasLayer/HUD/GameoverScreen");
		gameOverLabel = GetNode<Label>("CanvasLayer/HUD/GameoverScreen/GameOverLabel");
		animationPlayer = GetNode<AnimationPlayer>("CanvasLayer/HUD/GameoverScreen/AnimationPlayer");

		bubble = GD.Load<PackedScene>("res://scenes/bubble_player.tscn");

		//连接开火倒计时结束时的信号
		Callable OnFireRateTimerTimeoutCallable = new(this, MethodName.OnFireRateTimerTimeout);
		fireRateTimer.Connect("timeout", OnFireRateTimerTimeoutCallable, 0);

		Callable OnGameOverAnimationFinishedCallable = new(this, MethodName.OnGameOverAnimationFinished);
		animationPlayer.Connect("animation_finished", OnGameOverAnimationFinishedCallable, 0);

		HUDBackground.Hide();
		arrow.Hide();
	}

	public override void _PhysicsProcess(double delta)
	{
		if (isInIntro || isInEnding) return;
		
		Vector2 velocity = Velocity;

		// 移动
		Vector2 direction = Input.GetVector("move_left", "move_right", "move_up", "move_down");
		if (direction != Vector2.Zero)
		{
			velocity.X = direction.X * Speed;
			velocity.Y = direction.Y * Speed;
		}
		else
		{
			velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
			velocity.Y = Mathf.MoveToward(Velocity.Y, 0, Speed);
		}

		Velocity = velocity;
		MoveAndSlide();

		// 检测碰撞泡泡和水滴
		if (GetSlideCollisionCount() != 0)
		{
			KinematicCollision2D collision = GetLastSlideCollision();
			Node2D collider = (Node2D)collision.GetCollider();
			if (collider is bubble)
			{
				collider.Call("CollideExpolde");
				AddWetness(5);
			}
			else if (collider is drop)
			{
				collider.QueueFree();
				AddWetness(1);
			}
		}

		// 更新HUD
		wetnessBar.Scale = new(1, wetness / 1000f);
		angerBar.Scale = new(1, anger / 100f);
		bubbleWaterRemainBar.Scale = new(1, bubbleWater / (float)maxBubbleWater);

		if (bubbleWater <= 1)
		{
			shopkeeper shopkeeper = (shopkeeper)GetTree().GetFirstNodeInGroup("shopkeeper");
			Vector2 shopkeeperPosition = shopkeeper.Position - Position;
			float shopkeeperAngle = (float)Math.Atan2(shopkeeperPosition.Y, shopkeeperPosition.X);
			arrowShopkeeper.Rotation = shopkeeperAngle;
		}

		//设置ZIndex，表现前后遮挡的效果
		ZIndex = (int)(Position.Y / 24);
	}

    public override void _Process(double delta)
    {
		// 按下鼠标左键发射泡泡
        if (Input.IsActionJustPressed("action_fire") && !isInIntro && !isInEnding) Fire();
    }

    public override void _Input(InputEvent inputEvent)
    {
        // 读取鼠标位置，转动箭头和射线
        if (inputEvent is InputEventMouseMotion)
        {
            Vector2 mousePosition = GetLocalMousePosition();
			float mouseAngle = (float)Math.Atan2(mousePosition.Y, mousePosition.X);
			arrow.Rotation = mouseAngle;

			//根据鼠标位置反转角色精灵图，在开场时不反转
			if (mousePosition.X > 0) characterSprite.FlipH = true;
			else characterSprite.FlipH = false;
        }

		if (Input.GetVector("move_left", "move_right", "move_up", "move_down") != Vector2.Zero && !isInIntro) characterSprite.Play("run");
		else characterSprite.Play("idle");
    }

	public async void Fire()
	{
		//发射泡泡
		if (!canFire || isInEnding) return;
		if (bubbleWater <= 0) return;
		if (bubbleWater == 1) {arrow.Hide(); arrowShopkeeper.Show();}

		bubbleWater--;
		canFire = false;
		arrow.SelfModulate = new Color(1, 0, 0, 1);
		fireSound.PitchScale = (float)GD.RandRange(0.75f, 1.25f);
		fireSound.Play();

		for (int i = 1; i <= GD.RandRange(10,15); i++)
		{
			//实例化泡泡
			bubble bubbleInstance = (bubble)bubble.Instantiate();
			//将玩家设置为该泡泡的吹出者
			bubbleInstance.SetBubbleParent(this);
			//将玩家此时的位置设置为泡泡的初始位置
			bubbleInstance.GlobalPosition = GlobalPosition;
			bubbleInstance.SetInitialPosition(GlobalPosition);
			//随机力度
			float force = GD.RandRange(10, 15);
			//随机方向
			Vector2 firePosition = new Vector2(GetLocalMousePosition().X * 2 + GD.RandRange(-250, 250), GetLocalMousePosition().Y * 2 + GD.RandRange(-250, 250)).Normalized();
			if (isInIntro) firePosition = new Vector2(GetLocalMousePosition().X * 2 + GD.RandRange(250, 500), GetLocalMousePosition().Y * 2 + GD.RandRange(250, 500)).Normalized();
			//应用中心力
			bubbleInstance.ApplyCentralForce(firePosition * force); 
			//添加至场景
			if (isInIntro) AddChild(bubbleInstance); 
			else GetTree().GetFirstNodeInGroup("world").AddChild(bubbleInstance); 
			//等待一小段时间后再循环发射下一个
			await ToSignal(GetTree().CreateTimer(GD.RandRange(0.1f, 0.15f)), SceneTreeTimer.SignalName.Timeout); 
		}

		fireRateTimer.Start();
	}

	public void OnFireRateTimerTimeout()
	{
		canFire = true;
		if (bubbleWater > 0) arrow.SelfModulate = new Color(0, 0.6f, 1);
	}

	public void AddWetness(int wet)
	{
		if (isInEnding || isInIntro) return;

		//添加潮湿度的方法
		wetness += wet;
		gpuParticles2D.AmountRatio = wetness / 1000f;

		if (wetness > 1000 && !isInEnding)
		{
			isInEnding = true;
			ending = Endings.Wetness;
			gameOverLabel.Text = "湿透了！";
			gameOverScreen.Show();
			animationPlayer.Play("intro");
		}
	}

	public void AddAnger(int an)
	{
		anger += an;

		if (anger > 100 && !isInEnding)
		{
			isInEnding = true;
			ending = Endings.Angry;
			gameOverLabel.Text = "被群起而攻之……";
			gameOverScreen.Show();
			animationPlayer.Play("intro");
		}
	}

	public void AddBubbleWater()
	{
		if (bubbleWater < maxBubbleWater)
		{
			if (bubbleWater + 5 > maxBubbleWater) bubbleWater += maxBubbleWater - bubbleWater;
			else bubbleWater += 5;

			arrow.Show();
			arrow.SelfModulate = new Color(0, 0.6f, 1);
			arrowShopkeeper.Hide();
		}
		else return;
	}

	public void AddBugKilled()
	{
		bugKilled ++;

		if (bugKilled > 45 && !isInEnding)
		{
			isInEnding = true;
			ending = Endings.Bug;
			gameOverLabel.Text = "灭虫专家！";
			gameOverScreen.Show();
			animationPlayer.Play("intro");
		}
	}

	public void Escaped()
	{
		if (!isInEnding)
		{
			isInEnding = true;
			ending = Endings.Escaped;
			gameOverLabel.Text = "回家了！";
			gameOverScreen.Show();
			animationPlayer.Play("intro");
		}
	}

	public void OnGameOverAnimationFinished(string name)
	{
		if (!isInEnding || name != "intro") return;
		
		switch (ending)
		{
			case Endings.Angry:
				GetTree().ChangeSceneToFile("res://scenes/endings/angry_ending.tscn");
				break;
			case Endings.Wetness:
				GetTree().ChangeSceneToFile("res://scenes/endings/wet_ending.tscn");
				break;
			case Endings.Escaped:
				GetTree().ChangeSceneToFile("res://scenes/endings/escaped_ending.tscn");
				break;
			case Endings.Bug:
				GetTree().ChangeSceneToFile("res://scenes/endings/bug_ending.tscn");
				break;
			default:
				break;
		}
	}

	public void QuitIntro()
	{
		isInIntro = false;
		HUDBackground.Show();
		arrow.Show();
	}
}
