using UnityEngine;
using VAU.V320NeoNext.Runtime.Bus;

namespace VAU.V320NeoNext.Runtime.Systems.FlightControl.Flybywire.Law
{
    public sealed partial class FlightControlLaw
    {
        private float RunPitchLaw(float pilotPitchInput)
        {
            GetCStar();
            return pilotPitchInput;
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