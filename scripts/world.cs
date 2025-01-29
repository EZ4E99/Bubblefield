using Godot;
using System;

public partial class world : Node2D
{
	private PackedScene enemy;
	private Timer enemySpawnTimer;
	private AudioStreamPlayer bgmPlayer;
	private bool isInIntro = true;
	private AnimationPlayer introPlayer;
	private bubble introBubble;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		enemySpawnTimer = GetNode<Timer>("EnemySpawnTimer");
		bgmPlayer = GetNode<AudioStreamPlayer>("BGMPlayer");

		introPlayer = GetNode<AnimationPlayer>("IntroAnimationPlayer");
		introBubble = GetNode<bubble>("IntroBubble");

		Callable OnEnemySpawnTimerTimeoutCallable = new(this, MethodName.OnEnemySpawnTimerTimeout);
		enemySpawnTimer.Connect("timeout", OnEnemySpawnTimerTimeoutCallable, 0);

		Callable OnBGMPlayerFinishedCallable = new(this, MethodName.OnBGMPlayerFinished);
		bgmPlayer.Connect("finished", OnBGMPlayerFinishedCallable, 0);

		Callable OnIntroAnimationPlayerFinishedCallable = new(this, MethodName.OnIntroAnimationPlayerFinished);
		introPlayer.Connect("animation_finished", OnIntroAnimationPlayerFinishedCallable);

		GetTree().Paused = true;
	}

    public void OnEnemySpawnTimerTimeout()
	{
		player player = (player)GetTree().GetFirstNodeInGroup("player");

		string sex;
		if (GD.Randf() > 0.5f) sex = "male";
		else sex = "female";

		int number = (int)(GD.Randi() % 3 + 1);

		string arm;
		if (GD.Randf() > 0.5f) arm = "armed";
		else arm = "unarmed";

		string enemyScenePath = "enemy_" + sex + number + "_" + arm;
		
		enemy = GD.Load<PackedScene>("res://scenes/characters/" + enemyScenePath + ".tscn");
		enemy enemyIns = enemy.Instantiate<enemy>();
		var enemySpawnLocation = GetNode<PathFollow2D>("EnemyPath/EnemySpawnLocation");
		enemySpawnLocation.ProgressRatio = GD.Randf();
		enemyIns.Position = enemySpawnLocation.Position;

		if (enemyIns.Position.DistanceTo(player.Position) >= 600)
		{
			GetNode<Node2D>("Enemies").AddChild(enemyIns);
		}
	}

	public void OnBGMPlayerFinished()
	{
		bgmPlayer.Play();
	}

	public void OnIntroAnimationPlayerFinished(string name)
	{
		if (name != "intro") return;
		introBubble.Position = new(60, 663);
		introBubble.CollideExpolde();
		introBubble.Hide();

		player player = (player)GetTree().GetFirstNodeInGroup("player");
		player.QuitIntro();
	}
}
