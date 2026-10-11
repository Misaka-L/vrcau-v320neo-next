using UdonSharp;
using UnityEngine;
using VAU.V320NeoNext.Runtime.InputSystem.SimplePilotInput.Abstract;

namespace VAU.V320NeoNext.Runtime.InputSystem.SimplePilotInput.InputProvider
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DefaultExecutionOrder(AircraftScriptsExecutionOrder.SimplePilotInputMixer)]
    public sealed class SimplePilotInput3MixedProvider : SimplePilotInputProvider
    {
        public SimplePilotInputVrProvider vrProvider;
        public SimplePilotInputDesktopProvider desktopProvider;

        private void FixedUpdate()
        {
            var mixedRaw = vrProvider.InputVector + desktopProvider.InputVector;

            _SetInputVector(new Vector3(
                Mathf.Clamp(mixedRaw.x, -1f, 1f),
                Mathf.Clamp(mixedRaw.y, -1f, 1f),
                Mathf.Clamp(mixedRaw.z, -1f, 1f)
            ));
        }
    }
}