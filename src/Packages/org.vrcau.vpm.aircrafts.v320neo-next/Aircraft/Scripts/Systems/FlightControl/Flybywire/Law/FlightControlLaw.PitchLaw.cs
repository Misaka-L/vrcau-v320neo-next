using UnityEngine;
using VAU.V320NeoNext.Runtime.Bus;

namespace VAU.V320NeoNext.Runtime.Systems.FlightControl.Flybywire.Law
{
    public sealed partial class FlightControlLaw
    {
        [Header("C Star Debug")]
        public float currentCStar;
        public float cStarTarget;
        public float cStarError;
        public float cStarToPitchRateIntegrator;

        [Header("Pitch Rate Override Debug")]
        public bool pilotInputAsPitchRate;
        public float pilotInputAsPitchRateScale;
        public bool overridePitchRateCommand;
        public float overridePitchRateCommandValue;

        [Header("Pitch Rate Debug")]
        public float currentPitchRate;
        public float pitchRateTarget;
        public float pitchRateError;
        public float pitchRateToSaccInputIntegrator;
        public float pitchRateToSaccInput;

        private float RunPitchLaw(float pilotPitchInput)
        {
            var isCleanConfig = _ReadInt(AvionicsBusIntDataIds.V32NN_Frequent_Flap_SFCC_1_ActualFlapPosition) == 0;

            var cStarTarget = -(isCleanConfig
                ? pilotPitchInput < 0 ? pilotPitchInput * 1.5f : pilotPitchInput
                : pilotPitchInput);

            var targetPitchRate = RunCStarToPitchRate(cStarTarget);
            
            if (pilotInputAsPitchRate)
                targetPitchRate = pilotPitchInput * pilotInputAsPitchRateScale;
            if (overridePitchRateCommand)
                targetPitchRate = overridePitchRateCommandValue;
            var outputToSacc = RunPitchRateToSaccOutput(targetPitchRate);
            this.pitchRateToSaccInput = outputToSacc;

            if (!_ReadBool(AvionicsBusBoolDataIds.V32NN_Infrequent_LandingGear_Sync_GearLeverUp))
                return pilotPitchInput;
            return outputToSacc;
        }

        private float RunCStarToPitchRate(float cStarTarget)
        {
            var kp = cStarToPitchRateKp;
            var ki = cStarToPitchRateKi;
            var kd = cStarToPitchRateKd;

            var currentCStar = GetCStar();
            this.cStarTarget = cStarTarget;
            this.currentCStar = currentCStar;
            this.cStarError = cStarTarget - currentCStar;
            this.cStarToPitchRateIntegrator = _cStarToPitchRateIntegrator;

            return RunPid(
                cStarTarget, currentCStar,
                ref _cStarToPitchRateIntegrator, ref _cStarToPitchRateLastError, kp, ki, kd,
                cStarToPitchRateKdMinIntegrator, cStarToPitchRateKdMaxIntegrator
            );
        }

        private float RunPitchRateToSaccOutput(float pitchRateTarget)
        {
            var kp = pitchRateToSaccInputKp;
            var ki = pitchRateToSaccInputKi;
            var kd = pitchRateToSaccInputKd;

            var currentPitchRate =
                _ReadFloat(AvionicsBusFloatDataIds.V32NN_Frequent_ADIRS_IR_1_PitchRateInDegPerSecond);
            this.currentPitchRate = currentPitchRate;
            this.pitchRateTarget = pitchRateTarget;
            this.pitchRateError = pitchRateTarget - currentPitchRate;
            this.pitchRateToSaccInputIntegrator = _pitchRateToSaccInputIntegrator;

            return Mathf.Clamp(RunPid(
                pitchRateTarget, currentPitchRate,
                ref _pitchRateToSaccInputIntegrator, ref _pitchRateToSaccInputLastError, kp, ki, kd,
                pitchRateToSaccInputMinIntegrator, pitchRateToSaccInputMaxIntegrator
            ), -1f, 1f);
        }

        private float GetCStar()
        {
            var gLoad = _ReadFloat(AvionicsBusFloatDataIds.V32NN_Frequent_ADIRS_IR_1_Vertical_G_Load);
            var pitchRad = _ReadFloat(AvionicsBusFloatDataIds.V32NN_Frequent_ADIRS_IR_1_Pitch) * Mathf.Deg2Rad;
            var rollRad = _ReadFloat(AvionicsBusFloatDataIds.V32NN_Frequent_ADIRS_IR_1_Roll) * Mathf.Deg2Rad;
            var pitchRateRad =
                _ReadFloat(AvionicsBusFloatDataIds.V32NN_Frequent_ADIRS_IR_1_PitchRateInDegPerSecond) * Mathf.Deg2Rad;

            const float crossSpeed = 121.92f; // m/s 400ft/s~
            const float gravity = 9.80665f;

            var nzAero = gLoad - Mathf.Cos(pitchRad) / Mathf.Cos(rollRad);

            return nzAero + (crossSpeed / gravity) * pitchRateRad;
        }
    }
}