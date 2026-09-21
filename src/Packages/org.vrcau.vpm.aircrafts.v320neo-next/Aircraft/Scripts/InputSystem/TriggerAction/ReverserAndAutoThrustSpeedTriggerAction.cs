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
        public VRCPlayerApi.TrackingDataType trackingDataType = VRCPlayerApi.TrackingDataType.RightHand;

        public KeyCode keyboardControl = KeyCode.X;

        private float _lastTriggerReleasedAtTime;
        private bool _triggerPressedLastFrame;
        private bool _inAutoThrustMode;

        private VRCPlayerApi _localPlayer;
        private bool _playerInVr;
        private Transform _objectTransform;

        private void Start()
        {
            _localPlayer = Networking.LocalPlayer;
            _playerInVr = _localPlayer.IsUserInVR();
            _objectTransform = transform;
        }

        public override void PostLateUpdate()
        {
            var allowReverse = _ReadBool(AvionicsBusBoolDataIds.Sim_Frequent_Grounded);
            var allowStartReverse =
                allowReverse &&
                _ReadFloat(AvionicsBusFloatDataIds.V32NN_Frequent_Engine_Both_ThrustLever) < 0.38f;

            if (!_playerInVr)
            {
                HandleDesktopInput(allowReverse, allowStartReverse);
            }
            else
            {
                HandleVrInput(allowStartReverse);
            }
        }

        private void HandleVrInput(bool allowStartReverse)
        {
            var isTriggerPressed = Input.GetAxisRaw(triggerAxis) > 0.75f;
            if (isTriggerPressed)
            {
                if (Time.time - _lastTriggerReleasedAtTime < 0.4f) // Double tap -> A/THR Target speed mode
                {
                    _inAutoThrustMode = true;
                }

                if (_inAutoThrustMode)
                {
                    HandleAutoThrustInput();
                }
                else if (allowStartReverse)
                {
                    if (!GetReverserLeverOn()) SetReverserLever(true);
                }

                _triggerPressedLastFrame = true;
            }
            else
            {
                _inAutoThrustMode = false;
                _referenceSpeed = -1;
                if (GetReverserLeverOn()) SetReverserLever(false);

                if (_triggerPressedLastFrame)
                {
                    _lastTriggerReleasedAtTime = Time.time;
                    _triggerPressedLastFrame = false;
                }
            }
        }

        private int _referenceSpeed;
        private void HandleAutoThrustInput()
        {
            var selectedSpeed = _ReadInt(AvionicsBusIntDataIds.V32NN_Infrequent_FCU_Sync_SelectedAirspeedInKt);
            if (_referenceSpeed == -1) _referenceSpeed = selectedSpeed;

            var speedDiff = GetControllerMoveInput() * 250f;
            var newSpeedTarget = _referenceSpeed + Mathf.RoundToInt(speedDiff);
            newSpeedTarget = Mathf.Clamp(newSpeedTarget, 100, 399);
            _WriteAndNotifyInt(AvionicsBusIntDataIds.V32NN_Infrequent_FCU_Sync_SelectedAirspeedInKt, newSpeedTarget);
        }

        private Vector3 _referenceRelativePositionToSelf;

        private float GetControllerMoveInput()
        {
            var controllerTrackingData = _localPlayer.GetTrackingData(trackingDataType);
            var relativePositionToSelf = _objectTransform.InverseTransformDirection(
                _objectTransform.position - controllerTrackingData.position);

            if (!_triggerPressedLastFrame)
            {
                _referenceRelativePositionToSelf = relativePositionToSelf;
                return 0;
            }

            var input = relativePositionToSelf - _referenceRelativePositionToSelf;
            return input.z;
        }

        private void HandleDesktopInput(bool allowReverse, bool allowStartReverse)
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