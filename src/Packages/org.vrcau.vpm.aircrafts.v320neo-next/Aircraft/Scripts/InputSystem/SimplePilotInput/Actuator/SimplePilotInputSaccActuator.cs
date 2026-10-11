using SaccFlightAndVehicles;
using UdonSharp;
using UnityEngine;
using VAU.V320NeoNext.Runtime.InputSystem.SimplePilotInput.Abstract;

namespace VAU.V320NeoNext.Runtime.InputSystem.SimplePilotInput.Actuator
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public sealed class SimplePilotInputSaccActuator : SimplePilotInputActuator
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

        protected override void ApplyPilotInput(Vector3 pilotInput)
        {
            saccAirVehicle.JoystickOverride = pilotInput;
        }
    }
}