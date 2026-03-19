using Godot;

public partial class Enemy : CharacterBody2D
{
	[Export] public int MaxHealth = 50;
	public int Health = 50;
	[Export] public int Damage = 10;
	[Export] public float Speed = 100f;
	

	private AnimatedSprite2D _anim;

	private bool _IsDead = false;
	private bool _CanAttack = true;
	private float _AttackCooldown = 1.0f;
	private float _AttackTimer = 0f;
	public float gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();

	public override void _Ready()
	{
		_IsDead = false;
		Health = MaxHealth;
		_anim = GetNode<AnimatedSprite2D>("Zombie");
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_IsDead) return;
		if (!_CanAttack)
		{
			_AttackTimer -= (float)delta;
			if (_AttackTimer <= 0f)
			{
				_CanAttack = true;
			}
		}
		Vector2 velocity = Velocity;
		var player = GetTree().GetNodesInGroup("player");
		if (player.Count > 0)
		{
			var p = player[0] as Player;
			if (p == null)
			{
				GD.Print("[Enemy] Le cast vers Player a échoué !");
			}
			else
			{
				GD.Print($"[Enemy] Cast Player OK. IsDashing={p.IsDashing}, Distance={GlobalPosition.DistanceTo(p.GlobalPosition)}");
				// Appliquer la gravité comme pour le joueur
				if (!IsOnFloor())
				{
					float currentGravity = gravity;
					if (IsOnWallOnly() && velocity.Y > 0) currentGravity *= 0.3f;
					velocity.Y += currentGravity * (float)delta;
				}
				else
				{
					velocity.Y = 0;
				}
				Vector2 direction = (p.GlobalPosition - GlobalPosition).Normalized();
				// Flip l'ennemi selon la direction X
				if (direction.X != 0)
				{
					_anim.FlipH = direction.X > 0;
				}
				velocity.X = direction.X * Speed;
				Velocity = velocity;
				MoveAndSlide();
				// Attaque si la distance 2D est < 50 (peu importe Y)
				float dist = GlobalPosition.DistanceTo(p.GlobalPosition);
				if (!p.IsDashing && dist < 50 && _CanAttack)
				{
					GD.Print("[Enemy] Condition d'attaque remplie, attaque !");
					_anim.Play("Attack");
					Attack(p);
					_CanAttack = false;
					_AttackTimer = _AttackCooldown;
				}
				else if (direction != Vector2.Zero && _CanAttack)
				{
					_anim.Play("Walk");
				}
			}
		}
	}

	public async void TakeDamage(int amount)
	{
		if (_IsDead) return;
		Health -= amount;
		// Effet de hit : joue l'animation "Hit"
		if (_anim != null)
		{
			_anim.Play("Hit");
		}
		if (Health <= 0)
		{
			Die();
		}
	}

	private async void Die()
	{
		_IsDead = true;
		if (_anim != null)
		{
			_anim.Play("Dead");
			GD.Print("Enemy is dead");
			await ToSignal(_anim, "animation_finished");
		}
		QueueFree();
	}

	public void Attack(Player player)
	{
		GD.Print($"Enemy attaque le joueur ! (dégâts: {Damage})");
		player.TakeDamage(Damage);
	}
}
