using JetBrains.Annotations;
using UdonSharp;
using VAU.V320NeoNext.Runtime.Systems.Seat;

namespace VAU.V320NeoNext.Runtime.InputSystem.FlightMenuController.Seat
{
    /// <summary>
    /// FlightMenu 的「Seat Adjust」菜单项与某个座位 <see cref="SeatAdjuster"/> 之间的转发器。
    /// <para>
    /// 菜单项通过 <c>holdContinuousEventName</c> 每帧发送自定义事件，本类只把事件按名字转发给
    /// <see cref="seatAdjuster"/>；自身不持有任何位移状态，也不依赖 AvionicsBus。
    /// 按住期间不需要 <c>holdStateVariableName</c>，因此本类没有 Update()。
    /// </para>
    /// <para>
    /// **每个座位一份实例**：菜单 prefab 里本类的 <c>seatAdjuster</c> 是空的，由宿主 prefab
    /// 对每个菜单实例做一次外层覆盖，指向该座位自己的 SeatAdjuster。
    /// 座位之间互不共享状态，所以不能全机共用一份菜单实例（否则所有座位都会去动被引用的那一个）。
    /// </para>
    /// <para>座位调整是每玩家本机行为，无 <c>_Sync_</c>、无 Sync 类。</para>
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public sealed class SeatAdjusterFlightMenuController : UdonSharpBehaviour
    {
        /// <summary>本菜单实例对应的座位调节器，由宿主 prefab 覆盖赋值。</summary>
        public SeatAdjuster seatAdjuster;

        /// <summary>菜单项 holdContinuousEventName（Move Up）。</summary>
        [PublicAPI]
        public void _OnHoldMoveUp()
        {
            if (!seatAdjuster) return;

            seatAdjuster._OnHoldMoveUp();
        }

        /// <summary>菜单项 holdContinuousEventName（Move Down）。</summary>
        [PublicAPI]
        public void _OnHoldMoveDown()
        {
            if (!seatAdjuster) return;

            seatAdjuster._OnHoldMoveDown();
        }

        /// <summary>菜单项 holdContinuousEventName（Move Forward）。</summary>
        [PublicAPI]
        public void _OnHoldMoveForward()
        {
            if (!seatAdjuster) return;

            seatAdjuster._OnHoldMoveForward();
        }

        /// <summary>菜单项 holdContinuousEventName（Move Backward）。</summary>
        [PublicAPI]
        public void _OnHoldMoveBackward()
        {
            if (!seatAdjuster) return;

            seatAdjuster._OnHoldMoveBackward();
        }
    }
}
