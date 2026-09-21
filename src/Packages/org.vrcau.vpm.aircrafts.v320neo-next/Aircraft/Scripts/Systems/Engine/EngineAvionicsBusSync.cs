using UdonSharp;
using VAU.V320NeoNext.Runtime.Bus;
using VRC.SDKBase;

namespace VAU.V320NeoNext.Runtime.Systems.Engine
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class EngineAvionicsBusSync : AbstractAvionicsBusClient
    {
        [UdonSynced] private bool _reverser1LeverOn;
        [UdonSynced] private bool _reverser2LeverOn;

        private bool _applyingRemoteData;

        protected override void _OnAvionicsBusPostStart()
        {
            _SubscribeBool(
                AvionicsBusBoolDataIds.V32NN_Infrequent_Engine_Engine_1_Sync_ReverserLeverOn,
                nameof(_OnReverserLeverToggle));
            _SubscribeBool(
                AvionicsBusBoolDataIds.V32NN_Infrequent_Engine_Engine_2_Sync_ReverserLeverOn,
                nameof(_OnReverserLeverToggle));
        }

        public override void OnDeserialization()
        {
            _applyingRemoteData = true;

            _WriteAndNotifyBool(AvionicsBusBoolDataIds.V32NN_Infrequent_Engine_Engine_1_Sync_ReverserLeverOn,
                _reverser1LeverOn);
            _WriteAndNotifyBool(AvionicsBusBoolDataIds.V32NN_Infrequent_Engine_Engine_2_Sync_ReverserLeverOn,
                _reverser2LeverOn);

            _applyingRemoteData = false;
        }

        private void _OnReverserLeverToggle()
        {
            if (_applyingRemoteData) return;

            _reverser1LeverOn = _ReadBool(AvionicsBusBoolDataIds.V32NN_Infrequent_Engine_Engine_1_Sync_ReverserLeverOn);
            _reverser2LeverOn = _ReadBool(AvionicsBusBoolDataIds.V32NN_Infrequent_Engine_Engine_2_Sync_ReverserLeverOn);

            if (!Networking.IsOwner(gameObject)) Networking.SetOwner(Networking.LocalPlayer, gameObject);
            RequestSerialization();
        }
    }
}