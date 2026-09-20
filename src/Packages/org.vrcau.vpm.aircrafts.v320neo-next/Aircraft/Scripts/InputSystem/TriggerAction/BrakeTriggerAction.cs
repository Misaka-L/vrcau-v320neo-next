using UdonSharp;
using UnityEngine;
using VAU.V320NeoNext.Runtime.Bus;

namespace VAU.V320NeoNext.Runtime.InputSystem.TriggerAction
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class BrakeTriggerAction : AbstractAvionicsBusClient
    {
        public string triggerAxisName = "Oculus_CrossPlatform_PrimaryIndexTrigger";

        public KeyCode brakeKey = KeyCode.B;
        public KeyCode toggleParkBrakeKey = KeyCode.N;

        private void LateUpdate()
        {
            var vrInput = HandleVrControllerInput();
            var desktopInput = HandleDesktopBrakeInput();

            var finalBrakeInput = Mathf.Max(vrInput, desktopInput);
            _WriteFloat(AvionicsBusFloatDataIds.V32NN_Frequent_Brake_Sync_PedalInput, finalBrakeInput);
        }

        #region VR Controller

        private bool _triggeredLastFrame;
        private float _triggerHoldTime;

        private float HandleVrControllerInput()
        {
            var triggerInput = Input.GetAxisRaw(triggerAxisName);
            _triggerHoldTime += Time.deltaTime;

            if (triggerInput > 0.75f)
            {
                if (!_triggeredLastFrame)
                {
                    // Double tap
                    if (_triggerHoldTime < 0.4f)
                    {
                        _ToggleParkBrake();
                        _triggerHoldTime = 1;
                    }
                    // Non double tap
                    else
                    {
                        _triggerHoldTime = 0;
                    }
                }

                _triggeredLastFrame = true;
            }
            else
            {
                _triggeredLastFrame = false;
            }

            return triggerInput;
        }

        #endregion

        private float HandleDesktopBrakeInput()
        {
            if (Input.GetKeyDown(toggleParkBrakeKey))
            {
                _ToggleParkBrake();
            }

            if (Input.GetKey(brakeKey)) return 1f;
            return 0;
        }

        private void _ToggleParkBrake()
        {
            var isParkBrakeSet = _ReadBool(AvionicsBusBoolDataIds.V32NN_Infrequent_Brake_Sync_ParkBrakeSet);
            _WriteAndNotifyBool(AvionicsBusBoolDataIds.V32NN_Infrequent_Brake_Sync_ParkBrakeSet, !isParkBrakeSet);
        }
    }
}