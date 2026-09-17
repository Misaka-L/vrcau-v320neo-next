using UdonSharp;
using VAU.V320NeoNext.Runtime.Bus;
using VAU.V320NeoNext.Runtime.Systems.FlightControl.SaccExt;

namespace VAU.V320NeoNext.Runtime.Systems.FlightControl
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public sealed class FlapControllerAvionicsBusAdapter : AbstractAvionicsBusClient
    {
        public DFUNC_a320_FlapController flapController;

        private const AvionicsBusIntDataIds FlapLevelerPositionId =
            AvionicsBusIntDataIds.V32NN_Infrequent_Flap_LevelerPosition;

        private const AvionicsBusIntDataIds FlapActualPositionId =
            AvionicsBusIntDataIds.V32NN_Infrequent_Flap_SFCC_1_ActualFlapPosition;

        private int _lastFlapLevelerPosition = -1;
        private int _lastFlapActualPosition = -1;

        private void Update()
        {
            if (_lastFlapLevelerPosition != flapController.targetDetentIndex)
            {
                _lastFlapLevelerPosition = flapController.targetDetentIndex;
                _WriteAndNotifyInt(FlapLevelerPositionId, flapController.targetDetentIndex);
            }

            if (flapController.detentIndex != -1 && _lastFlapActualPosition != flapController.detentIndex)
            {
                _lastFlapActualPosition = flapController.detentIndex;
                _WriteAndNotifyInt(FlapActualPositionId, flapController.detentIndex);
            }
        }
    }
}