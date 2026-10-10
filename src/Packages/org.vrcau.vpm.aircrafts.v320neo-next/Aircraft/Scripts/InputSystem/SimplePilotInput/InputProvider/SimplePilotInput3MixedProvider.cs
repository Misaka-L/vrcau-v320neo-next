using UdonSharp;
using UnityEngine;
using VAU.V320NeoNext.Runtime.InputSystem.SimplePilotInput.Abstract;
using VAU.V320NeoNext.Runtime.InputSystem.SimplePilotInput.InputProvider.AvionicsBusProvider;

namespace VAU.V320NeoNext.Runtime.InputSystem.SimplePilotInput.InputProvider
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DefaultExecutionOrder(AircraftScriptsExecutionOrder.SimplePilotInputMixer)]
    public sealed class SimplePilotInput3MixedProvider : SimplePilotInputProvider
    {
        public SimplePilotInputVrProvider vrProvider;
        public SimplePilotInputDesktopProvider desktopProvider;
        public SimplePilotInputAvionicsBusProvider autoPilotProvider;

        private void FixedUpdate()
        {
            if (autoPilotProvider.overrideOthers)
            {
                _SetInputVector(autoPilotProvider.InputVector);
                return;
            }

            var mixedRaw = vrProvider.InputVector + desktopProvider.InputVector;

            _SetInputVector(new Vector3(
                Mathf.Clamp(mixedRaw.x, -1f, 1f),
                Mathf.Clamp(mixedRaw.y, -1f, 1f),
                Mathf.Clamp(mixedRaw.z, -1f, 1f)
            ));
        }
    }
}