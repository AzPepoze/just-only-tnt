using Godot;

namespace justonlytnt.Player;

public sealed partial class PlayerController
{
    public override void _PhysicsProcess(double delta)
    {
        if (_world is null || _tnt is null || _camera is null)
        {
            return;
        }

        if (!_isFlying && _initialGroundLockActive)
        {
            _initialGroundLockTimer += (float)delta;
            if (IsStandingChunkReady() || IsOnFloor() || _initialGroundLockTimer >= InitialGroundLockSeconds)
            {
                _initialGroundLockActive = false;
            }
            else
            {
                Velocity = Vector3.Zero;
                return;
            }
        }

        if (!_isFlying && GlobalPosition.Y < -20f)
        {
            GlobalPosition = new Vector3(GlobalPosition.X, _world.Config.ChunkHeight * 0.75f, GlobalPosition.Z);
            Velocity = Vector3.Zero;
            _initialGroundLockActive = true;
            _initialGroundLockTimer = 0f;
        }

        float dt = (float)delta;
        double now = Time.GetTicksMsec() / 1000.0;

        bool jumpPressed = _jumpPressedQueued;
        _jumpPressedQueued = false;

        if (jumpPressed)
        {
            if ((now - _lastJumpPressedTime) <= DoubleTapWindowSeconds)
            {
                _isFlying = !_isFlying;
                Velocity = Vector3.Zero;
                _lastJumpPressedTime = -10.0;
            }
            else
            {
                _lastJumpPressedTime = now;
            }
        }

        Vector2 input = _cachedMoveInput;
        Vector3 moveDir = (Transform.Basis * new Vector3(input.X, 0f, input.Y)).Normalized();
        Vector3 horizontalVelocity = new Vector3(Velocity.X, 0f, Velocity.Z);

        if (_isFlying)
        {
            float flySpeed = _cachedSprintHeld ? FlySprintSpeed : FlySpeed;
            Vector3 targetFlyHorizontal = moveDir * flySpeed;
            horizontalVelocity = horizontalVelocity.MoveToward(targetFlyHorizontal, GroundAcceleration * dt);

            float verticalAxis = 0f;
            if (_cachedJumpHeld)
            {
                verticalAxis += 1f;
            }
            if (_cachedDescendHeld)
            {
                verticalAxis -= 1f;
            }

            Velocity = new Vector3(horizontalVelocity.X, verticalAxis * FlyVerticalSpeed, horizontalVelocity.Z);
        }
        else
        {
            float targetSpeed = _cachedSprintHeld ? SprintSpeed : WalkSpeed;
            Vector3 targetHorizontal = moveDir * targetSpeed;

            if (targetHorizontal.LengthSquared() > 0.0001f)
            {
                float accel = IsOnFloor() ? GroundAcceleration : AirAcceleration;
                horizontalVelocity = horizontalVelocity.MoveToward(targetHorizontal, accel * dt);
            }
            else
            {
                horizontalVelocity = horizontalVelocity.MoveToward(Vector3.Zero, GroundDeceleration * dt);
            }

            if (!IsOnFloor())
            {
                Velocity += Vector3.Down * 18f * dt;
            }
            else if (jumpPressed)
            {
                Velocity = new Vector3(Velocity.X, JumpVelocity, Velocity.Z);
            }

            Velocity = new Vector3(horizontalVelocity.X, Velocity.Y, horizontalVelocity.Z);
        }

        MoveAndSlide();

        bool placePressed = _placePressedQueued;
        _placePressedQueued = false;
        if (placePressed)
        {
            HandleSecondaryAction();
        }

        bool ignitePressed = _ignitePressedQueued;
        _ignitePressedQueued = false;
        if (ignitePressed)
        {
            HandlePrimaryAction();
        }
    }
}
