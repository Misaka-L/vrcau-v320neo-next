using UdonSharp;
using VAU.V320NeoNext.Runtime.Bus;
using VRC.SDKBase;

namespace VAU.V320NeoNext.Runtime.Systems.LandingGear.Brake
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
    public sealed class BrakeAvionicsBusContinuousSync : AbstractAvionicsBusClient
    {
        [UdonSynced] private float _brakePedalInput;
        [UdonSynced] private float _autoBrakeInput;

        protected override void _OnAvionicsBusPostStart()
        {
            _SubscribeBool(
                AvionicsBusBoolDataIds.Sim_Infrequent_HaveAircraftOwnership,
                nameof(_OnHaveAircraftOwnershipChanged));
        }

        public void _OnHaveAircraftOwnershipChanged()
        {
            var isAircraftOwner = _ReadBool(AvionicsBusBoolDataIds.Sim_Infrequent_HaveAircraftOwnership);
            if (isAircraftOwner) TakeOwnership();
            enabled = isAircraftOwner;
        }

        private void LateUpdate()
        {
            var isAircraftOwner = _ReadBool(AvionicsBusBoolDataIds.Sim_Infrequent_HaveAircraftOwnership);
            if (!isAircraftOwner)
            {
                enabled = false;
                return;
            }

            _brakePedalInput = _ReadFloat(AvionicsBusFloatDataIds.V32NN_Frequent_Brake_Sync_PedalInput);
            _autoBrakeInput = _ReadFloat(AvionicsBusFloatDataIds.V32NN_Frequent_Brake_Sync_AutoBrakeInput);
        }

        public override void OnDeserialization()
        {
            _WriteFloat(AvionicsBusFloatDataIds.V32NN_Frequent_Brake_Sync_PedalInput, _brakePedalInput);
            _WriteFloat(AvionicsBusFloatDataIds.V32NN_Frequent_Brake_Sync_AutoBrakeInput, _autoBrakeInput);
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