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
    /// 两类输入都由本类解读成**目标配平位置**这一个数值写到总线
    /// （<c>V32NN_Frequent_ElevatorTrim_TargetTrim</c>），不使用 pulse；
    /// 飞机系统只接收数值，不感知 hold 与按键。
    /// </para>
    /// <para>
    /// 目标配平位置在长按时每帧都变，因此只写不 Notify，由 Adapter 轮询；
    /// 配平显示值同理，由本类轮询读取。
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

        private const AvionicsBusBoolDataIds AutoTrimActiveRequestId =
            AvionicsBusBoolDataIds.V32NN_Infrequent_ElevatorTrim_Sync_AutoTrimActive;

        private const AvionicsBusFloatDataIds TrimRequestId =
            AvionicsBusFloatDataIds.V32NN_Frequent_ElevatorTrim_TargetTrim;

        private const AvionicsBusBoolDataIds AutoTrimActiveStatusId =
            AvionicsBusBoolDataIds.V32NN_Frequent_ElevatorTrim_AutoTrimActive;

        private const AvionicsBusFloatDataIds TrimPositionId =
            AvionicsBusFloatDataIds.V32NN_Frequent_ElevatorTrim_TrimPosition;

        private void Update()
        {
            UpdateStatus();
            UpdateHold();
        }

        private void UpdateStatus()
        {
            var autoTrim = _ReadBool(AutoTrimActiveStatusId);
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

            WriteTrimTarget(_pendingTrim);
        }

        private void WriteTrimTarget(float trim)
        {
            _pendingTrim = Mathf.Clamp(trim, -1f, 1f);
            _WriteFloat(TrimRequestId, _pendingTrim);
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

            WriteTrimTarget(_pendingTrim + step);
        }

        [PublicAPI]
        public void _ToggleAutoTrim()
        {
            _WriteAndNotifyBool(AutoTrimActiveRequestId, !autoTrimActive);
        }
    }
}
