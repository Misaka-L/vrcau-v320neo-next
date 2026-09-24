using UdonSharp;
using UnityEngine;
using VAU.V320NeoNext.Runtime.Bus;
using VAU.V320NeoNext.Runtime.Systems.FlightControl.SaccExt;

namespace VAU.V320NeoNext.Runtime.Systems.FlightControl
{
    /// <summary>
    /// 俯仰配平的 system ↔ bus 转换：
    /// 把 <c>V32NN_Frequent_ElevatorTrim_TargetTrim</c>（由 FlightMenu bridge 解读用户输入后写出）
    /// 应用到 <see cref="DFUNC_a320_ElevatorTrim"/>，并把配平状态发布到 status id。
    /// <para>
    /// 目标配平位置在长按时每帧都变，所以只写不 Notify、由本类轮询；
    /// 状态同样只写不 Notify，由 bridge 轮询。Auto Trim 目标状态很少变，仍走事件通知。
    /// </para>
    /// 网络同步由 <see cref="ElevatorTrimAvionicsBusSync"/>（Auto Trim）与系统自身的
    /// Continuous 同步（trim）负责，本类不做同步。
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public sealed class ElevatorTrimAvionicsBusAdapter : AbstractAvionicsBusClient
    {
        public DFUNC_a320_ElevatorTrim elevatorTrim;

        private const AvionicsBusBoolDataIds AutoTrimActiveRequestId =
            AvionicsBusBoolDataIds.V32NN_Infrequent_ElevatorTrim_Sync_AutoTrimActive;

        private const AvionicsBusFloatDataIds TrimRequestId =
            AvionicsBusFloatDataIds.V32NN_Frequent_ElevatorTrim_TargetTrim;

        private const AvionicsBusBoolDataIds AutoTrimActiveStatusId =
            AvionicsBusBoolDataIds.V32NN_Frequent_ElevatorTrim_AutoTrimActive;

        private const AvionicsBusFloatDataIds TrimPositionId =
            AvionicsBusFloatDataIds.V32NN_Frequent_ElevatorTrim_TrimPosition;

        private bool _lastAutoTrimActive, _hasPublished;
        private float _lastTrim;

        protected override void _OnAvionicsBusPostStart()
        {
            _SubscribeBool(AutoTrimActiveRequestId, nameof(_OnAutoTrimRequested));
        }

        /// <summary>命令只由飞机 owner 应用；trim 本身的网络同步由系统自己维护。</summary>
        public void _OnAutoTrimRequested()
        {
            if (!elevatorTrim) return;
            if (!_ReadBool(AvionicsBusBoolDataIds.Sim_Infrequent_HaveAircraftOwnership)) return;

            var requested = _ReadBool(AutoTrimActiveRequestId);
            if (requested != elevatorTrim.autoTrimActive) elevatorTrim._ToggleAutoTrim();
        }

        private void Update()
        {
            if (!elevatorTrim) return;

            // 目标配平位置：只是一个数值，直接写进系统（轮询，不走事件通知）
            if (_ReadBool(AvionicsBusBoolDataIds.Sim_Infrequent_HaveAircraftOwnership))
            {
                var requestedTrim = _ReadFloat(TrimRequestId);
                if (!Mathf.Approximately(requestedTrim, elevatorTrim.trim)) elevatorTrim.trim = requestedTrim;
            }

            var autoTrimActive = elevatorTrim.autoTrimActive;
            var trim = elevatorTrim.trim;

            if (_hasPublished && autoTrimActive == _lastAutoTrimActive && Mathf.Approximately(trim, _lastTrim))
                return;

            _hasPublished = true;
            _lastAutoTrimActive = autoTrimActive;
            _lastTrim = trim;

            // 只写不 Notify：bridge 轮询
            _WriteBool(AutoTrimActiveStatusId, autoTrimActive);
            _WriteFloat(TrimPositionId, trim);
        }
    }
}
