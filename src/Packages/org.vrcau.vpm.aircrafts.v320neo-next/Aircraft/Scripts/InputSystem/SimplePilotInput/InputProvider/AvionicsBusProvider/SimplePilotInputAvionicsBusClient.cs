using UdonSharp;
using UnityEngine;
using VAU.V320NeoNext.Runtime.Bus;

namespace VAU.V320NeoNext.Runtime.InputSystem.SimplePilotInput.InputProvider.AvionicsBusProvider
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public sealed class SimplePilotInputAvionicsBusClient : AbstractAvionicsBusClient
    {
        private const AvionicsBusVector3DataIds InputId = AvionicsBusVector3DataIds.V32NN_Frequent_AutoPilot_Input;
        private const AvionicsBusBoolDataIds EngagedId = AvionicsBusBoolDataIds.V32NN_Infrequent_AutoPilot_Engaged;

        private bool _isInitialized;
        private SimplePilotInputAvionicsBusProvider _inputProvider;

        protected override void _OnAvionicsBusStart()
        {
            _SubscribeBool(EngagedId, nameof(_OnAutoPilotEngagedChanged));
        }

        public Vector3 GetInputFromAvionicsBus()
        {
            if (!_ReadBool(EngagedId)) return Vector3.zero;

            return _ReadVector3(InputId);
        }

        public void _OnAutoPilotEngagedChanged()
        {
            if (!_isInitialized) return;

            _inputProvider.overrideOthers = _ReadBool(EngagedId);
        }

        public void _SetInputProvider(SimplePilotInputAvionicsBusProvider provider)
        {
            _inputProvider = provider;
            _isInitialized = provider != null;
        }
    }
}