using UdonSharp;
using UnityEngine;
using VAU.V320NeoNext.Runtime.Bus;

namespace VAU.V320NeoNext.Runtime.InputSystem.SimplePilotInput.Actuator.AvionicsBus
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DisallowMultipleComponent]
    public class SimplePilotInputAvionicsBusActuatorClient : AbstractAvionicsBusClient
    {
        private const AvionicsBusVector3DataIds PilotInputDataId =
            AvionicsBusVector3DataIds.V32NN_Frequent_FlightControl_LocalPilotInput;

        public void _WritePilotInput(Vector3 input)
        {
            _WriteVector3(PilotInputDataId, input);
        }
    }
}