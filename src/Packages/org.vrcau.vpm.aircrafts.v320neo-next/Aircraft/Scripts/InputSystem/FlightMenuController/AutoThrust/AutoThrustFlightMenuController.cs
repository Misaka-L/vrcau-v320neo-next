using System;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VAU.V320NeoNext.Runtime.Bus;

namespace VAU.V320NeoNext.Runtime.InputSystem.FlightMenuController.AutoThrust
{
    /// <summary>
    /// FlightMenu 与自动推力（DFUNC_a320_AutoThrust）之间的 bridge。
    /// 只读写 AvionicsBus，不持有任何飞机系统引用。
    /// <para>
    /// 「+ / -」的长按由本类解读成**目标速度**这一个数值，直接写
    /// <c>V32NN_Infrequent_FCU_Sync_SelectedAirspeedInKt</c>（系统本来就读这个 id），不使用 pulse。
    /// 长按不依赖 holdStateVariableName：菜单项通过 holdContinuousEventName 在按住期间每帧
    /// 发送自定义事件（<see cref="_OnTargetSpeedHoldIncrease"/> / <see cref="_OnTargetSpeedHoldDecrease"/>），
    /// 因此本类不需要 Update()。
    /// </para>
    /// <para>
    /// Toggle A/THR 由本类解读成**目标接通状态** <c>V32NN_Infrequent_AutoThrust_Sync_Engage</c>：
    /// 是否需要预位/断开由系统决定（arm → 起飞后自动接通），bridge 只判断当前是否已预位或已在巡航。
    /// </para>
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public sealed class AutoThrustFlightMenuController : AbstractAvionicsBusClient
    {
        // 菜单项 isActivatedVariableName
        [NonSerialized] public bool Cruise;

        // 菜单项 titleVariableName（"A/THR\n<size=18>{0}kt</size>"）
        [NonSerialized] public int SetSpeed;

        /// <summary>长按时的目标速度变化速率（kt/s）。</summary>
        public float speedRatePerSecond = 10f;

        /// <summary>与 DFUNC_a320_AutoThrust.MinSpeedInKt / MaxSpeedInKt 对应。</summary>
        public int minSpeedInKt = 100;
        public int maxSpeedInKt = 399;

        private float _speedAccumulator;

        private const AvionicsBusBoolDataIds EngageRequestId =
            AvionicsBusBoolDataIds.V32NN_Infrequent_AutoThrust_EngageRequest;

        private const AvionicsBusBoolDataIds CruiseId =
            AvionicsBusBoolDataIds.V32NN_Infrequent_AutoThrust_Cruise;

        private const AvionicsBusBoolDataIds ArmedId =
            AvionicsBusBoolDataIds.V32NN_Infrequent_AutoThrust_Armed;

        private const AvionicsBusIntDataIds SelectedAirspeedId =
            AvionicsBusIntDataIds.V32NN_Infrequent_FCU_Sync_SelectedAirspeedInKt;

        private bool _armed;

        protected override void _OnAvionicsBusStart()
        {
            _SubscribeBool(CruiseId, nameof(_OnAutoThrustStatusChanged));
            _SubscribeBool(ArmedId, nameof(_OnAutoThrustStatusChanged));
            _SubscribeInt(SelectedAirspeedId, nameof(_OnAutoThrustStatusChanged));
            _OnAutoThrustStatusChanged();
        }

        public void _OnAutoThrustStatusChanged()
        {
            Cruise = _ReadBool(CruiseId);
            _armed = _ReadBool(ArmedId);
            SetSpeed = _ReadInt(SelectedAirspeedId);
        }

        /// <summary>菜单项 holdStartEventName（+ / - 共用）：丢弃上一次长按残留的累加量。</summary>
        public void _OnTargetSpeedHoldStart()
        {
            _speedAccumulator = 0f;
        }

        /// <summary>菜单项 holdContinuousEventName（+）：按住期间每帧上调目标速度。</summary>
        public void _OnTargetSpeedHoldIncrease()
        {
            ApplyHoldStep(1f);
        }

        /// <summary>菜单项 holdContinuousEventName（-）：按住期间每帧下调目标速度。</summary>
        public void _OnTargetSpeedHoldDecrease()
        {
            ApplyHoldStep(-1f);
        }

        /// <summary>
        /// 由 FlightMenu 的持续 hold 事件每帧调用，把 <see cref="speedRatePerSecond"/> 按
        /// <c>Time.deltaTime</c> 累加，攒够 1 kt 才写总线，保证结果与帧率无关。
        /// </summary>
        private void ApplyHoldStep(float direction)
        {
            _speedAccumulator += speedRatePerSecond * Time.deltaTime * direction;

            var deltaKt = Mathf.RoundToInt(_speedAccumulator);
            if (deltaKt == 0) return;

            _speedAccumulator -= deltaKt;
            SetSpeed = Mathf.Clamp(_ReadInt(SelectedAirspeedId) + deltaKt, minSpeedInKt, maxSpeedInKt);
            _WriteAndNotifyInt(SelectedAirspeedId, SetSpeed);
        }

        /// <summary>菜单项 triggerEventName：Toggle A/THR，写出目标接通状态。</summary>
        [PublicAPI]
        public void KeyboardInput()
        {
            _WriteAndNotifyBool(EngageRequestId, !(_armed || Cruise));
        }
    }
}
