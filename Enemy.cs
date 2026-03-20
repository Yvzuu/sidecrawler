using Godot;
using System;

public partial class Enemy : CharacterBody2D
{
	[Export] public int MaxHealth = 50;
	public int Health = 50;
	[Export] public int Damage = 10;
	[Export] public float Speed = 200f;

	private AnimatedSprite2D _anim;

	private bool _IsDead = false;
	private bool _CanAttack = true;
	private float _AttackCooldown = 1.0f;
	private float _AttackTimer = 0f;
	public float gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();
	
	private float _hitCooldown = 0.5f;
	private float _hitTimer = 0f;
	private bool _isStunned = false; // Pour bloquer l'IA pendant le hit

	public override void _Ready()
	{
		_IsDead = false;
		Health = MaxHealth;
		_anim = GetNode<AnimatedSprite2D>("Zombie");
		
		// S'assurer que l'animation ne tourne pas en boucle pour le Hit et l'Attack
		// Connecter le signal de fin d'animation
		_anim.AnimationFinished += OnAnimationFinished;
		
		// Animation de départ
		_anim.Play("Walk"); 
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_IsDead || _isStunned) return; // Si étourdi ou mort, on ne fait rien

		// Gestion du cooldown d'attaque
		if (!_CanAttack)
		{
			_AttackTimer -= (float)delta;
			if (_AttackTimer <= 0f) _CanAttack = true;
		}

		// Gestion du timer d'invincibilité
		if (_hitTimer > 0f) _hitTimer -= (float)delta;

		Vector2 velocity = Velocity;
		var players = GetTree().GetNodesInGroup("player");
		
		if (players.Count > 0)
		{
			var p = players[0] as Player;
			if (p != null)
			{
				// Gravité
				if (!IsOnFloor())
				{
					velocity.Y += gravity * (float)delta;
				}
				else
				{
					velocity.Y = 0;
				}

				float dist = GlobalPosition.DistanceTo(p.GlobalPosition);
				Vector2 direction = (p.GlobalPosition - GlobalPosition).Normalized();

				// Flip visuel
				if (direction.X != 0) _anim.FlipH = direction.X > 0;

				// LOGIQUE D'ATTAQUE
				if (!p.IsDashing && dist < 95 && _CanAttack)
				{
					velocity.X = 0; // S'arrête pour attaquer
					_anim.Play("Attack");
					Attack(p);
					_CanAttack = false;
					_AttackTimer = _AttackCooldown;
				}
				// LOGIQUE DE MOUVEMENT (Seulement si on n'est pas en train d'attaquer)
				else if (_anim.Animation != "Attack")
				{
					velocity.X = direction.X * Speed;
					_anim.Play("Walk");
				}

				Velocity = velocity;
				MoveAndSlide();
			}
		}
	}

	private void OnAnimationFinished()
	{
		// Quand "Hit" ou "Attack" se termine, on libère l'ennemi
		if (_anim.Animation == "Hit")
		{
			_isStunned = false;
			_anim.Play("Walk");
		}
		
		if (_anim.Animation == "Attack")
		{
			_anim.Play("Walk");
		}
	}

	public void TakeDamage(int amount)
	{
		if (_IsDead || _hitTimer > 0f) return;

		Health -= amount;
		_hitTimer = _hitCooldown;
		_isStunned = true; // L'ennemi s'arrête car il prend un coup

		if (Health <= 0)
		{
			Die();
		}
		else
		{
			_anim.Play("Hit");
			GD.Print("[Enemy] Prend des dégâts");
		}
	}

	private async void Die()
	{
		_IsDead = true;
		_anim.Play("Dead");
		// On attend la fin de l'anim de mort avant de supprimer
		await ToSignal(_anim, "animation_finished");
		QueueFree();
	}

	public void Attack(Player player)
	{
		player.TakeDamage(Damage);
	}
}
