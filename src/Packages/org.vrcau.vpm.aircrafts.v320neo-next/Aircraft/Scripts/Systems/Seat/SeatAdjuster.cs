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

        /// <summary>每次 <c>StepMove*</c> 移动的距离（米）。</summary>
        public float adjustStep = 0.5f;

        private bool _isInitialized;

        private bool _isPlayerInVr;
        private Vector3 _targetInitialLocalPosition;

        private Vector3 _lastAppliedOffset;
        private bool _hasAppliedOffset;

        private const AvionicsBusVector3DataIds OffsetId =
            AvionicsBusVector3DataIds.V32NN_Infrequent_Seat_Offset;

        /// <summary>
        /// 座位偏移量由 <c>SeatAdjusterFlightMenuController</c> 解读「长按移动」后写到
        /// <c>V32NN_Infrequent_Seat_Offset</c>，本系统直接跟随这个值
        /// （该值只在按住时改变，所以走事件通知，本类不需要 Update）。
        /// <para>座位调整是每玩家本机行为，只有按下菜单的客户端会写自己的本地总线。</para>
        /// </summary>
        protected override void _OnAvionicsBusPostStart()
        {
            _SubscribeVector3(OffsetId, nameof(_OnOffsetChanged));
        }

        public void _OnOffsetChanged()
        {
            var offset = _ReadVector3(OffsetId);

            if (_hasAppliedOffset && offset == _lastAppliedOffset) return;

            _hasAppliedOffset = true;
            _lastAppliedOffset = offset;

            LazyStart();
            SetTargetLocalPosition(_targetInitialLocalPosition + offset);
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