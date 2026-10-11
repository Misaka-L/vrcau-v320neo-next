using UdonSharp;
using UnityEngine;
using VAU.V320NeoNext.Runtime.Bus;

namespace VAU.V320NeoNext.Runtime.Systems.FlightControl.Flybywire.Law
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DefaultExecutionOrder(AircraftScriptsExecutionOrder.FlightControlComputer)]
    public sealed partial class FlightControlLaw : AbstractAvionicsBusClient
    {
        private void FixedUpdate()
        {
            if (!_ReadBool(AvionicsBusBoolDataIds.Sim_Infrequent_HaveAircraftOwnership))
                return;

            var pilotInput = GetPilotInput();

            var pitchOutput = RunPitchLaw(pilotInput.x);
            var rollOutput = RunRollLaw(pilotInput.z);
            var yawOutput = RunYawLaw(pilotInput.y);

            _WriteVector3(
                AvionicsBusVector3DataIds.V32NN_Frequent_FlightControl_FinalPilotInputToSacc,
                new Vector3(pitchOutput, yawOutput, rollOutput)
            );
        }

        private Vector3 GetPilotInput()
        {
            if (_ReadBool(AvionicsBusBoolDataIds.V32NN_Infrequent_AutoPilot_Engaged))
                return _ReadVector3(AvionicsBusVector3DataIds.V32NN_Frequent_FlightControl_AutoPilotInput);

            return _ReadVector3(AvionicsBusVector3DataIds.V32NN_Frequent_FlightControl_LocalPilotInput);
        }
    }
}