using UdonSharp;
using UnityEngine;
using VAU.V320NeoNext.Runtime.InputSystem.SimplePilotInput.Abstract;

namespace VAU.V320NeoNext.Runtime.InputSystem.SimplePilotInput.InputProvider
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DefaultExecutionOrder(AircraftScriptsExecutionOrder.SimplePilotInputProvider)]
    public sealed class SimplePilotInputDesktopProvider : SimplePilotInputProvider
    {
        public KeyCode rollLeftKey = KeyCode.A;
        public KeyCode rollRightKey = KeyCode.D;

        public KeyCode pitchUpKey = KeyCode.W;
        public KeyCode pitchDownKey = KeyCode.S;

        public KeyCode yawLeftKey = KeyCode.Q;
        public KeyCode yawRightKey = KeyCode.E;

        private void Update()
        {
            var roll = KeyPressedToInt(rollRightKey, rollLeftKey);
            var pitch = KeyPressedToInt(pitchDownKey, pitchUpKey);
            var yaw = KeyPressedToInt(yawLeftKey, yawRightKey);

            _SetInputVector(new Vector3(pitch, yaw, roll));
        }

        private static int KeyPressedToInt(KeyCode negativeKey, KeyCode positiveKey)
        {
            return (Input.GetKey(negativeKey) ? -1 : 0) + (Input.GetKey(positiveKey) ? 1 : 0);
        }
    }
}