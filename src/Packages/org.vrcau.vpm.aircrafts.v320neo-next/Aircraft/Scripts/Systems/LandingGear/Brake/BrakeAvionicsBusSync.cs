using UdonSharp;
using VAU.V320NeoNext.Runtime.Bus;
using VRC.SDKBase;

namespace VAU.V320NeoNext.Runtime.Systems.LandingGear.Brake
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public sealed class BrakeAvionicsBusSync : AbstractAvionicsBusClient
    {
        [UdonSynced] private bool _isParkBrakeSet;
        private bool _applyingRemoteData;

        private const AvionicsBusBoolDataIds ParkBrakeSetId =
            AvionicsBusBoolDataIds.V32NN_Infrequent_Brake_Sync_ParkBrakeSet;

        protected override void _OnAvionicsBusPostStart()
        {
            _SubscribeBool(ParkBrakeSetId, nameof(_OnParkBrakeSetChanged));
        }

        private void _OnParkBrakeSetChanged()
        {
            if (_applyingRemoteData) return;
            if (Networking.IsOwner(gameObject)) Networking.SetOwner(Networking.LocalPlayer, gameObject);

            _isParkBrakeSet = _ReadBool(ParkBrakeSetId);
            RequestSerialization();
        }

        public override void OnDeserialization()
        {
            _applyingRemoteData = true;
            _WriteAndNotifyBool(ParkBrakeSetId, _isParkBrakeSet);
            _applyingRemoteData = false;
        }
    }
}