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

	private AnimatedSprite2D _anim;
	private AnimatedSprite2D _bladeEffect;
	private AnimatedSprite2D _runEffect;
	public float gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();

	private bool _isDashing = false;
	private bool _isAttacking = false;
	private int _jumpCount = 0;
	private float _dashCooldownTimer = 0f;
	private float _attackCooldownTimer = 0f;

	public override void _Ready()
	{
		_anim = GetNode<AnimatedSprite2D>("Main");
		_bladeEffect = GetNode<AnimatedSprite2D>("BladeEffect");
		_runEffect = GetNode<AnimatedSprite2D>("MovementEffect");
		
		_bladeEffect.Visible = false;
		_runEffect.Visible = false;
		
		_anim.AnimationFinished += OnAnimationFinished;
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector2 velocity = Velocity;

		// 1. Gestion des Timers
		if (_dashCooldownTimer > 0) _dashCooldownTimer -= (float)delta;
		if (_attackCooldownTimer > 0) _attackCooldownTimer -= (float)delta;

		// 2. Récupération de la direction (Important pour l'anim Idle/Run)
		float direction = Input.GetAxis("move_left", "move_right");

		// 3. Gestion des Inputs (Dash & Attaque)
		if (!_isDashing && !_isAttacking)
		{
			if (Input.IsActionJustPressed("dash") && _dashCooldownTimer <= 0) 
			{
				StartDash();
			}
			else if (Input.IsActionJustPressed("attack") && _attackCooldownTimer <= 0)
			{
				if (Input.IsActionPressed("move_up")) StartAttackUp();
				else if (Input.IsActionPressed("move_down")) StartAttackDown();
				else StartAttack();
				_attackCooldownTimer = 0.8f;
			}
		}

		// 4. Physique : Dash vs Mouvement Normal (Gravité)
		if (_isDashing)
		{
			// On utilise la direction du regard (FlipH) pour le dash
			velocity.X = (_anim.FlipH ? -1 : 1) * DashSpeed;
			velocity.Y = 0; // Pas de gravité pendant le dash
		}
		else
		{
			// Appelle la gravité, le saut et le déplacement
			HandleNormalMovement(delta, ref velocity);
		}

		// 5. Mise à jour des Animations (Priorité au Dash)
		if (!_isAttacking)
		{
			UpdateAnimations(direction);
		}

		// 6. Application du mouvement final
		Velocity = velocity;
		MoveAndSlide();
	}

	private void HandleNormalMovement(double delta, ref Vector2 velocity)
	{
		// Gravité & Wall Slide
		if (!IsOnFloor())
		{
			float currentGravity = gravity;
			// Si on glisse contre un mur en tombant
			if (IsOnWallOnly() && velocity.Y > 0) currentGravity *= 0.3f; 
			velocity.Y += currentGravity * (float)delta;
		}
		else
		{
			_jumpCount = 0; // Reset des sauts au sol
		}

		// Saut & Double Saut
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

		// Déplacement latéral
		float direction = Input.GetAxis("move_left", "move_right");
		if (direction != 0)
		{
			velocity.X = direction * Speed;
			if (!_isAttacking) _anim.FlipH = direction < 0;
		}
		else 
		{
			velocity.X = Mathf.MoveToward(velocity.X, 0, Speed);
		}
	}

	private void UpdateAnimations(float direction)
	{
		// PRIORITÉ 1 : DASH
		if (_isDashing)
		{
			if (_anim.Animation != "dash") _anim.Play("dash");
			_runEffect.Visible = true;
			if (_runEffect.Animation != "DashAnimation") _runEffect.Play("DashAnimation");
			_runEffect.FlipH = _anim.FlipH;
			_runEffect.Position = new Vector2(_anim.FlipH ? 80 : -80, 0);
			return;
		}

		// PRIORITÉ 2 : MUR
		if (IsOnWallOnly() && !IsOnFloor())
		{
			if (_anim.Animation != "wall_climb") _anim.Play("wall_climb");
			_runEffect.Visible = false;
		}
		// PRIORITÉ 3 : AIR (Saut/Chute)
		else if (!IsOnFloor())
		{
			if (_anim.Animation != "jump") _anim.Play("jump");
			_runEffect.Visible = false;
		}
		// PRIORITÉ 4 : COURSE
		else if (Mathf.Abs(direction) > 0.1f)
		{
			if (_anim.Animation != "run") _anim.Play("run");
			_runEffect.Visible = true;
			if (_runEffect.Animation != "RunAnimation") _runEffect.Play("RunAnimation");
			_runEffect.FlipH = _anim.FlipH;
			_runEffect.Position = new Vector2(_anim.FlipH ? 70 : -70, 50);
		}
		// PRIORITÉ 5 : IDLE
		else
		{
			if (_anim.Animation != "idle") _anim.Play("idle");
			_runEffect.Visible = false;
			_runEffect.Stop();
		}
	}

	private void StartDash()
	{
		_isDashing = true;
		_dashCooldownTimer = DashCooldown;
		// Le dash dure 0.2 secondes
		GetTree().CreateTimer(0.2f).Timeout += () => _isDashing = false;
	}

	// --- SYSTÈME D'ATTAQUE ---
	private void StartAttack()
	{
		_isAttacking = true;
		_anim.Play("attack");
		_bladeEffect.Visible = true;
		_bladeEffect.FlipH = _anim.FlipH;
		_bladeEffect.Position = new Vector2(_anim.FlipH ? -120 : 120, 0);
		_bladeEffect.Play("attack_effect");
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

	private void OnAnimationFinished()
	{
		string currentAnim = _anim.Animation;
		if (currentAnim == "attack" || currentAnim == "attack_effect_up" || currentAnim == "attack_effect_down")
		{
			_isAttacking = false;
			_bladeEffect.Visible = false;
		}
	}
}
