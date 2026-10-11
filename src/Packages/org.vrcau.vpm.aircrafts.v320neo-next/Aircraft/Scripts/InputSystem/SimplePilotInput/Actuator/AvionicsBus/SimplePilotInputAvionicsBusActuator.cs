using UdonSharp;
using UnityEngine;
using VAU.V320NeoNext.Runtime.InputSystem.SimplePilotInput.Abstract;

namespace VAU.V320NeoNext.Runtime.InputSystem.SimplePilotInput.Actuator.AvionicsBus
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DefaultExecutionOrder(AircraftScriptsExecutionOrder.SimplePilotInputWriteToAvionicsBusActuator)]
    [RequireComponent(typeof(SimplePilotInputAvionicsBusActuatorClient))]
    [DisallowMultipleComponent]
    public sealed class SimplePilotInputAvionicsBusActuator : SimplePilotInputActuator
    {
        private SimplePilotInputAvionicsBusActuatorClient _client;
        private bool _isInitialized;

        private void LazyStart()
        {
            if (_isInitialized) return;
            _client = GetComponent<SimplePilotInputAvionicsBusActuatorClient>();
            _isInitialized = _client != null;
        }

        protected override void ApplyPilotInput(Vector3 pilotInput)
        {
            LazyStart();
            if (!_isInitialized) return;

            _client._WritePilotInput(pilotInput);
        }
    }
}