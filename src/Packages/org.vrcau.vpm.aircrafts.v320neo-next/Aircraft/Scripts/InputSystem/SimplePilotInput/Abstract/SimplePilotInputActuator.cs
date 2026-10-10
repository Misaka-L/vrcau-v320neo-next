using UdonSharp;
using UnityEngine;

namespace VAU.V320NeoNext.Runtime.InputSystem.SimplePilotInput.Abstract
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DefaultExecutionOrder(AircraftScriptsExecutionOrder.SimplePilotInputSaccActuator)]
    public abstract class SimplePilotInputActuator : UdonSharpBehaviour
    {
        public SimplePilotInputProvider inputProvider;

        private Vector3 _lastInputVector;

        private void LateUpdate()
        {
            var pilotInput = inputProvider.InputVector;
            _lastInputVector = pilotInput;
        }

        private void FixedUpdate()
        {
            ApplyPilotInput(_lastInputVector);
        }

        protected abstract void ApplyPilotInput(Vector3 pilotInput);
    }
}