using System;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VAU.V320NeoNext.Runtime.Bus;

namespace VAU.V320NeoNext.Runtime.InputSystem.FlightMenuController.LandingGear
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public sealed class LandingGearFlightMenuController : AbstractAvionicsBusClient
    {
        public KeyCode toggleGearKey = KeyCode.G;

        [NonSerialized] [PublicAPI] public bool GearLeverUp;

        private void LateUpdate()
        {
            if (Input.GetKeyDown(toggleGearKey)) _ToggleGearUp();
            GearLeverUp = _ReadBool(AvionicsBusBoolDataIds.V32NN_Infrequent_LandingGear_Sync_GearLeverUp);
        }

        [PublicAPI]
        public void _ToggleGearUp()
        {
            _WriteAndNotifyBool(
                AvionicsBusBoolDataIds.V32NN_Infrequent_LandingGear_Sync_GearLeverUp,
                !_ReadBool(AvionicsBusBoolDataIds.V32NN_Infrequent_LandingGear_Sync_GearLeverUp));
        }
    }
}