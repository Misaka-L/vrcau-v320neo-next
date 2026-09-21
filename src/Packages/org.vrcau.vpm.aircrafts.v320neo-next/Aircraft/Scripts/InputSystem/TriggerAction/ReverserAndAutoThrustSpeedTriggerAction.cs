using UdonSharp;
using UnityEngine;
using VAU.V320NeoNext.Runtime.Bus;
using VRC.SDKBase;

namespace VAU.V320NeoNext.Runtime.InputSystem.TriggerAction
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ReverserAndAutoThrustSpeedTriggerAction : AbstractAvionicsBusClient
    {
        public string triggerAxis = "Oculus_CrossPlatform_SecondaryIndexTrigger";

        public KeyCode keyboardControl = KeyCode.X;

        private float _lastTriggerReleasedAtTime;
        private bool _triggerPressedLastFrame;
        private bool _inAutoThrustMode;

        private bool _playerInVr;

        private void Start()
        {
            _playerInVr = Networking.LocalPlayer.IsUserInVR();
        }

        private void LateUpdate()
        {
            var allowReverse = _ReadBool(AvionicsBusBoolDataIds.Sim_Frequent_Grounded);
            var allowStartReverse =
                allowReverse &&
                _ReadFloat(AvionicsBusFloatDataIds.V32NN_Frequent_Engine_Both_ThrustLever) < 0.38f;

            if (!_playerInVr)
            {
                if (!allowReverse)
                {
                    if (GetReverserLeverOn()) SetReverserLever(false);
                }
                else if (Input.GetKeyDown(keyboardControl))
                {
                    if (GetReverserLeverOn())
                    {
                        SetReverserLever(false);
                    }
                    else if (allowStartReverse)
                    {
                        SetReverserLever(true);
                    }
                }
            }
            else
            {
                var isTriggerPressed = Input.GetAxisRaw(triggerAxis) > 0.75f;
                if (isTriggerPressed)
                {
                    _triggerPressedLastFrame = true;

                    if (Time.time - _lastTriggerReleasedAtTime < 0.4f) // Double tap -> A/THR Target speed mode
                    {
                        _inAutoThrustMode = true;
                    }

                    if (_inAutoThrustMode)
                    {
                        // TODO: A/THR Target Speed
                    }
                    else if (allowStartReverse)
                    {
                        if (!GetReverserLeverOn()) SetReverserLever(true);
                    }
                }
                else
                {
                    _inAutoThrustMode = false;
                    if (GetReverserLeverOn()) SetReverserLever(false);

                    if (_triggerPressedLastFrame)
                    {
                        _lastTriggerReleasedAtTime = Time.time;
                        _triggerPressedLastFrame = false;
                    }
                }
            }
        }

        private bool GetReverserLeverOn()
        {
            return
                _ReadBool(AvionicsBusBoolDataIds.V32NN_Infrequent_Engine_Engine_1_Sync_ReverserLeverOn) ||
                _ReadBool(AvionicsBusBoolDataIds.V32NN_Infrequent_Engine_Engine_2_Sync_ReverserLeverOn);
        }

        private void SetReverserLever(bool reverserOn)
        {
            _WriteAndNotifyBool(
                AvionicsBusBoolDataIds.V32NN_Infrequent_Engine_Engine_1_Sync_ReverserLeverOn, reverserOn);
            _WriteAndNotifyBool(
                AvionicsBusBoolDataIds.V32NN_Infrequent_Engine_Engine_2_Sync_ReverserLeverOn, reverserOn);
        }
    }
}