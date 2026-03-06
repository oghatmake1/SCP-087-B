using System;
using Godot;

[SceneGlobal]
public partial class PlayerController : CharacterControllerBase {
    [Export] public required AudioStream Step;

    [Export] public required AnimationPlayer Animations;
    [Export] public required XRCamera3D VrCamera;
    [Export] public float ForwardSpeed = 1.2f;
    [Export] public float BackwardSpeed = 0.9f;
    [Export] public float SidewardsSpeed = 0.48f;

    [Export] public byte Brightness = 40;
    [Export] public float FogNear = 1f;
    // Originally 2.5f, adjusted to account for radial fog.
    [Export] public float FogFar = 3f;

    [Export] private float ControllerSensitivity = 300f;

    [Export]
    public float DeathBrightnessLerp {
        get;
        set {
            field = value;

            if (value == 0f) {
                Helpers.SetBrightness(Brightness);
                return;
            }

            var subtract = float.Lerp(0f, 90f / 255f, value);
            Helpers.SetBrightness(Color.Color8(255, 100, 100) - new Color(subtract, subtract, subtract));
        }
    }

    private bool _isDead;
    private float _stepAcc;
    private const float StepInterval = 0.45f;

    public int Floor => Helpers.GetFloor(GlobalPosition - new Vector3(0f, 0.5f, 0f));
    public Camera3D Camera => VrCamera;


    public override void _Ready() {
        if (!TryEnableVr()) {
            OS.Alert("OpenXR headset not detected. This build is VR-only.", "VR Required");
            GetTree().Quit();
            return;
        }

        Input.MouseMode = Input.MouseModeEnum.Captured;
        SetFogRange(FogNear, FogFar);
        Helpers.SetBrightness(Brightness);
        VrCamera.Current = true;
    }

    public override void _Process(double delta) {
        var turnInput = Input.GetAxis("LookLeft", "LookRight");
        if (Mathf.Abs(turnInput) > 0.01f) {
            RotateY(-turnInput * Mathf.DegToRad(ControllerSensitivity) * (float)delta);
        }
    }

    public override void _PhysicsProcess(double deltaD) {
        var inputDir = _isDead ? Vector2.Zero : new(
            SidewardsSpeed * (Input.GetActionStrength(Actions.MoveRight) - Input.GetActionStrength(Actions.MoveLeft)),
            Input.GetActionStrength(Actions.MoveBackward) != 0 ^ Input.GetActionStrength(Actions.MoveForward) != 0
                ? -ForwardSpeed * (Input.GetActionStrength(Actions.MoveForward))
                  + BackwardSpeed * (Input.GetActionStrength(Actions.MoveBackward))
                : 0
        );

        var rightDir = (-VrCamera.GlobalBasis.Z).Cross(UpDirection).Normalized();
        var forwardDir = rightDir.Cross(UpDirection).Normalized();

        Velocity = (rightDir * inputDir.X + forwardDir * inputDir.Y) with {
            Y = Velocity.Y,
        };

        if (inputDir != Vector2.Zero) {
            _stepAcc += (float)deltaD;
            if (_stepAcc >= StepInterval) {
                _stepAcc = 0f;
                AudioManager.PlaySound(Step);
            }
        } else {
            _stepAcc = 0f;
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

    private bool TryEnableVr() {
        var xr = XRServer.FindInterface("OpenXR");
        if (xr == null || !xr.Initialize()) {
            return false;
        }

        GetViewport().UseXR = true;
        return true;
    }
}
