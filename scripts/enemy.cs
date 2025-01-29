using Godot;
using System;

public partial class enemy : CharacterBody2D
{
	[Export]
	public bool isEnemyArmed = true; //敌人是否装备了泡泡水（如果没有则是路人） 
	[Export]
	public bool isEnemyBug = false; //敌人是否是虫子 
	[Export]
	public float Speed = 50.0f;
	public int wetness = 0; //潮湿度
	private AnimatedSprite2D characterSprite; //玩家的动画2D精灵图
	private AudioStreamPlayer2D fireSound; //开火音效
	private PackedScene bubble; //泡泡的场景
	private Timer moveTimer; //移动计时器，在这个倒计时结束之后随机移动
	private Timer fireRateTimer; //开火速率计时器，在这个倒计时结束之前不能进行下一次吹泡泡
	private GpuParticles2D gpuParticles2D; //潮湿度高时出现的水滴粒子效果
    private bool canFire; //判断是否可以开火
	private bool isMoving; //判断敌人是否正在移动
	public Vector2 moveDirection; //移动方向
	public Sprite2D angerIcon; //愤怒的气泡
	public bool isAnger; //判断敌人是否愤怒
	public bool isInIntro;

    public override void _Ready()
    {
		characterSprite = GetNode<AnimatedSprite2D>("CharacterSprite");
        fireSound = GetNode<AudioStreamPlayer2D>("FireSound");
		moveTimer = GetNode<Timer>("MoveTimer");
		fireRateTimer = GetNode<Timer>("FireRateTimer");
		gpuParticles2D = GetNode<GpuParticles2D>("GPUParticles2D");
		if (HasNode("AngerIcon")) angerIcon = GetNode<Sprite2D>("AngerIcon");

		bubble = GD.Load<PackedScene>("res://scenes/bubble.tscn");

		//连接移动倒计时结束时的信号
		moveTimer.WaitTime = GD.RandRange(0.75f, 1.25f);
		moveTimer.Start();
		Callable OnMoveTimerTimeoutCallable = new(this, MethodName.OnMoveTimerTimeout);
		moveTimer.Connect("timeout", OnMoveTimerTimeoutCallable, 0);

		//连接开火倒计时结束时的信号
		if (isEnemyArmed)
		{
			fireRateTimer.WaitTime = GD.RandRange(3f, 6f);
			fireRateTimer.Start();
			Callable OnFireRateTimerTimeoutCallable = new(this, MethodName.OnFireRateTimerTimeout);
			fireRateTimer.Connect("timeout", OnFireRateTimerTimeoutCallable, 0);
		}
		else
		{
			fireRateTimer.Stop();
		}
    }

    public override void _PhysicsProcess(double delta)
	{
		Vector2 velocity;
		if (isMoving)
		{
			velocity.X = moveDirection.X * Speed;
			velocity.Y = moveDirection.Y * Speed;
			
			if (isAnger) velocity *= 3;
			
			if (!isEnemyBug) characterSprite.Play("run");
		}
		else
		{
			velocity = Vector2.Zero;
			characterSprite.Play("idle");
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
				AddWetness(5, collider);
			}
			else if (collider is drop)
			{
				collider.QueueFree();
				AddWetness(1, collider);
			}
		}

		//设置敌人的ZIndex，表现前后遮挡的效果
		ZIndex = (int)(Position.Y / 24);
	}

	public void OnMoveTimerTimeout()
	{
		if (isAnger)
		{
			isMoving = true;
 			return;
		}

		if (isMoving) isMoving = false;
		else isMoving = true;

		moveDirection.X = GD.RandRange(-50, 50);
		moveDirection.Y = GD.RandRange(-50, 50);
		moveDirection = moveDirection.Normalized();

		if (moveDirection.X > 0) characterSprite.FlipH = true;
		else characterSprite.FlipH = false;
	}

	public async void Fire()
	{
		//发射泡泡
		if (!canFire || !isEnemyArmed || isAnger) return;

		canFire = false;
		fireSound.PitchScale = (float)GD.RandRange(0.75f, 1.25f);
		fireSound.Play();

		for (int i = 1; i <= GD.RandRange(3,7); i++)
		{
			//实例化泡泡
			bubble bubbleInstance = (bubble)bubble.Instantiate();
			//将敌人设置为该泡泡的吹出者
			bubbleInstance.SetBubbleParent(this);
			//将敌人此时的位置设置为泡泡的初始位置
			bubbleInstance.GlobalPosition = GlobalPosition;
			bubbleInstance.SetInitialPosition(GlobalPosition);
			//随机力度
			float force = GD.RandRange(10, 15);
			//随机方向
			Vector2 firePosition = new Vector2(GetLocalMousePosition().X + GD.RandRange(-300, 300), GetLocalMousePosition().Y + GD.RandRange(-300, 300)).Normalized();
			//应用中心力
			bubbleInstance.ApplyCentralForce(firePosition * force); 
			//添加至场景
			GetTree().GetFirstNodeInGroup("world").AddChild(bubbleInstance); 
			//等待一小段时间后再循环发射下一个
			await ToSignal(GetTree().CreateTimer(GD.RandRange(0.1f, 0.15f)), SceneTreeTimer.SignalName.Timeout); 
		}

		fireRateTimer.Start();
	}

	public void OnFireRateTimerTimeout()
	{
		canFire = true;
		Fire();
	}

	public void AddWetness(int wet, Node2D node2D)
	{
		bool isPlayerBubble = false;
        //添加潮湿度的方法
        if (node2D is bubble b)
        {
            if (b.isPlayerBubble)
			{
				wet *= 5;
				isPlayerBubble = true;
			}
        }

        wetness += wet;
		gpuParticles2D.AmountRatio = wetness / 100f;

		if (isEnemyBug && isPlayerBubble)
		{
			Hide();
			player player = (player)GetTree().GetFirstNodeInGroup("player");
			player.Call("AddBugKilled");
			QueueFree();
		}

		if (!isEnemyBug && wetness >= 200)
		{
			isAnger = true;
			angerIcon.Show();
			SetCollisionMaskValue(1, false);
			SetCollisionMaskValue(2, false);
			SetCollisionMaskValue(3, false);
			SetCollisionMaskValue(4, false);

			if (isPlayerBubble) {GetTree().GetFirstNodeInGroup("player").Call("AddAnger", 5);}
		}
	}

	public async void DisableEnemy()
	{
		Hide();
		await ToSignal(GetTree().CreateTimer(10, true, true), SceneTreeTimer.SignalName.Timeout); 
		QueueFree();
	}
}
