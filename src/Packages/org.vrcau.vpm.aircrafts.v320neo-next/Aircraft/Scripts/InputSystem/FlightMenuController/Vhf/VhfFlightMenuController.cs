using System;
using JetBrains.Annotations;
using UdonSharp;
using VAU.V320NeoNext.Runtime.Bus;

namespace VAU.V320NeoNext.Runtime.InputSystem.FlightMenuController.Vhf
{
    /// <summary>
    /// FlightMenu 与 VHF 收发机（SFEXT_URC_VHF）之间的 bridge。
    /// 只读写 AvionicsBus，不持有任何飞机系统引用。
    /// <para>频率输入弹窗（ActiveFrequency）仍由 integration 包的 FlightMenuRadioController 负责。</para>
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public sealed class VhfFlightMenuController : AbstractAvionicsBusClient
    {
        [NonSerialized] public bool rxPower;
        [NonSerialized] public bool txPower;

        [NonSerialized] [PublicAPI] public bool isVhfRxActivated;

        [NonSerialized] public string activeFrequencyText;
        [NonSerialized] public string vhfStatusOverviewText;

        private const AvionicsBusBoolDataIds RxRequestId =
            AvionicsBusBoolDataIds.V32NN_Infrequent_VHF_RequestedRxOn;

        private const AvionicsBusBoolDataIds TxRequestId =
            AvionicsBusBoolDataIds.V32NN_Infrequent_VHF_RequestedTxOn;

        private const AvionicsBusBoolDataIds RxPowerId =
            AvionicsBusBoolDataIds.V32NN_Infrequent_VHF_RxPower;

        private const AvionicsBusBoolDataIds TxPowerId =
            AvionicsBusBoolDataIds.V32NN_Infrequent_VHF_TxPower;

        private const AvionicsBusIntDataIds ChannelKhzId =
            AvionicsBusIntDataIds.V32NN_Infrequent_VHF_ChannelKhz;

        protected override void _OnAvionicsBusStart()
        {
            _SubscribeBool(RxPowerId, nameof(_OnVhfStatusChanged));
            _SubscribeBool(TxPowerId, nameof(_OnVhfStatusChanged));
            _SubscribeInt(ChannelKhzId, nameof(_OnVhfStatusChanged));
            _OnVhfStatusChanged();
        }

        public void _OnVhfStatusChanged()
        {
            rxPower = _ReadBool(RxPowerId);
            txPower = _ReadBool(TxPowerId);
            isVhfRxActivated = rxPower;

            activeFrequencyText = (_ReadInt(ChannelKhzId) * 0.001).ToString("000.000");

            var status = txPower ? "TX" : rxPower ? "RX" : "OFF";
            vhfStatusOverviewText = status + "\n" + activeFrequencyText;
        }

        [PublicAPI]
        public void ToggleRx()
        {
            _WriteAndNotifyBool(RxRequestId, !rxPower);
        }

        [PublicAPI]
        public void ToggleTx()
        {
            _WriteAndNotifyBool(TxRequestId, !txPower);
        }
    }
}
