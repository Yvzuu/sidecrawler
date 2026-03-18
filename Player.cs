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

    // Timers
    if (_dashCooldownTimer > 0) _dashCooldownTimer -= (float)delta;
    if (_attackCooldownTimer > 0) _attackCooldownTimer -= (float)delta;

    float direction = Input.GetAxis("move_left", "move_right");

    // On ne gère le dash/attaque que si on n'est pas déjà occupé
    if (!_isDashing && !_isAttacking)
    {
        if (Input.IsActionJustPressed("dash") && _dashCooldownTimer <= 0) StartDash();
        else if (Input.IsActionJustPressed("attack") && _attackCooldownTimer <= 0)
        {
            if (Input.IsActionPressed("move_up")) StartAttackUp();
            else if (Input.IsActionPressed("move_down")) StartAttackDown();
            else StartAttack();
            _attackCooldownTimer = 0.8f;
        }
    }

    // PHYSIQUE
    if (_isDashing)
    {
        velocity.X = (_anim.FlipH ? -1 : 1) * DashSpeed;
        velocity.Y = 0;
    }
}

    private void HandleNormalMovement(double delta, ref Vector2 velocity)
    {
        if (!IsOnFloor())
        {
            float currentGravity = gravity;
            if (IsOnWallOnly() && velocity.Y > 0) currentGravity *= 0.3f; 
            velocity.Y += currentGravity * (float)delta;
        }
        else
        {
            _jumpCount = 0;
        }

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
                // On ne joue pas l'anim ici, UpdateAnimations s'en charge
            }
        }

        float direction = Input.GetAxis("move_left", "move_right");
        if (direction != 0)
        {
            velocity.X = direction * Speed;
            if (!_isAttacking) _anim.FlipH = direction < 0;
        }
        else velocity.X = Mathf.MoveToward(velocity.X, 0, Speed);
    }

private void UpdateAnimations(float direction)
{
    // --- LOGIQUE DU DASH ---
    if (_isDashing)
    {
        if (_anim.Animation != "dash") _anim.Play("dash");
        
        _runEffect.Visible = true;
        if (_runEffect.Animation != "DashAnimation") _runEffect.Play("DashAnimation");
        _runEffect.FlipH = _anim.FlipH;
        _runEffect.Position = new Vector2(_anim.FlipH ? 80 : -80, 0);
        return; 
    }

    // --- LOGIQUE HORS DASH ---
    if (IsOnWallOnly() && !IsOnFloor())
    {
        if (_anim.Animation != "wall_climb") _anim.Play("wall_climb");
        _runEffect.Visible = false;
    }
    else if (!IsOnFloor()) 
    {
        if (_anim.Animation != "jump") _anim.Play("jump");
        _runEffect.Visible = false;
    }
    else if (direction != 0)
    {
        if (_anim.Animation != "run") _anim.Play("run");
        
        _runEffect.Visible = true;
        if (_runEffect.Animation != "RunAnimation") _runEffect.Play("RunAnimation");
        _runEffect.FlipH = _anim.FlipH;
        _runEffect.Position = new Vector2(_anim.FlipH ? 70 : -70, 50);
    }
    else 
    {
        if (_anim.Animation != "idle") _anim.Play("idle");
        _runEffect.Visible = false;
        _runEffect.Stop(); // On force l'arrêt de l'effet
    }
}

    private void StartDash()
    {
        _isDashing = true;
        _dashCooldownTimer = DashCooldown;
        // On laisse UpdateAnimations lancer les visuels
        GetTree().CreateTimer(0.2f).Timeout += () => _isDashing = false;
    }

    private void HandleDash(double delta, ref Vector2 velocity)
    {
        velocity.X = (_anim.FlipH ? -1 : 1) * DashSpeed;
        velocity.Y = 0; // Dash parfaitement horizontal
    }

    // --- ATTAQUES ---
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
        if (_anim.Animation == "attack" || _anim.Animation == "attack_effect_up" || _anim.Animation == "attack_effect_down")
        {
            _isAttacking = false;
            _bladeEffect.Visible = false;
        }
    }
}