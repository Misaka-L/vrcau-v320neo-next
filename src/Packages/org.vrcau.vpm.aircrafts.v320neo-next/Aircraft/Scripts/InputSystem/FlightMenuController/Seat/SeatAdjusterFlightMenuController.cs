using System;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VAU.V320NeoNext.Runtime.Bus;

namespace VAU.V320NeoNext.Runtime.InputSystem.FlightMenuController.Seat
{
    /// <summary>
    /// FlightMenu 与 SeatAdjuster 之间的 bridge。
    /// 只读写 AvionicsBus，不持有任何飞机系统引用。
    /// <para>
    /// 菜单的 Move Up/Down/Forward/Backward 是「按住移动」，由本类解读成
    /// **座位相对初始位置的偏移** 写到总线上的唯一一个变量
    /// （<c>V32NN_Frequent_Seat_Offset</c>）；<c>SeatAdjuster</c> 直接读这个变量，
    /// 不维护第二份状态，也不需要每帧回发布。
    /// </para>
    /// <para>座位调整是每玩家本机行为，无 <c>_Sync_</c>、无 Sync 类。</para>
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public sealed class SeatAdjusterFlightMenuController : AbstractAvionicsBusClient
    {
        // 菜单项 holdStateVariableName
        [NonSerialized] public bool holdToMoveUp;
        [NonSerialized] public bool holdToMoveDown;
        [NonSerialized] public bool holdToMoveForward;
        [NonSerialized] public bool holdToMoveBackward;

        /// <summary>按住时的移动速度（米/秒）。</summary>
        public float moveSpeed = 0.5f;

        private const AvionicsBusVector3DataIds OffsetId =
            AvionicsBusVector3DataIds.V32NN_Infrequent_Seat_Offset;

        private void Update()
        {
            var hasUp = holdToMoveUp;
            var hasDown = holdToMoveDown;
            var hasForward = holdToMoveForward;
            var hasBackward = holdToMoveBackward;

            if (!hasUp && !hasDown && !hasForward && !hasBackward) return;

            var direction = Vector3.zero;
            if (hasUp) direction += Vector3.up;
            if (hasDown) direction += Vector3.down;
            if (hasForward) direction += Vector3.forward;
            if (hasBackward) direction += Vector3.back;

            // 总线上的偏移量就是唯一状态，直接读它作为累加起点。
            // 只在按住时改变，所以走事件通知（SeatAdjuster 不需要 Update）。
            _WriteAndNotifyVector3(OffsetId, _ReadVector3(OffsetId) + direction * (moveSpeed * Time.deltaTime));
        }

        protected override void _OnAvionicsBusRespawnByLocalPlayer() => ResetOffset();

        protected override void _OnAvionicsBusRespawnByRemotePlayer() => ResetOffset();

        /// <summary>重生后座位回到初始位置，总线上的偏移量也要一起归零（本类是它唯一的写入方）。</summary>
        private void ResetOffset()
        {
            _WriteAndNotifyVector3(OffsetId, Vector3.zero);
        }
    }
}
