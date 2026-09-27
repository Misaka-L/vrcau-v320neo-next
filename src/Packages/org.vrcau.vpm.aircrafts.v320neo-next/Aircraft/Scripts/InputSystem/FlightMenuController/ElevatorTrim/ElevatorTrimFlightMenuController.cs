using System;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VAU.V320NeoNext.Runtime.Bus;

namespace VAU.V320NeoNext.Runtime.InputSystem.FlightMenuController.ElevatorTrim
{
    /// <summary>
    /// FlightMenu 与俯仰配平（DFUNC_a320_ElevatorTrim）之间的 bridge。
    /// 只读写 AvionicsBus，不持有任何飞机系统引用。
    /// <para>
    /// 菜单的 Pitch Up / Down 同时具备「单击步进」（triggerEventName = _TrimUp / _TrimDown）
    /// 与「长按连续配平」（holdStateVariableName = isTrimUpHold / isTrimDownHold）。
    /// 两类输入都由本类解读成**配平位置**这一个数值，直接写到总线上的配平状态
    /// （<c>V32NN_Frequent_ElevatorTrim_Sync_TrimPosition</c>），不使用 pulse；
    /// 飞机系统只接收数值，不感知 hold 与按键。
    /// </para>
    /// <para>
    /// 配平位置在长按时每帧都变，因此只写不 Notify，由系统与 BusSync 轮询；
    /// 配平显示值与 Auto Trim 状态同理，由本类轮询读取；两者的网络同步由
    /// ElevatorTrimAvionicsBusContinuousSync / ElevatorTrimAvionicsBusSync 负责。
    /// </para>
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public sealed class ElevatorTrimFlightMenuController : AbstractAvionicsBusClient
    {
        // 菜单项 holdStateVariableName
        [NonSerialized] public bool isTrimUpHold;
        [NonSerialized] public bool isTrimDownHold;

        // 菜单项 isActivatedVariableName
        [NonSerialized] [PublicAPI] public bool autoTrimActive;

        // 菜单项 titleVariableName
        [NonSerialized] public string trimDisplayString;

        /// <summary>单击一次 Pitch Up/Down 的配平变化量。</summary>
        public float trimStep = 0.02f;

        /// <summary>长按时的配平变化速率（每秒）。</summary>
        public float trimRatePerSecond = 0.2f;

        private float _pendingTrim;
        private bool _hasPendingTrim;

        private bool _hasReadStatus;
        private bool _lastAutoTrimActive;
        private float _lastTrim;

        /// <summary>Auto Trim 状态本身（总线即状态，读写同一个 id）。</summary>
        private const AvionicsBusBoolDataIds AutoTrimActiveId =
            AvionicsBusBoolDataIds.V32NN_Infrequent_ElevatorTrim_Sync_AutoTrimActive;

        /// <summary>配平位置本身（总线即状态，本类直接写、轮询读）。</summary>
        private const AvionicsBusFloatDataIds TrimPositionId =
            AvionicsBusFloatDataIds.V32NN_Frequent_ElevatorTrim_Sync_TrimPosition;

        private void Update()
        {
            UpdateStatus();
            UpdateHold();
        }

        private void UpdateStatus()
        {
            var autoTrim = _ReadBool(AutoTrimActiveId);
            var trim = _ReadFloat(TrimPositionId);

            if (_hasReadStatus && autoTrim == _lastAutoTrimActive && Mathf.Approximately(trim, _lastTrim)) return;

            _hasReadStatus = true;
            _lastAutoTrimActive = autoTrim;
            _lastTrim = trim;

            autoTrimActive = autoTrim;
            trimDisplayString = (trim > 0 ? "UP" : "DOWN") + " " + Mathf.Abs(trim).ToString("f2");
        }

        private void UpdateHold()
        {
            var isUp = isTrimUpHold;
            var isDown = isTrimDownHold;

            if (!isUp && !isDown)
            {
                // 松开后丢弃累加值：下次按下重新从系统当前配平位置起算
                _hasPendingTrim = false;
                return;
            }

            if (!_hasPendingTrim)
            {
                _pendingTrim = _ReadFloat(TrimPositionId);
                _hasPendingTrim = true;
            }

            var delta = trimRatePerSecond * Time.deltaTime;
            if (isUp) _pendingTrim += delta;
            if (isDown) _pendingTrim -= delta;

            WriteTrim(_pendingTrim);
        }

        /// <summary>
        /// 直接写总线上的配平状态（<c>V32NN_Frequent_ElevatorTrim_Sync_TrimPosition</c>）：
        /// 菜单不需要经过一层「目标配平位置」，飞机系统与 BusSync 都从这个变量上读。
        /// 只写不 Notify，消费方轮询。
        /// </summary>
        private void WriteTrim(float trim)
        {
            _pendingTrim = Mathf.Clamp(trim, -1f, 1f);
            _WriteFloat(TrimPositionId, _pendingTrim);
        }

        // 菜单项 triggerEventName（单击步进）
        [PublicAPI]
        public void _TrimUp()
        {
            AddTrimStep(trimStep);
        }

        [PublicAPI]
        public void _TrimDown()
        {
            AddTrimStep(-trimStep);
        }

        private void AddTrimStep(float step)
        {
            // 同一帧内长按已经起算时，步进叠加在累加值上，避免被长按的增量覆盖
            if (!_hasPendingTrim)
            {
                _pendingTrim = _ReadFloat(TrimPositionId);
                _hasPendingTrim = true;
            }

            WriteTrim(_pendingTrim + step);
        }

        [PublicAPI]
        public void _ToggleAutoTrim()
        {
            // 总线即状态：直接取反总线上的值，不依赖本类轮询出来的镜像
            _WriteAndNotifyBool(AutoTrimActiveId, !_ReadBool(AutoTrimActiveId));
        }
    }
}
