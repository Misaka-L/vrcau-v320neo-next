using System;
using JetBrains.Annotations;
using UdonSharp;
using VAU.V320NeoNext.Runtime.Bus;

namespace VAU.V320NeoNext.Runtime.InputSystem.FlightMenuController.AutoStart
{
    /// <summary>
    /// FlightMenu 与 Auto Start（A320AutoStarter）之间的 bridge。
    /// 只读写 AvionicsBus，不持有任何飞机系统引用。
    /// <para>
    /// <c>V32NN_Infrequent_AutoStart_Sync_Start</c> 就是「是否已请求启动」的状态本身：
    /// 由 <c>AutoStartAvionicsBusSync</c> 做网络同步，A320AutoStarter 也直接读写同一个变量，
    /// 因此不需要 adapter、也没有两套状态。
    /// </para>
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public sealed class AutoStartFlightMenuController : AbstractAvionicsBusClient
    {
        /// <summary>菜单项 isActivatedVariableName。</summary>
        [NonSerialized] [PublicAPI] public bool isIndicatorActivated;

        private const AvionicsBusBoolDataIds StartId =
            AvionicsBusBoolDataIds.V32NN_Infrequent_AutoStart_Sync_Start;

        private const AvionicsBusBoolDataIds IndicatorActivatedId =
            AvionicsBusBoolDataIds.V32NN_Infrequent_AutoStart_IndicatorActivated;

        protected override void _OnAvionicsBusStart()
        {
            _SubscribeBool(IndicatorActivatedId, nameof(_OnIndicatorActivatedChanged));
            _OnIndicatorActivatedChanged();
        }

        public void _OnIndicatorActivatedChanged()
        {
            isIndicatorActivated = _ReadBool(IndicatorActivatedId);
        }

        /// <summary>菜单项 triggerEventName：翻转总线上的启动状态。</summary>
        [PublicAPI]
        public void _ToggleStart()
        {
            _WriteAndNotifyBool(StartId, !_ReadBool(StartId));
        }
    }
}
