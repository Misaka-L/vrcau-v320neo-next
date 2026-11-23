using UnityEngine;
using VAU.V320NeoNext.Runtime.Bus;

namespace VAU.V320NeoNext.Runtime.Systems.FlightControl.Flybywire.Law.Pitch
{
    public partial class PitchLaw
    {
        private const float AoaToPitchRateMin = -5f;
        private const float AoaToPitchRateMax = 5f;

        private float _lastAoaProtectionWeight;
        private float _aoaProtectionLastError;
        private float _aoaToPitchRateIntegrator;
        private bool _inAoAProtection;

        private float GetHighAoaProtectionWeight()
        {
            var aoa = _ReadFloat(AvionicsBusFloatDataIds.V32NN_Frequent_ADIRS_ADR_1_AoA);
            var protectionAoa = _ReadFloat(AvionicsBusFloatDataIds.V32NN_Frequent_FAC_1_ProtectionAoa);

            if (!_inAoAProtection)
            {
                _inAoAProtection = aoa > protectionAoa;
                if (_inAoAProtection)
                {
                    _aoaToPitchRateIntegrator = 0;
                    _aoaProtectionLastError = 0;
                }
            }
            else if (aoa < protectionAoa - 0.5f)
            {
                _inAoAProtection = false;
            }

            var weightTarget = _inAoAProtection ? 1 : 0;
            var weightMaxDelta = Time.fixedDeltaTime * 1.5f;

            var weight =
                Mathf.MoveTowards(_lastAoaProtectionWeight, weightTarget, weightMaxDelta);
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
            _WriteFloat(AvionicsBusFloatDataIds.V32NN_Frequent_FlyByWireDebug_HighAoAProtectionTargetAoA, aoaTarget);

            var output = Mathf.Clamp(FlightControlLaw.RunPid(
                aoaTarget, aoa,
                ref _aoaToPitchRateIntegrator, ref _aoaProtectionLastError,
                aoaToPitchRateKp, aoaToPitchRateKi, aoaToPitchRateKd,
                aoaToPitchRateKdMinIntegrator, aoaToPitchRateKdMaxIntegrator
            ), AoaToPitchRateMin, AoaToPitchRateMax);

            _WriteFloat(AvionicsBusFloatDataIds.V32NN_Frequent_FlyByWireDebug_HighAoAProtectionIntegrator,
                _aoaToPitchRateIntegrator);
            _WriteFloat(AvionicsBusFloatDataIds.V32NN_Frequent_FlyByWireDebug_HighAoAProtectionOutput, output);
            return output;
        }
    }
}