using UdonSharp;
using UnityEngine;
using VAU.V320NeoNext.Runtime.InputSystem.SimplePilotInput.Abstract;

namespace VAU.V320NeoNext.Runtime.InputSystem.SimplePilotInput.InputProvider.AvionicsBusProvider
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [RequireComponent(typeof(SimplePilotInputAvionicsBusClient))]
    [DefaultExecutionOrder(AircraftScriptsExecutionOrder.SimplePilotInputProvider)]
    public sealed class SimplePilotInputAvionicsBusProvider : SimplePilotInputProvider
    {
        private SimplePilotInputAvionicsBusClient _avionicsBusClient;

        private void Start()
        {
            _avionicsBusClient = GetComponent<SimplePilotInputAvionicsBusClient>();
            _avionicsBusClient._SetInputProvider(this);
        }

        private void FixedUpdate()
        {
            _SetInputVector(_avionicsBusClient.GetInputFromAvionicsBus());
        }
    }
}