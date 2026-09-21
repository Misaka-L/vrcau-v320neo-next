using System;
using JetBrains.Annotations;
using UdonSharp;
using VAU.V320NeoNext.Runtime.Bus;

namespace VAU.V320NeoNext.Runtime.InputSystem.FlightMenuController.Brake
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public sealed class BrakeFlightMenuController : AbstractAvionicsBusClient
    {
        [PublicAPI] [NonSerialized] public bool isParkBrakeSet;

        protected override void _OnAvionicsBusStart()
        {
            _SubscribeBool(
                AvionicsBusBoolDataIds.V32NN_Infrequent_Brake_Sync_ParkBrakeSet, nameof(_OnParkBrakeSetChanged));
            _OnParkBrakeSetChanged();
        }

        public void _OnParkBrakeSetChanged()
        {
            isParkBrakeSet = _ReadBool(AvionicsBusBoolDataIds.V32NN_Infrequent_Brake_Sync_ParkBrakeSet);
        }

        [PublicAPI]
        public void _ToggleParkBrake()
        {
            _WriteAndNotifyBool(AvionicsBusBoolDataIds.V32NN_Infrequent_Brake_Sync_ParkBrakeSet, !isParkBrakeSet);
        }
    }
}