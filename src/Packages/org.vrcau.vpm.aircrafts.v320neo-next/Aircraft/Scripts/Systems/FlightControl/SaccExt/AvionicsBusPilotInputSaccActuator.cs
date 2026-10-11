using SaccFlightAndVehicles;
using UdonSharp;
using UnityEngine;
using VAU.V320NeoNext.Runtime.Bus;

namespace VAU.V320NeoNext.Runtime.Systems.FlightControl.SaccExt
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DefaultExecutionOrder(AircraftScriptsExecutionOrder.SaccJoystickOverridenActuator)]
    public sealed class AvionicsBusPilotInputSaccActuator : AbstractAvionicsBusClient
    {
        public SaccAirVehicle saccAirVehicle;

        private void OnEnable()
        {
            saccAirVehicle.JoystickOverridden_++;
            saccAirVehicle.DisableJoystickControl_++;
        }

        private void OnDisable()
        {
            saccAirVehicle.JoystickOverridden_--;
            saccAirVehicle.DisableJoystickControl_--;
        }

        private void FixedUpdate()
        {
            if (!_ReadBool(AvionicsBusBoolDataIds.Sim_Infrequent_HaveAircraftOwnership))
                return;

            saccAirVehicle.JoystickOverride =
                _ReadVector3(AvionicsBusVector3DataIds.V32NN_Frequent_FlightControl_FinalPilotInputToSacc);
        }
    }
}