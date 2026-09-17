using System;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VAU.V320NeoNext.Runtime.Bus;
using VRC.SDKBase;

namespace VAU.V320NeoNext.Runtime.Systems.Seat
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public sealed class SeatAdjuster : AbstractAvionicsBusClient
    {
        public Transform adjustTargetInVr;
        public Transform adjustTargetInDesktop;

        public float adjustStep = 0.5f;

        private bool _isInitialized;

        private bool _isPlayerInVr;
        private Vector3 _targetInitialLocalPosition;

        [NonSerialized] [PublicAPI] public bool holdToMoveUp;
        [NonSerialized] [PublicAPI] public bool holdToMoveDown;
        [NonSerialized] [PublicAPI] public bool holdToMoveBackward;
        [NonSerialized] [PublicAPI] public bool holdToMoveForward;

        private void Update()
        {
            // Handle Hold To Move Logic
            if (!holdToMoveDown && !holdToMoveBackward && !holdToMoveForward && !holdToMoveUp) return;
            LazyStart();

            var moveOffset = Vector3.zero;
            if (holdToMoveUp) moveOffset += Vector3.up;
            if (holdToMoveDown) moveOffset += Vector3.down;
            if (holdToMoveForward) moveOffset += Vector3.forward;
            if (holdToMoveBackward) moveOffset += Vector3.back;

            var moveTarget = GetTargetLocalPosition() + moveOffset * (adjustStep * Time.deltaTime);

            SetTargetLocalPosition(moveTarget);
        }

        protected override void _OnAvionicsBusRespawnByLocalPlayer() => ResetTargetPosition();
        protected override void _OnAvionicsBusRespawnByRemotePlayer() => ResetTargetPosition();

        private void ResetTargetPosition()
        {
            if (!_isInitialized) return;
            SetTargetLocalPosition(_targetInitialLocalPosition);
        }

        #region Step Move Menu Event

        [PublicAPI]
        public void StepMoveUp()
        {
            LazyStart();
            var target = GetTargetLocalPosition() + Vector3.up * adjustStep;
            SetTargetLocalPosition(target);
        }

        [PublicAPI]
        public void StepMoveDown()
        {
            LazyStart();
            var target = GetTargetLocalPosition() + Vector3.down  * adjustStep;
            SetTargetLocalPosition(target);
        }
        [PublicAPI]
        public void StepMoveBackward()
        {
            LazyStart();
            var target = GetTargetLocalPosition() + Vector3.back * adjustStep;
            SetTargetLocalPosition(target);
        }

        [PublicAPI]
        public void StepMoveForward()
        {
            LazyStart();
            var target = GetTargetLocalPosition() + Vector3.forward * adjustStep;
            SetTargetLocalPosition(target);
        }

        #endregion

        private Vector3 GetTargetLocalPosition()
        {
            return _isPlayerInVr ? adjustTargetInVr.localPosition : adjustTargetInDesktop.localPosition;
        }

        private void SetTargetLocalPosition(Vector3 position)
        {
            if (_isPlayerInVr)
            {
                adjustTargetInVr.localPosition = position;
            }
            else
            {
                adjustTargetInDesktop.localPosition = position;
            }
        }

        private void LazyStart()
        {
            if (_isInitialized) return;

            _isPlayerInVr = Networking.LocalPlayer.IsUserInVR();
            _targetInitialLocalPosition =
                _isPlayerInVr ? adjustTargetInVr.localPosition : adjustTargetInDesktop.localPosition;

            _isInitialized = true;
        }
    }
}