using Godot;
using System;

public partial class Player : CharacterBody2D
{
	[Export] public float Speed = 800.0f;
	[Export] public float JumpVelocity = -600.0f;
	[Export] public float DashSpeed = 3000.0f;
	[Export] public float DashCooldown = 1.0f;
	[Export] public int MaxJumps = 2;
	[Export] public float WallJumpPushback = 100.0f;
	[Export] public float MaxHealth = 100f;
	[Export] public float MaxMana = 100f;
	[Export] public int AttackDamage = 20;

	private AnimatedSprite2D _anim;
	private AnimatedSprite2D _bladeEffect;
	private AnimatedSprite2D _runEffect;
	private ProgressBar _HealthBar;
	private ProgressBar _ManaBar;
	public float gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();

	private bool _isDashing = false;
	private bool _isParry = false;
	private bool _isAttacking = false;

	public bool _isHit = false;
	public bool IsDashing => _isDashing;
	private int _jumpCount = 0;
	private float _dashCooldownTimer = 0f;
	private float _attackCooldownTimer = 0f;

	public int Health = 50;

	public int Mana = 100;


	public override void _Ready()
	{
		_anim = GetNode<AnimatedSprite2D>("Main");
		_bladeEffect = GetNode<AnimatedSprite2D>("BladeEffect");
		_runEffect = GetNode<AnimatedSprite2D>("MovementEffect");
		_HealthBar = GetNode<ProgressBar>("/root/World/HUD/Control/HealthBar");
		_ManaBar = GetNode<ProgressBar>("/root/World/HUD/Control/ManaBar");

		_bladeEffect.Visible = false;
		_runEffect.Visible = false;

		_anim.AnimationFinished += OnAnimationFinished;
		UpdateBars();

	}

	public override void _PhysicsProcess(double delta)
	{
		Vector2 velocity = Velocity;

		if (_dashCooldownTimer > 0) _dashCooldownTimer -= (float)delta;
		if (_attackCooldownTimer > 0) _attackCooldownTimer -= (float)delta;

		float direction = Input.GetAxis("move_left", "move_right");

		// Inputs
		if (!_isDashing && !_isAttacking && !_isParry && !_isHit)
		{
			if (Input.IsActionJustPressed("dash") && _dashCooldownTimer <= 0) StartDash();
			// else if (Input.IsActionJustPressed("parry")) StartParry();
			else if (Input.IsActionJustPressed("attack") && _attackCooldownTimer <= 0)
			{
				if (Input.IsActionPressed("move_up")) StartAttackUp();
				else if (Input.IsActionPressed("move_down")) StartAttackDown();
				else StartAttack();
				_attackCooldownTimer = 0.8f;
			}
		}

		// Physique
		if (_isDashing)
		{
			velocity.X = (_anim.FlipH ? -1 : 1) * DashSpeed;
			velocity.Y = 0;
		}
		else if (_isParry)
		{
			velocity.X = 0; // On fige le perso pendant le parry
			velocity.Y = 0;
		}
		else
		{
			HandleNormalMovement(delta, ref velocity);
		}

		// Animations (On ne met pas à jour si on attaque ou parry, car ils gèrent leurs propres anims)
		if (!_isAttacking && !_isParry && !_isHit)
		{
			UpdateAnimations(direction);
		}

		Velocity = velocity;
		MoveAndSlide();
	}

	private void HandleNormalMovement(double delta, ref Vector2 velocity)
	{
		if (!IsOnFloor())
		{
			float currentGravity = gravity;
			if (IsOnWallOnly() && velocity.Y > 0) currentGravity *= 0.3f;
			velocity.Y += currentGravity * (float)delta;
		}
		else _jumpCount = 0;

		if (Input.IsActionJustPressed("jump"))
		{
			if (IsOnFloor() || _jumpCount < MaxJumps)
			{
				velocity.Y = JumpVelocity;
				_jumpCount++;
			}
			else if (IsOnWallOnly() && !IsOnFloor())
			{
				velocity.Y = JumpVelocity;
				velocity.X = GetWallNormal().X * WallJumpPushback;
				_anim.FlipH = GetWallNormal().X < 0;
			}
		}

		float direction = Input.GetAxis("move_left", "move_right");
		if (direction != 0)
		{
			velocity.X = direction * Speed;
			_anim.FlipH = direction < 0;
		}
		else velocity.X = Mathf.MoveToward(velocity.X, 0, Speed);
	}

