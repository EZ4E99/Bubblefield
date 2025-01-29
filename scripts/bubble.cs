using Godot;
using System;

public partial class bubble : RigidBody2D
{
	[Export]
	public bool isPlayerBubble; //是否为玩家的泡泡
	[Export]
	public bool isIntroBubble; //是否为开场的泡泡
	private float bubbleSize; //泡泡的大小
	private AnimatedSprite2D animatedSprite2D; // 泡泡的精灵图
	private CollisionShape2D collisionShape2D; //泡泡的碰撞形状
	private CharacterBody2D bubbleParent; //吹出这个泡泡的人
	private Vector2 initialPosition; //泡泡被吹出时的位置
	private PackedScene drop; //水滴的场景
	private PackedScene water; //水渍的场景
	private AudioStreamPlayer2D popSound; //泡泡爆炸音效
	private bool canHurtParent = false; //是否可以伤害到吹出的人
	private bool canHurtBubble = false; //是否可以伤害到其他泡泡
	private bool isPopping = false; //判断泡泡是否正在破裂
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		animatedSprite2D = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		collisionShape2D = GetNode<CollisionShape2D>("CollisionShape2D");
		popSound = GetNode<AudioStreamPlayer2D>("PopSound");

		drop = GD.Load<PackedScene>("res://scenes/drop.tscn");
		water = GD.Load<PackedScene>("res://scenes/water.tscn");

		//泡泡随机大小
		bubbleSize = GD.RandRange(6, 10);
		animatedSprite2D.Scale = new(bubbleSize / 6, bubbleSize / 6);
		CircleShape2D circleShape2D = new(){Radius = bubbleSize / 2};
		collisionShape2D.SetDeferred("Shape",circleShape2D);

		if (IsInstanceValid(bubbleParent)) AddCollisionExceptionWith(bubbleParent); //泡泡刚被吹出时不会与吹出的人碰撞
		SetCollisionMaskValue(3, false); //泡泡刚被吹出时不会与其他泡泡碰撞
		SetCollisionMaskValue(4, false); //泡泡刚被吹出时不会与水滴碰撞

		//连接碰撞信号
		Callable OnBodyEnteredCallable = new(this, MethodName.OnBodyEntered);
		Connect("body_entered", OnBodyEnteredCallable, 0);

		//随机破碎音高
		popSound.PitchScale = (float)GD.RandRange(1f, 1.75f);
		//连接泡泡爆炸音效信号
		Callable OnPopSoundFinishedCallable = new(this, MethodName.OnPopSoundFinished);
		popSound.Connect("finished", OnPopSoundFinishedCallable, 0);

		if (!isIntroBubble) TimeoutExplode(); //到时间就会自爆
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		//泡泡吹出距离初始位置一定距离之后，就会与吹出的人或者其他泡泡碰撞
		if (!canHurtBubble || !canHurtParent)
		{
			float distance = GlobalPosition.DistanceTo(initialPosition);
			if (distance > 40)
			{
				canHurtParent = true;
				if (IsInstanceValid(bubbleParent)) RemoveCollisionExceptionWith(bubbleParent);
			}
			if (distance > 50)
			{
				canHurtBubble = true;
				SetCollisionMaskValue(3, true);
				SetCollisionMaskValue(4, true);
			}
		}
	}

    public override void _PhysicsProcess(double delta)
    {
        //设置ZIndex，表现前后遮挡的效果
		ZIndex = (int)(Position.Y / 24);
    }

    public void OnBodyEntered(Node2D body)
    {
		if (body is drop && !isPopping)
		{
			body.QueueFree();
			CollideExpolde();
		}
    }

    public async void TimeoutExplode()
	{
		//时间到了自爆
		float time = (float)GD.RandRange(5f, 8f);
		await ToSignal(GetTree().CreateTimer(time), SceneTreeTimer.SignalName.Timeout);
		CollideExpolde();
	}

	public async void CollideExpolde()
	{	
		if (isPopping) return;
		//设置泡泡状态为正在破裂，防止重复执行爆炸方法
		isPopping = true;
		
		//暂时禁用碰撞，等待音频播放结束后删除节点
		SetCollisionMaskValue(4, false);

		//碰撞爆炸
		popSound.Play();
		animatedSprite2D.Play("pop");

		Sprite2D waterInstance = (Sprite2D)water.Instantiate();
		waterInstance.GlobalPosition = GlobalPosition;
		GetTree().GetFirstNodeInGroup("world").GetNode<Node2D>("WaterSprites").AddChild(waterInstance);
		
		if (isIntroBubble) return;

		for (int i = 1; i <= bubbleSize / 2; i++)
		{
			//实例化水滴
			drop dropInstance = (drop)drop.Instantiate();
			//将泡泡此时的位置设置为水滴的初始位置
			dropInstance.GlobalPosition = GlobalPosition;
			//随机力度
			float force = GD.RandRange(8, 12);
			//随机方向
			Vector2 dropPosition = new Vector2(GD.RandRange(-75, 75), GD.RandRange(-75, 75)).Normalized();
			//设置旋转角度
			dropInstance.Rotation = (float)Math.Atan2(dropPosition.Y, dropPosition.X);
			//应用中心力
			dropInstance.ApplyCentralForce(dropPosition * force); 
			//添加至场景
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			GetTree().GetFirstNodeInGroup("world").AddChild(dropInstance);
		}
	}

	public void OnPopSoundFinished()
	{
		QueueFree();
	}

	public void SetBubbleParent(CharacterBody2D body2D)
	{
		bubbleParent = body2D;
	}

	public void SetInitialPosition(Vector2 position)
	{
		initialPosition = position;
	}
}
