using UdonSharp;
using VAU.V320NeoNext.Runtime.Bus;
using VRC.SDKBase;

namespace VAU.V320NeoNext.Runtime.Systems.LandingGear
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public sealed class LandingGearAvionicsBusSync : AbstractAvionicsBusClient
    {
        [UdonSynced] private bool _gearLeverUp;

        private bool _applyingRemoteData;

        protected override void _OnAvionicsBusPostStart()
        {
            _SubscribeBool(
                AvionicsBusBoolDataIds.V32NN_Infrequent_LandingGear_Sync_GearLeverUp,
                nameof(_OnSubscribedValuesChanged));
        }

        public void _OnSubscribedValuesChanged()
        {
            if (_applyingRemoteData) return;
            if (!Networking.GetOwner(gameObject).isLocal) Networking.SetOwner(Networking.LocalPlayer, gameObject);

            _gearLeverUp = _ReadBool(AvionicsBusBoolDataIds.V32NN_Infrequent_LandingGear_Sync_GearLeverUp);
            RequestSerialization();
        }

        public override void OnDeserialization()
        {
            _applyingRemoteData = true;
            _WriteAndNotifyBool(AvionicsBusBoolDataIds.V32NN_Infrequent_LandingGear_Sync_GearLeverUp, _gearLeverUp);
            _applyingRemoteData = false;
        }
    }
}