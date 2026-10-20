using UdonSharp;
using UnityEngine;
using VAU.V320NeoNext.Runtime.Bus;

namespace VAU.V320NeoNext.Runtime.Systems.FlightControl.Flybywire.Law
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DefaultExecutionOrder(AircraftScriptsExecutionOrder.FlightControlComputer)]
    public sealed partial class FlightControlLaw : AbstractAvionicsBusClient
    {
        // Use variable in partial will mess up version control due to u# bugs.....

        #region Variables

        #region Pitch Law

        [Header("Pitch Law PID")] [Header("C Star -> Pitch Rate")]
        public float cStarToPitchRateKp = 1;
        public float cStarToPitchRateKi = 10;
        public float cStarToPitchRateKd = 0.05f;
        public float cStarToPitchRateKdMinIntegrator = -1;
        public float cStarToPitchRateKdMaxIntegrator = 1;

        private float _cStarToPitchRateIntegrator;
        private float _cStarToPitchRateLastError;

        [Header("Pitch Rate -> Sacc Output")]
        public float pitchRateToSaccInputKp = -1;
        public float pitchRateToSaccInputKi = -1;
        public float pitchRateToSaccInputKd = -0.02f;
        public float pitchRateToSaccInputMinIntegrator = -1;
        public float pitchRateToSaccInputMaxIntegrator = 1;

        private float _pitchRateToSaccInputIntegrator;
        private float _pitchRateToSaccInputLastError;

        [Header("Pitch Attitude Limit")] public float pitchAttitudeLimitKp = 0.5f;

        [Header("High Speed Protection")]
        public float highSpeedProtectionKp = -0.3f;

        #endregion

        #endregion

        private void FixedUpdate()
        {
            if (!_ReadBool(AvionicsBusBoolDataIds.Sim_Infrequent_HaveAircraftOwnership))
                return;

            var pilotInput = GetPilotInput();

            var pitchOutput = RunPitchLaw(pilotInput.x);
            var rollOutput = RunRollLaw(pilotInput.z);
            var yawOutput = RunYawLaw(pilotInput.y);

            _WriteVector3(
                AvionicsBusVector3DataIds.V32NN_Frequent_FlightControl_FinalPilotInputToSacc,
                new Vector3(pitchOutput, yawOutput, rollOutput)
            );
        }

        private Vector3 GetPilotInput()
        {
            if (_ReadBool(AvionicsBusBoolDataIds.V32NN_Infrequent_AutoPilot_Engaged))
                return _ReadVector3(AvionicsBusVector3DataIds.V32NN_Frequent_FlightControl_AutoPilotInput);

            return _ReadVector3(AvionicsBusVector3DataIds.V32NN_Frequent_FlightControl_LocalPilotInput);
        }
    }
}