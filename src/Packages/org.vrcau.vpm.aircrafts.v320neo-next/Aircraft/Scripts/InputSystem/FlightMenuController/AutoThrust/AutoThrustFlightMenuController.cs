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
    /// </para>
    /// <para>
    /// Toggle A/THR 由本类解读成**目标接通状态** <c>V32NN_Infrequent_AutoThrust_Sync_Engage</c>：
    /// 是否需要预位/断开由系统决定（arm → 起飞后自动接通），bridge 只判断当前是否已预位或已在巡航。
    /// </para>
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public sealed class AutoThrustFlightMenuController : AbstractAvionicsBusClient
    {
        // 菜单项 holdStateVariableName
        [NonSerialized] public bool holdIncreaseTargetSpeed;
        [NonSerialized] public bool holdDecreaseTargetSpeed;

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

        private void Update()
        {
            var isIncrease = holdIncreaseTargetSpeed;
            var isDecrease = holdDecreaseTargetSpeed;

            if (!isIncrease && !isDecrease)
            {
                _speedAccumulator = 0;
                return;
            }

            var direction = 0f;
            if (isIncrease) direction += 1f;
            if (isDecrease) direction -= 1f;

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
