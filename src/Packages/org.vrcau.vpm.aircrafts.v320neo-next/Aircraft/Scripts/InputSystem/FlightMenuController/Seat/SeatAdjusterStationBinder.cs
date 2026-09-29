using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VAU.V320NeoNext.Runtime.Systems.Seat;

namespace VAU.V320NeoNext.Runtime.InputSystem.FlightMenuController.Seat
{
    /// <summary>
    /// 座位侧桥接：本地玩家进入本 GameObject 上的 <c>VRCStation</c> 时，
    /// 把本座位自己的 <see cref="SeatAdjuster"/> 指定为座椅调节菜单当前控制的对象。
    /// <para>
    /// 座椅调节菜单（<c>SeatAdjustFlightMenu.prefab</c>）全机只有一份实例，所以
    /// <see cref="SeatAdjusterFlightMenuController.seatAdjuster"/> 不能在 prefab 里写死成某个座位：
    /// 由挂在每个座位 station 上的本类在**本机玩家入座时**改写，
    /// 于是菜单实例可以全机共用，而座位偏移状态仍然是每个座位各一份（各自 <see cref="SeatAdjuster"/> 自己的）。
    /// </para>
    /// <para>
    /// VRCStation 只把 <c>OnStationEntered</c> 发给**同一个 GameObject** 上的 UdonBehaviour，
    /// 所以本组件必须挂在 station 根节点上（例：<c>Seats/Cockpit/SeatPilot</c>），
    /// 不能挂在 SeatAdjuster 子节点或它的父节点上。
    /// </para>
    /// <para>
    /// 座位调整是每玩家本机行为：只处理 <c>isLocal</c>，无 <c>_Sync_</c>、无网络同步。
    /// 只在入座时改写一次引用，因此本类没有 Update()，也不处理离座（下一个座位入座时自然改写）。
    /// </para>
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public sealed class SeatAdjusterStationBinder : UdonSharpBehaviour
    {
        /// <summary>全机共享的座椅调节菜单控制器（<c>EnableInVehicle/AvioncsFlightMenu/SeatAdjust/MenuController</c>）。</summary>
        public SeatAdjusterFlightMenuController menuController;

        /// <summary>本座位自己的调节器（<c>Seats/&lt;座位&gt;/InSeatOnlyPilot/SeatAdjuster</c>）。</summary>
        public SeatAdjuster seatAdjuster;

        public override void OnStationEntered(VRCPlayerApi player)
        {
            if (!Utilities.IsValid(player) || !player.isLocal) return;

            if (!menuController || !seatAdjuster)
            {
                Debug.LogWarning($"[{nameof(SeatAdjusterStationBinder)}] {name}: " +
                                 "menuController 或 seatAdjuster 未设置，座椅调节菜单仍指向原来的对象。");
                return;
            }

            menuController.seatAdjuster = seatAdjuster;
        }
    }
}