using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace VAU.V320NeoNext.Runtime.Systems.Seat
{
    /// <summary>
    /// 座椅位置调节：把「菜单/按键的移动请求」累加成一个相对初始位置的偏移，
    /// 写回当前模式对应的目标 Transform（VR 用站位进入点，桌面用注视点目标）。
    /// <para>
    /// 本类**不依赖 AvionicsBus**，也不与任何其它座位共享状态：每个座位放一份，
    /// 由 <c>SeatAdjusterFlightMenuController</c>（或任何 SendCustomEvent 调用方）驱动自己这一份。
    /// </para>
    /// <para>
    /// 座位调整是每玩家本机行为：无 <c>_Sync_</c>、无网络同步，也不需要每帧轮询。
    /// 偏移只在菜单 hold 事件/步进调用时改变，所以本类没有 Update()。
    /// </para>
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public sealed class SeatAdjuster : UdonSharpBehaviour
    {
        /// <summary>VR 下被移动的目标（座位的站位进入点）。</summary>
        public Transform adjustTargetInVr;

        /// <summary>桌面模式下被移动的目标（座位的注视点目标）。</summary>
        public Transform adjustTargetInDesktop;

        /// <summary>每次 <c>StepMove*</c> 移动的距离（米）。</summary>
        public float adjustStep = 0.5f;

        /// <summary>按住移动时的速度（米/秒），菜单 hold 事件每帧调用一次，位移按 deltaTime 累计。</summary>
        public float moveSpeed = 0.5f;

        private bool _isInitialized;

        /// <summary>按本机是否 VR 选定的目标，在 <c>Start()</c> 决定一次。</summary>
        private Transform _activeTarget;

        private Vector3 _targetInitialLocalPosition;

        /// <summary>相对初始位置的偏移，本类唯一的位移状态。</summary>
        private Vector3 _offset;

        /// <summary>
        /// 本对象只在本地玩家位于该座位时才被启用（SaccVehicleSeat 的 EnableInSeat 门控），
        /// 所以这里记录的就是「玩家入座时」的目标初始位置。
        /// </summary>
        private void Start()
        {
            SetupActiveTarget();
        }

        private void SetupActiveTarget()
        {
            var isPlayerInVr = Networking.LocalPlayer.IsUserInVR();
            _activeTarget = isPlayerInVr ? adjustTargetInVr : adjustTargetInDesktop;

            if (!_activeTarget)
            {
                Debug.LogWarning($"[{nameof(SeatAdjuster)}] {name}: " +
                                 $"{(isPlayerInVr ? nameof(adjustTargetInVr) : nameof(adjustTargetInDesktop))} 未设置，座位调整已停用。");
                return;
            }

            _targetInitialLocalPosition = _activeTarget.localPosition;
            _isInitialized = true;

            ApplyOffset(Vector3.zero);
        }

        #region Hold Move (FlightMenu holdContinuousEventName)

        [PublicAPI]
        public void _OnHoldMoveUp() => ApplyHoldMove(Vector3.up);

        [PublicAPI]
        public void _OnHoldMoveDown() => ApplyHoldMove(Vector3.down);

        [PublicAPI]
        public void _OnHoldMoveForward() => ApplyHoldMove(Vector3.forward);

        [PublicAPI]
        public void _OnHoldMoveBackward() => ApplyHoldMove(Vector3.back);

        private void ApplyHoldMove(Vector3 direction)
        {
            if (!_isInitialized) return;

            ApplyOffset(_offset + direction * (moveSpeed * Time.deltaTime));
        }

        #endregion

        #region Step Move (按钮/按键等离散调用)

        [PublicAPI]
        public void StepMoveUp() => ApplyStepMove(Vector3.up);

        [PublicAPI]
        public void StepMoveDown() => ApplyStepMove(Vector3.down);

        [PublicAPI]
        public void StepMoveForward() => ApplyStepMove(Vector3.forward);

        [PublicAPI]
        public void StepMoveBackward() => ApplyStepMove(Vector3.back);

        private void ApplyStepMove(Vector3 direction)
        {
            if (!_isInitialized) return;

            ApplyOffset(_offset + direction * adjustStep);
        }

        #endregion

        /// <summary>
        /// 座位回到初始位置并清空偏移。
        /// <para>
        /// 本类**不自动调用**它（原来由 AvionicsBus 的重生事件触发）：是否在重生/离座/换座位时归零
        /// 由宿主决定，宿主自行 <c>SendCustomEvent(nameof(ResetAdjustment))</c> 即可。
        /// </para>
        /// </summary>
        [PublicAPI]
        public void ResetAdjustment()
        {
            if (!_isInitialized) return;

            ApplyOffset(Vector3.zero);
        }

        /// <summary>
        /// 偏移是本类唯一的位移状态：所有入口都只改 <see cref="_offset"/>，
        /// 再统一写回目标 Transform（避免步进与 hold 各自维护一份状态）。
        /// </summary>
        private void ApplyOffset(Vector3 offset)
        {
            _offset = offset;
            _activeTarget.localPosition = _targetInitialLocalPosition + _offset;
        }
    }
}
