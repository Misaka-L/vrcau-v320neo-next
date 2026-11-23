using UdonSharp;
using UnityEngine;
using VAU.V320NeoNext.Runtime.Bus;

namespace VAU.V320NeoNext.Runtime.Systems.FlightControl.Flybywire.Law.Pitch
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public sealed partial class PitchLaw : AbstractAvionicsBusClient
    {
        [Header("Pitch Rate -> Sacc Output")]
        public float pitchRateToSaccInputKp = -1;
        public float pitchRateToSaccInputKi = -1;
        public float pitchRateToSaccInputKd = -0.02f;
        public float pitchRateToSaccInputMinIntegrator = -1;
        public float pitchRateToSaccInputMaxIntegrator = 1;

        [Header("Pitch Law PID")] [Header("C Star -> Pitch Rate")]
        public float cStarToPitchRateKp = 1;

        public float cStarToPitchRateKi = 10;
        public float cStarToPitchRateKd = 0.05f;
        public float cStarToPitchRateKdMinIntegrator = -1;
        public float cStarToPitchRateKdMaxIntegrator = 1;

        [Header("Pitch Attitude Limit")] public float pitchAttitudeLimitKp = 0.5f;
        [Header("High Speed Protection")] public float highSpeedProtectionKp = -0.3f;

        [Header("Aoa -> Pitch Rate")]
        public float aoaToPitchRateKp = 1;
        public float aoaToPitchRateKi = 10;
        public float aoaToPitchRateKd = 0.05f;
        public float aoaToPitchRateKdMaxIntegrator = 1;
        public float aoaToPitchRateKdMinIntegrator = -1;

        private float _pitchRateToSaccInputIntegrator;
        private float _pitchRateToSaccInputLastError;

        public float RunPitchLaw(float pilotPitchInput)
        {
            var cStarTarget = GetFinalCStarTarget(pilotPitchInput);
            var targetPitchRate = RunCStarToPitchRate(cStarTarget);

            var highAoaProtectionWeight = GetHighAoaProtectionWeight();
            if (highAoaProtectionWeight > 0)
            {
                targetPitchRate = targetPitchRate * (1 - highAoaProtectionWeight) +
                                  RunHighAoaProtection(pilotPitchInput) * highAoaProtectionWeight;
            }

            _WriteFloat(AvionicsBusFloatDataIds.V32NN_Frequent_FlyByWireDebug_PitchRateTarget, targetPitchRate);
            var outputToSacc = RunPitchRateToSaccOutput(targetPitchRate);

            if (!_ReadBool(AvionicsBusBoolDataIds.V32NN_Infrequent_LandingGear_Sync_GearLeverUp))
                return pilotPitchInput;
            return outputToSacc;
        }
        
        private float RunPitchRateToSaccOutput(float pitchRateTarget)
        {
            var kp = pitchRateToSaccInputKp;
            var ki = pitchRateToSaccInputKi;
            var kd = pitchRateToSaccInputKd;

            var currentPitchRate =
                _ReadFloat(AvionicsBusFloatDataIds.V32NN_Frequent_ADIRS_IR_1_PitchRateInDegPerSecond);

            var pitchRateRawOutput = FlightControlLaw.RunPid(
                pitchRateTarget, currentPitchRate,
                ref _pitchRateToSaccInputIntegrator, ref _pitchRateToSaccInputLastError, kp, ki, kd,
                pitchRateToSaccInputMinIntegrator, pitchRateToSaccInputMaxIntegrator
            );

            return Mathf.Clamp(pitchRateRawOutput, -1f, 1f);
        }
    }
}