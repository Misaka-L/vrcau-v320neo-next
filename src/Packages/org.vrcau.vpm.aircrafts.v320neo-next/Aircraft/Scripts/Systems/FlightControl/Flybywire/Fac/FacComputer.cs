using UdonSharp;
using UnityEngine;
using VAU.V320NeoNext.Runtime.Bus;

namespace VAU.V320NeoNext.Runtime.Systems.FlightControl.Flybywire.Fac
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DefaultExecutionOrder(AircraftScriptsExecutionOrder.Fac)]
    public sealed class FacComputer : AbstractAvionicsBusClient
    {
        private const float FlapUpMaxAoA = 7f;
        private const float Flap1MaxAoa = 9.5f;
        private const float Flap2MaxAoa = 9.5f;
        private const float Flap3MaxAoa = 9.5f;
        private const float FlapFullMaxAoa = 9.5f;

        private void FixedUpdate()
        {
            var ias = _ReadFloat(AvionicsBusFloatDataIds.V32NN_Frequent_ADIRS_ADR_1_IndicatedAirspeed);
            var aoa = _ReadFloat(AvionicsBusFloatDataIds.V32NN_Frequent_ADIRS_ADR_1_AoA);
            var gLoad = _ReadFloat(AvionicsBusFloatDataIds.V32NN_Frequent_ADIRS_IR_1_Vertical_G_Load);
            var maxAoa = GetMaxAoa();

            var vs1g = ias * Mathf.Sqrt(aoa / maxAoa);
            // var vStall = vs1g * Mathf.Sqrt(gLoad);

            _WriteFloat(AvionicsBusFloatDataIds.V32NN_Frequent_FAC_1_Stall_Warning_Speed, vs1g);
        }

        private float GetMaxAoa()
        {
            var flapIndex = _ReadInt(AvionicsBusIntDataIds.V32NN_Frequent_Flap_SFCC_1_ActualFlapPosition);
            // 负值表示正在向 -index-1 档移动，取目标档位
            if (flapIndex < 0) flapIndex = -flapIndex - 1;
            switch (flapIndex)
            {
                case 1:
                    return Flap1MaxAoa;
                case 2:
                    return Flap2MaxAoa;
                case 3:
                    return Flap3MaxAoa;
                case 4:
                    return FlapFullMaxAoa;
                default:
                    return FlapUpMaxAoA;
            }
        }
    }
}