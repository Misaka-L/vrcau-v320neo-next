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
        private bool _inAoAProtection;

        private float GetHighAoaProtectionWeight()
        {
            var aoa = _ReadFloat(AvionicsBusFloatDataIds.V32NN_Frequent_ADIRS_ADR_1_AoA);
            var protectionAoa = _ReadFloat(AvionicsBusFloatDataIds.V32NN_Frequent_FAC_1_ProtectionAoa);

            if (!_inAoAProtection)
            {
                _inAoAProtection = aoa > protectionAoa;
            }
            else if (aoa < protectionAoa - 1)
            {
                _inAoAProtection = false;
            }

            var weightTarget = _inAoAProtection ? 1 : 0;
            var weightMaxDelta = Time.fixedDeltaTime * (_inAoAProtection ? 1f : 0.3f);

            var weight =
                Mathf.MoveTowards(_lastAoaProtectionWeight, weightTarget,  weightMaxDelta);
            _WriteFloat(AvionicsBusFloatDataIds.V32NN_Frequent_FlyByWireDebug_HighAoAProtectionWeight, weight);
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
            _WriteFloat(AvionicsBusFloatDataIds.V32NN_Frequent_FlyByWireDebug_HighAoAProtectionTargetAoA, aoaTarget);

            aoaToPitchRateOutput = FlightControlLaw.RunPid(
                aoaTarget, aoa,
                ref _aoaToPitchRateIntegrator, ref highSpeedProtectionLastError,
                aoaToPitchRateKp, aoaToPitchRateKi, aoaToPitchRateKd,
                aoaToPitchRateKdMinIntegrator, aoaToPitchRateKdMaxIntegrator
            );

            _WriteFloat(AvionicsBusFloatDataIds.V32NN_Frequent_FlyByWireDebug_HighAoAProtectionOutput, aoaToPitchRateOutput);
            return aoaToPitchRateOutput;
        }
    }
}