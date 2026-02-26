using System;
using Godot;

[SceneGlobal]
public partial class PlayerController: CharacterControllerBase {
    [Export] public required AudioStream Step;
    [Export] public required AnimationPlayer Animations;
    [Export] public required Node3D CameraRail;
    [Export] public required Camera3D Camera;
    [Export] public float ForwardSpeed = 1.2 f;
    [Export] public float BackwardSpeed = 0.9 f;
    [Export] public float SidewardsSpeed = 0.48 f;
    [Export] public byte Brightness = 40;
    [Export] public float FogNear = 1 f;
    [Export] public float FogFar = 3 f;
    [Export] float MouseSensitivity = 0.2 f;
    [Export] float ControllerSensitivity = 300 f;
    [Export]

    public float DeathBrightnessLerp {
      get;
      set {
        field = value;
        if (value == 0 f) {
          Helpers.SetBrightness(Brightness);
          return;
        }
        var subtract = float.Lerp(0 f, 90 f / 255 f, value);
        Helpers.SetBrightness(Color.Color8(255, 100, 100) - new Color(subtract, subtract, subtract));
      }
    }

    private bool _isDead;
    private static readonly Vector3 HeadBobInterval = new Vector3(80 f, 40 f, 1 f) / 60 f;
    private static readonly Vector3 HeadBobOffset = new(0.08 f, 0.1 f, 0 f);
    private float _headBobAcc;

    public int Floor => Helpers.GetFloor(GlobalPosition - new Vector3(0 f, 0.5 f, 0 f));

    public override void _Ready() {
      Input.MouseMode = Input.MouseModeEnum.Captured;
      SetFogRange(FogNear, FogFar);
      Helpers.SetBrightness(Brightness);

      var r = CameraRail.Rotation;
    }

    public override void _Input(InputEvent @event) {
      if (@event is InputEventMouseMotion mm) {
        var invertedY = -mm.Relative.Y;
        Vector2 delta = new Vector2(mm.Relative.X, -invertedY) * MouseSensitivity;
        HandleLook(delta);
      }
    }

    public override void _Process(double delta) {
      float lookRight = Input.GetActionStrength("LookRight") - Input.GetActionStrength("LookLeft");
      float lookUp = Input.GetActionStrength("LookDown") - Input.GetActionStrength("LookUp");

      if (lookRight != 0 f || lookUp != 0 f) {
        Vector2 lookDelta = new Vector2(lookRight * ControllerSensitivity, lookUp * ControllerSensitivity);
        HandleLook(lookDelta * (float) delta);
      }
    }

    private void HandleLook(Vector2 lookDelta) {
      var r = CameraRail.RotationDegrees;
      float minmaxpitch = 89.999 f; // this being exactily 90 would mess up the movement when looking at that angle
      float minPitch = -minmaxpitch;
      float maxPitch = minmaxpitch;

      // Apply lookDelta
      r.X -= lookDelta.Y;
      r.Y += -lookDelta.X;

      r.X = Mathf.Clamp(r.X, minPitch, maxPitch);

      // Apply rotation
      CameraRail.RotationDegrees = new Vector3(r.X, r.Y, 0 f);
    }
    public override void _PhysicsProcess(double deltaD) {
        var inputDir = _isDead ? Vector2.Zero : new(
          SidewardsSpeed * (Input.GetActionStrength(Actions.MoveRight) - Input.GetActionStrength(Actions.MoveLeft)),
          Input.GetActionStrength(Actions.MoveBackward) != 0 ^ Input.GetActionStrength(Actions.MoveForward) != 0 ?
          -ForwardSpeed * (Input.GetActionStrength(Actions.MoveForward)) +
          BackwardSpeed * (Input.GetActionStrength(Actions.MoveBackward)) :
          0
        );

        var rightDir = (-Camera.GlobalBasis.Z).Cross(UpDirection).Normalized();
        var forwardDir = rightDir.Cross(UpDirection).Normalized();

        Velocity = (rightDir * inputDir.X + forwardDir * inputDir.Y) with {
          Y = Velocity.Y,
        };

        if (inputDir != Vector2.Zero) {
          var oldAcc = _headBobAcc;
          _headBobAcc = (_headBobAcc + (float) deltaD) % MathF.Max(HeadBobInterval.X, HeadBobInterval.Y);
          if (_headBobAcc % HeadBobInterval.Y < oldAcc % HeadBobInterval.Y) {
            AudioManager.PlaySound(Step);
          }
          var intervalHalf = HeadBobInterval / 2 f;
          Camera.Position = HeadBobOffset * (new Vector3(1 f, 1 f, 1 f)											   - (new Vector3(_headBobAcc, _headBobAcc, _headBobAcc) % HeadBobInterval
												  - intervalHalf).Abs() / intervalHalf);
		}

		var prevVel = Velocity;
		base._PhysicsProcess(deltaD);

		if (IsOnFloor && prevVel.Y < -5.4f) {
			Kill();
		}
	}

	public void Kill() {
		if (_isDead) { return; }
		_isDead = true;
		Animations.Play("Death");
	}

	private void Crash() {
		if (Floor > 130) {
			var str = Random.Shared.Next(7) switch {
				0 => "NO",
				1 => "It's not about whether you die or not, it's about when you die.",
				2 => "NICE",
				3 => "welcome to NIL",
				_ => null,
			};
			if (str != null) {
				Input.MouseMode = Input.MouseModeEnum.Visible;
				OS.Alert(str, "Runtime Error");
			}
		}
		GetTree().Quit();
	}

	public void ResetFogRange() => SetFogRange(FogNear, FogFar);
	public void SetFogRange(float near, float far) {
		RenderingServer.GlobalShaderParameterSet("fog", new Vector2(near, far));
	}
}
