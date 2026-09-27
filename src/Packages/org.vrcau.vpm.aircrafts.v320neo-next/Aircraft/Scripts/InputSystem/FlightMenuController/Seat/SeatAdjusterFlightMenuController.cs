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
    /// （<c>V32NN_Infrequent_Seat_Offset</c>）；<c>SeatAdjuster</c> 直接读这个变量，
    /// 不维护第二份状态，也不需要每帧回发布。
    /// </para>
    /// <para>
    /// 按住期间由菜单项通过 holdContinuousEventName 每帧发送自定义事件，本类不依赖
    /// holdStateVariableName，因此不需要 Update()。
    /// </para>
    /// <para>座位调整是每玩家本机行为，无 <c>_Sync_</c>、无 Sync 类。</para>
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public sealed class SeatAdjusterFlightMenuController : AbstractAvionicsBusClient
    {
        /// <summary>按住时的移动速度（米/秒）。</summary>
        public float moveSpeed = 0.5f;

        private const AvionicsBusVector3DataIds OffsetId =
            AvionicsBusVector3DataIds.V32NN_Infrequent_Seat_Offset;

        /// <summary>菜单项 holdContinuousEventName（Move Up）。</summary>
        public void _OnHoldMoveUp()
        {
            ApplyHoldMove(Vector3.up);
        }

        /// <summary>菜单项 holdContinuousEventName（Move Down）。</summary>
        public void _OnHoldMoveDown()
        {
            ApplyHoldMove(Vector3.down);
        }

        /// <summary>菜单项 holdContinuousEventName（Move Forward）。</summary>
        public void _OnHoldMoveForward()
        {
            ApplyHoldMove(Vector3.forward);
        }

        /// <summary>菜单项 holdContinuousEventName（Move Backward）。</summary>
        public void _OnHoldMoveBackward()
        {
            ApplyHoldMove(Vector3.back);
        }

        /// <summary>
        /// 由 FlightMenu 的持续 hold 事件每帧调用：总线上的偏移量就是唯一状态，
        /// 直接读它作为累加起点；只在按住时改变，所以走事件通知（SeatAdjuster 订阅 OffsetId）。
        /// </summary>
        private void ApplyHoldMove(Vector3 direction)
        {
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
