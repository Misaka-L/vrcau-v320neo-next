using JetBrains.Annotations;
using SaccFlightAndVehicles;
using UdonSharp;
using VRC.SDKBase;

namespace VAU.V320NeoNext.Runtime.Bus.Adapter
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public sealed class AvionicsBusSaccEntityAdapter : AbstractAvionicsBusClient
    {
        // ReSharper disable once InconsistentNaming
        public SaccEntity EntityControl;

        [PublicAPI]
        public void SFEXT_L_EntityStart() => UpdateOwnershipStatus();

        [PublicAPI]
        public void SFEXT_O_LoseOwnership() => UpdateOwnershipStatus();

        [PublicAPI]
        public void SFEXT_L_OwnershipTransfer() => UpdateOwnershipStatus();

        [PublicAPI]
        public void SFEXT_G_PilotEnter()
        {
            _WriteAndNotifyBool(AvionicsBusBoolDataIds.Sim_Infrequent_HasPilot, true);
            UpdateOwnershipStatus();
        }

        [PublicAPI]
        public void SFEXT_G_PilotExit()
        {
            _WriteAndNotifyBool(AvionicsBusBoolDataIds.Sim_Infrequent_HasPilot, false);
            UpdateOwnershipStatus();
        }

        private void UpdateOwnershipStatus()
        {
            _WriteAndNotifyBool(AvionicsBusBoolDataIds.Sim_Infrequent_HaveAircraftOwnership, EntityControl.IsOwner);
            _WriteAndNotifyBool(AvionicsBusBoolDataIds.Sim_Infrequent_IsPilot, EntityControl.Piloting);
        }

        [PublicAPI]
        public void SFEXT_G_RespawnButton()
        {
            if (Networking.IsOwner(EntityControl.gameObject))
            {
                _avionicsBus._AvionicsBusRespawn_RunInOwner();
            }
            else
            {
                _avionicsBus._AvionicsBusRespawn_RunInRemote();
            }
        }
    }
}