	private void UpdateAnimations(float direction)
	{
		if (_isDashing)
		{
			if (_anim.Animation != "dash") _anim.Play("dash");
			_runEffect.Visible = true;
			if (_runEffect.Animation != "DashAnimation") _runEffect.Play("DashAnimation");
			_runEffect.FlipH = _anim.FlipH;
			_runEffect.Position = new Vector2(_anim.FlipH ? 80 : -80, 0);
			return;
		}

		// if (IsOnWallOnly() && !IsOnFloor()) _anim.Play("wall_climb");
		else if (!IsOnFloor()) _anim.Play("jump");
		else if (Mathf.Abs(direction) > 0.1f) // Deadzone pour Linux/Manette
		{
			_anim.Play("run");
			_runEffect.Visible = true;
			_runEffect.Play("RunAnimation");
			_runEffect.FlipH = _anim.FlipH;
			_runEffect.Position = new Vector2(_anim.FlipH ? 70 : -70, 50);
		}
		else if (_isHit)
		{
			_anim.Play("hit");
			_runEffect.Visible = false;
		}
		else
		{
			_anim.Play("idle");
			_runEffect.Visible = false;
		}
	}

	private void StartDash()
	{
		_isDashing = true;
		_dashCooldownTimer = DashCooldown;
		GetTree().CreateTimer(0.2f).Timeout += () => _isDashing = false;
	}

	// private void StartParry()
	// {
	// 	_isParry = true;
	// 	_anim.Play("parry");
	// 	_runEffect.Visible = true;
	// 	_runEffect.FlipH = _anim.FlipH;
	// 	_runEffect.Play("ParryAnimation");
	// 	_runEffect.Position = new Vector2(_anim.FlipH ? -80 : 80, 0);
	// }

	private void StartAttack()
	{
		_isAttacking = true;
		_anim.Play("attack");
		_bladeEffect.Visible = true;
		_bladeEffect.FlipH = _anim.FlipH;
		_bladeEffect.Position = new Vector2(_anim.FlipH ? -120 : 120, 0);
		_bladeEffect.Play("attack_effect");
		// Inflige des dégâts à tous les ennemis proches
		foreach (var enemy in GetTree().GetNodesInGroup("enemies"))
		{
			Enemy e = enemy as Enemy;
			if (e != null && e.GlobalPosition.DistanceTo(GlobalPosition) < 1000) // 50 = portée de l'attaque
			{
				e.TakeDamage(AttackDamage);
			}
		}
	}

	private void StartAttackUp()
	{
		_isAttacking = true;
		_anim.Play("attack_effect_up");
		_bladeEffect.Visible = true;
		_bladeEffect.Position = new Vector2(0, -120);
		_bladeEffect.Play("attack_effect_up");
	}

	private void StartAttackDown()
	{
		_isAttacking = true;
		_anim.Play("attack_effect_down");
		_bladeEffect.Visible = true;
		_bladeEffect.Position = new Vector2(0, 120);
		_bladeEffect.Play("attack_effect_down");
	}

	public void TakeDamage(int amount)
	{
		Health -= amount;
		if (Health <= 0)
		{
			Die();
		}
		else
		{
			_anim.Play("hit");
			_isHit = true;
			GD.Print($"[TakeDamage] je joue HIT -> anim courante={_anim.Animation}");
			GD.Print("[Main] Prend des dégâts");
			UpdateBars();


		}
	}

	private async void Die()
	{
		//ajouter reset du jeu ou écran ou je sais pas gros mais un truc la 

	}

	private void UpdateBars()
	{
		_HealthBar.MaxValue = MaxHealth;
		_HealthBar.Value = Health;
		_ManaBar.MaxValue = MaxMana;
		_ManaBar.Value = Mana;
	}

	private void OnAnimationFinished()
	{
		string currentAnim = _anim.Animation;
		if (currentAnim == "attack" || currentAnim == "attack_effect_up" || currentAnim == "attack_effect_down" || currentAnim == "parry" || currentAnim == "hit")
		{
			_isAttacking = false;
			_isParry = false; // LIBÈRE LE PERSONNAGE
			_bladeEffect.Visible = false;
			_isHit = false;
		}
	}
}
