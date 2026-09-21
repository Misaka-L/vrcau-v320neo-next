using UdonSharp;
using UnityEngine;
using VAU.V320NeoNext.Runtime.Bus;
using VRC.SDKBase;

namespace VAU.V320NeoNext.Runtime.Systems.AutoFlight
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public sealed class FcuAvionicsBusSync : AbstractAvionicsBusClient
    {
        [UdonSynced] private int _selectedAirSpeedInKt;

        private bool _isApplyingRemoteData;

        protected override void _OnAvionicsBusPostStart()
        {
            _SubscribeInt(AvionicsBusIntDataIds.V32NN_Infrequent_FCU_Sync_SelectedAirspeedInKt,
                nameof(_OnSubscribedValueChanged));
        }

        public void _OnSubscribedValueChanged()
        {
            Debug.Log(nameof(FcuAvionicsBusSync) + " " + nameof(_OnSubscribedValueChanged) + " called");
            if (_isApplyingRemoteData) return;
            TakeOwnership();
            _selectedAirSpeedInKt = _ReadInt(AvionicsBusIntDataIds.V32NN_Infrequent_FCU_Sync_SelectedAirspeedInKt);
            RequestSerialization();
        }

        public override void OnDeserialization()
        {
            Debug.Log(nameof(FcuAvionicsBusSync) + " " + nameof(OnDeserialization) + " called");
            _isApplyingRemoteData = true;
            _WriteAndNotifyInt(AvionicsBusIntDataIds.V32NN_Infrequent_FCU_Sync_SelectedAirspeedInKt, _selectedAirSpeedInKt);
            _isApplyingRemoteData = false;
        }

        private void TakeOwnership()
        {
            var isOwner = Networking.GetOwner(gameObject).isLocal;
            if (!isOwner)
            {
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
            }
        }
    }
}