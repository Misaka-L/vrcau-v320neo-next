using System;
using JetBrains.Annotations;
using UdonSharp;
using VAU.V320NeoNext.Runtime.Bus;
using VAU.V320NeoNext.Runtime.Systems.LegacyAutoBrake;

namespace VAU.V320NeoNext.Runtime.InputSystem.FlightMenuController.AutoBrake
{
    /// <summary>
    /// FlightMenu 与自动刹车（AutoBrake）之间的 bridge。
    /// 只读写 AvionicsBus，不持有任何飞机系统引用。
    /// <para>
    /// <c>V32NN_Infrequent_AutoBrake_Sync_Mode</c> 就是档位状态本身：
    /// 由 <c>AutoBrakeAvionicsBusSync</c> 做网络同步，AutoBrake 系统也直接读写同一个变量。
    /// 本类只负责把「按键选中哪一档（再按一次取消）」解读成目标档位这一个数值写进去，
    /// 并跟随该状态刷新菜单显示。
    /// </para>
    /// <para>总线编码：0=Off(None) 1=Low 2=Med 3=Max。</para>
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public sealed class AutoBrakeFlightMenuController : AbstractAvionicsBusClient
    {
        [NonSerialized] [PublicAPI] public bool isAutoBrakeLowSelected;
        [NonSerialized] [PublicAPI] public bool isAutoBrakeMedSelected;
        [NonSerialized] [PublicAPI] public bool isAutoBrakeMaxSelected;

        [NonSerialized] [PublicAPI] public bool isAutoBrakeArmOrWorking;

        [NonSerialized] [PublicAPI] public string autoBrakeStatusText;

        /// <summary>档位状态本身，由 AutoBrakeAvionicsBusSync 做网络同步。</summary>
        private const AvionicsBusIntDataIds ModeStateId =
            AvionicsBusIntDataIds.V32NN_Infrequent_AutoBrake_Sync_Mode;

        private const AvionicsBusBoolDataIds ActiveStatusId =
            AvionicsBusBoolDataIds.V32NN_Infrequent_AutoBrake_Active;

        private const AvionicsBusBoolDataIds ReachDecelTargetStatusId =
            AvionicsBusBoolDataIds.V32NN_Infrequent_AutoBrake_ReachDecelTarget;

        private AutoBrakeMode _currentMode;

        protected override void _OnAvionicsBusStart()
        {
            _SubscribeInt(ModeStateId, nameof(_OnAutoBrakeStatusChanged));
            _SubscribeBool(ActiveStatusId, nameof(_OnAutoBrakeStatusChanged));
            _SubscribeBool(ReachDecelTargetStatusId, nameof(_OnAutoBrakeStatusChanged));
            _OnAutoBrakeStatusChanged();
        }

        public void _OnAutoBrakeStatusChanged()
        {
            _currentMode = FromBusValue(_ReadInt(ModeStateId));

            isAutoBrakeLowSelected = _currentMode == AutoBrakeMode.Low;
            isAutoBrakeMedSelected = _currentMode == AutoBrakeMode.Med;
            isAutoBrakeMaxSelected = _currentMode == AutoBrakeMode.Max;

            isAutoBrakeArmOrWorking = _currentMode != AutoBrakeMode.None;

            string autoBrakeModeText;
            switch (_currentMode)
            {
                case AutoBrakeMode.None:
                    autoBrakeModeText = "OFF";
                    break;
                case AutoBrakeMode.Low:
                    autoBrakeModeText = "LOW";
                    break;
                case AutoBrakeMode.Med:
                    autoBrakeModeText = "MED";
                    break;
                case AutoBrakeMode.Max:
                    autoBrakeModeText = "MAX";
                    break;
                default:
                    autoBrakeModeText = _currentMode.ToString();
                    break;
            }

            var statusText = GetStatusText();
            autoBrakeStatusText = statusText + autoBrakeModeText;
        }

        private string GetStatusText()
        {
            if (_ReadBool(ActiveStatusId))
            {
                return _ReadBool(ReachDecelTargetStatusId) ? "DECEL\n" : "NO DECEL\n";
            }

            return _currentMode != AutoBrakeMode.None ? "ARM\n" : "";
        }

        /// <summary>
        /// 把「按键选中哪一档」解读成目标档位这一个数值写到总线；再按一次同一档位即取消。
        /// </summary>
        private void RequestMode(AutoBrakeMode mode)
        {
            _WriteAndNotifyInt(ModeStateId, ToBusValue(mode));
        }

        [PublicAPI]
        public void SelectAutoBrakeOff()
        {
            RequestMode(AutoBrakeMode.None);
        }

        [PublicAPI]
        public void SelectAutoBrakeLow()
        {
            RequestMode(_currentMode == AutoBrakeMode.Low ? AutoBrakeMode.None : AutoBrakeMode.Low);
        }

        [PublicAPI]
        public void SelectAutoBrakeMed()
        {
            RequestMode(_currentMode == AutoBrakeMode.Med ? AutoBrakeMode.None : AutoBrakeMode.Med);
        }

        [PublicAPI]
        public void SelectAutoBrakeMax()
        {
            RequestMode(_currentMode == AutoBrakeMode.Max ? AutoBrakeMode.None : AutoBrakeMode.Max);
        }

        // 总线编码：0=Off(None) 1=Low 2=Med 3=Max
        private int ToBusValue(AutoBrakeMode mode)
        {
            switch (mode)
            {
                case AutoBrakeMode.Low:
                    return 1;
                case AutoBrakeMode.Med:
                    return 2;
                case AutoBrakeMode.Max:
                    return 3;
                default:
                    return 0;
            }
        }

        private AutoBrakeMode FromBusValue(int value)
        {
            switch (value)
            {
                case 1:
                    return AutoBrakeMode.Low;
                case 2:
                    return AutoBrakeMode.Med;
                case 3:
                    return AutoBrakeMode.Max;
                default:
                    return AutoBrakeMode.None;
            }
        }
    }
}
