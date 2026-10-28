using UnityEngine;
using VAU.V320NeoNext.Runtime.Bus;

namespace VAU.V320NeoNext.Runtime.Systems.FlightControl.Flybywire.Law.Pitch
{
    public partial class PitchLaw
    {
        [Header("Aoa -> Pitch Rate")]
        public float aoaToPitchRateKp = 1;
        public float aoaToPitchRateKi = 10;
        public float aoaToPitchRateKd = 0.05f;
        public float aoaToPitchRateKdMaxIntegrator = 1;
        public float aoaToPitchRateKdMinIntegrator = -1;

        public float _aoaToPitchRateIntegrator;
        public float _targetAoa;
        public float _aoaError;

        public float aoaToPitchRateOutput;

        private float _lastAoaProtectionWeight;

        private float GetHighAoaProtectionWeight()
        {
            var aoa = _ReadFloat(AvionicsBusFloatDataIds.V32NN_Frequent_ADIRS_ADR_1_AoA);
            var protectionAoa = _ReadFloat(AvionicsBusFloatDataIds.V32NN_Frequent_FAC_1_ProtectionAoa);

            var weightTarget = aoa > protectionAoa ? 1f : 0f;
            var weight =
                Mathf.MoveTowards(_lastAoaProtectionWeight, weightTarget, Time.fixedDeltaTime);
            _lastAoaProtectionWeight = weight;

            return weight;
        }
        
        private float RunHighAoaProtection(float pilotPitchInput)
        {
            var aoa = _ReadFloat(AvionicsBusFloatDataIds.V32NN_Frequent_ADIRS_ADR_1_AoA);
            var maxAoa = _ReadFloat(AvionicsBusFloatDataIds.V32NN_Frequent_FAC_1_MaxAoa);
            var protectionAoa = _ReadFloat(AvionicsBusFloatDataIds.V32NN_Frequent_FAC_1_ProtectionAoa);

            var aoaTarget = protectionAoa + (protectionAoa - maxAoa) * pilotPitchInput;
            _targetAoa = aoaTarget;
            _aoaError = aoaTarget - aoa;

            aoaToPitchRateOutput = FlightControlLaw.RunPid(
                aoaTarget, aoa,
                ref _aoaToPitchRateIntegrator, ref highSpeedProtectionLastError,
                aoaToPitchRateKp, aoaToPitchRateKi, aoaToPitchRateKd,
                aoaToPitchRateKdMinIntegrator, aoaToPitchRateKdMaxIntegrator
            );

            return aoaToPitchRateOutput;
        }
    }
}