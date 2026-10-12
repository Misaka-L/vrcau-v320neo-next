using JetBrains.Annotations;
using SaccFlightAndVehicles;
using UdonSharp;
using UnityEngine;
using VAU.V320NeoNext.Runtime.Bus;

//note:this code is original from https://github.com/esnya/EsnyaSFAddons
//to satisfy vau320's demand, add autotrim
//to optimize change vellift in SAV to trim
//2024-09-29 尝试一个新东西，先把这个脚本作用在JoystickOverridde上，（FBW？）
//2026-10-12 自动配平逻辑移动到 LegacyAutoElevatorTrim
namespace VAU.V320NeoNext.Runtime.Systems.FlightControl.SaccExt
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DefaultExecutionOrder(AircraftScriptsExecutionOrder.SaccJoystickOverridenActuator)]
    public class DFUNC_a320_ElevatorTrim : AbstractAvionicsBusClient
    {
        public SaccAirVehicle SAVControl;
        public string animatorParameterName = "elevtrim";

        private Rigidbody _vehicleRigidbody;
        private Transform _pitchForcePosition;
        private Animator _vehicleAnimator;

        private float _rotMultiMaxSpeed;

        public float initialTrim = 0.3f;
        private float _prevTrim;

        private float Trim
        {
            get => _ReadFloat(TrimPositionId);
            set => _WriteFloat(TrimPositionId, value);
        }

        private const AvionicsBusFloatDataIds TrimPositionId =
            AvionicsBusFloatDataIds.V32NN_Frequent_ElevatorTrim_Sync_TrimPosition;

        #region Lifetime (Start, Respawn)

        public void SFEXT_L_EntityStart()
        {
            _rotMultiMaxSpeed = SAVControl.RotMultiMaxSpeed;
            _vehicleAnimator = SAVControl.VehicleAnimator;
            _vehicleRigidbody = SAVControl.VehicleRigidbody;
            _pitchForcePosition = SAVControl.PitchMoment;
        }
        
        private void Start()
        {
            _rotMultiMaxSpeed = SAVControl.RotMultiMaxSpeed;
            _vehicleAnimator = SAVControl.VehicleAnimator;
            _vehicleRigidbody = SAVControl.VehicleRigidbody;
            _pitchForcePosition = SAVControl.PitchMoment;
        }

        protected override void _OnAvionicsBusStart()
        {
            _WriteFloat(TrimPositionId, initialTrim);
            _SubscribeBool(AvionicsBusBoolDataIds.Sim_Infrequent_HasPilot, nameof(_HasPilotChanged));

            ResetStatus();
        }

        protected override void _OnAvionicsBusRespawn_RunInOwner() => ResetStatus();

        public void _HasPilotChanged()
        {
            if (_ReadBool(AvionicsBusBoolDataIds.Sim_Infrequent_HasPilot))
                gameObject.SetActive(true);
        }

        #endregion

        private void ResetStatus()
        {
            Trim = initialTrim;
            _prevTrim = initialTrim;

            if (_vehicleAnimator) _vehicleAnimator.SetFloat(animatorParameterName, .5f);

            DoTrimAnimatorAndUiUpdate();
        }

        private void DoTrimAnimatorAndUiUpdate()
        {
            if (_vehicleAnimator) _vehicleAnimator.SetFloat(animatorParameterName, Remap01(Trim, -1, 1));
        }

        private void FixedUpdate()
        {
            var hasPilot = _ReadBool(AvionicsBusBoolDataIds.Sim_Infrequent_HasPilot);
            var isPilot = _ReadBool(AvionicsBusBoolDataIds.Sim_Infrequent_IsPilot);

            var trimChanged = !Mathf.Approximately(Trim, _prevTrim);
            _prevTrim = Trim;

            if (trimChanged) DoTrimAnimatorAndUiUpdate();
            if (!hasPilot && !trimChanged)
                gameObject.SetActive(false);

            if (!isPilot) return;

            var rotLift = Mathf.Clamp(SAVControl.AirSpeed / _rotMultiMaxSpeed, -1, 1);

            var trimForce = (Trim * SAVControl.PitchStrength) * rotLift * SAVControl.Atmosphere *
                            -SAVControl.VehicleTransform.up;
            trimForce *= _vehicleRigidbody.mass;
            _vehicleRigidbody.AddForceAtPosition(trimForce, _pitchForcePosition.position, ForceMode.Force);

            //尝试了不同的作用力方式
            //1.改VelLiftStart
            //SAVControl.SetProgramVariable("VelLiftStart", trim * trimStrength + trimBias);
            //2.AddForceAtPosition

            //Vector3 trimPitching = Vector3.zero;
            //trim *= SAVControl.PitchStrength;
            //trim *= rotlift * Mathf.Min(SAVControl.AoALiftPitch, SAVControl.AoALiftYaw);
            //var downspeed = -Vector3.Dot(SAVControl.AirVel, SAVControl.VehicleTransform.up);
            //trimPitching = ((((SAVControl.VehicleTransform.up * trim) + (SAVControl.VehicleTransform.up * downspeed * SAVControl.VelStraightenStrPitch * SAVControl.AoALiftPitch * rotlift)) * SAVControl.Atmosphere));
            //vehicleRigidbody.AddForceAtPosition(trimPitching, transform.position, ForceMode.Force);//deltatime is built into ForceMode.Force

            //3.写JoystickOverride
            //FBWRotationInputs.x = Mathf.Clamp(trim, -1, 1);
            //FBWRotationInputs.y = 0;
            //FBWRotationInputs.z = 0;
            //SAVControl.SetProgramVariable("JoystickOverride", FBWRotationInputs);
        }

        private float Remap01(float value, float oldMin, float oldMax)
        {
            return (value - oldMin) / (oldMax - oldMin);
        }

        #region User Input

        public KeyCode desktopUp = KeyCode.PageUp, desktopDown = KeyCode.PageDown;

        public float desktopStep = 0.02f;

        [PublicAPI]
        public void _TrimUp()
        {
            Trim += desktopStep;
        }

        [PublicAPI]
        public void _TrimDown()
        {
            Trim -= desktopStep;
        }

        public override void PostLateUpdate()
        {
            if (!_ReadBool(AvionicsBusBoolDataIds.Sim_Infrequent_IsPilot)) return;

            if (Input.GetKey(desktopUp)) _TrimUp();
            if (Input.GetKey(desktopDown)) _TrimDown();
        }

        #endregion
    }
}