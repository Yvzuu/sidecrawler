using Godot;

public partial class Enemy : CharacterBody2D
{
	[Export] public int MaxHealth = 50;
	public int Health = 50;
	[Export] public int Damage = 10;
	[Export] public float Speed = 100f;
	

	private AnimatedSprite2D _anim;
	

	public override void _Ready()
	{
		Health = MaxHealth;
		_anim = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
	}

	public override void _PhysicsProcess(double delta)
	{   
// 		var player = GetTree().GetNodesInGroup("player");
// 		if (player.Count > 0)
// 		{
// 			var p = player[0] as Player;
// 			if (p != null)
// 		{
// 		Vector2 direction = (p.GlobalPosition - GlobalPosition).Normalized();
// 		Velocity = direction * Speed;
// 		MoveAndSlide();
// 	}
// }
		var playerList = GetTree().GetNodesInGroup("player");
		if (playerList.Count > 0)
		{
			var p = playerList[0] as Player;
			if (p != null)
			{
				// Attaque le joueur si à portée
				if (!p.IsDashing &&GlobalPosition.DistanceTo(p.GlobalPosition) < 50)
				{
					Attack(p);
				}
			}
		}

	}

	public void TakeDamage(int amount)
	{
		Health -= amount;
		if (Health <= 0)
		{
			if (_anim != null)
				_anim.Visible = false; // Cache le sprite
			QueueFree(); // L’ennemi meurt (supprime le node)
		}
	}

	public void Attack(Player player)
	{
		player.TakeDamage(Damage);
	}
}
