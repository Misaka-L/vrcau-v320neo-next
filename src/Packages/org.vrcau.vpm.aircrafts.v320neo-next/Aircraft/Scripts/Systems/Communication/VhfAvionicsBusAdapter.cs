using UdonSharp;
using UdonRadioCommunicationRedux.SaccFlight;
using VAU.V320NeoNext.Runtime.Bus;

namespace VAU.V320NeoNext.Runtime.Systems.Communication
{
    /// <summary>
    /// VHF 的 system ↔ bus 转换：
    /// 把 <c>V32NN_Infrequent_VHF_RequestedRxOn</c> / <c>..._RequestedTxOn</c> 应用到
    /// <see cref="SFEXT_URC_VHF"/>，并把 Rx / Tx / 频率发布到 status id。
    /// <para>
    /// SFEXT_URC_VHF 自己提供 <c>CallbackBehaviours</c> 事件通知
    /// （OnUpdateChannel / OnStartReceive / OnStopReceive / OnStartTransmit / OnStopTransmit），
    /// 所以这里只在事件里发布状态，<b>不使用 Update 轮询</b>。
    /// </para>
    /// <para>
    /// SFEXT_URC_VHF 自己也负责网络同步（TxOn/SetChannel 会抢所有权并序列化，RxPower 本来就是本机量），
    /// 因此这几个 id **不带** <c>_Sync_</c>、也不做 owner 判定：
    /// 只有按下菜单的那个客户端会写自己的本地总线，也就只有它的 Adapter 会应用。
    /// </para>
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public sealed class VhfAvionicsBusAdapter : AbstractAvionicsBusClient
    {
        public SFEXT_URC_VHF transceiver;

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

        /// <summary>注册到收发机的回调列表，并发布一次初始状态。</summary>
        private void Start()
        {
            if (!transceiver) return;

            var callbacks = transceiver.CallbackBehaviours;
            if (callbacks == null) callbacks = new UdonSharpBehaviour[0];

            var newArray = new UdonSharpBehaviour[callbacks.Length + 1];
            callbacks.CopyTo(newArray, 0);
            newArray[callbacks.Length] = this;
            transceiver.CallbackBehaviours = newArray;

            PublishStatus();
        }

        protected override void _OnAvionicsBusPostStart()
        {
            _SubscribeBool(RxRequestId, nameof(_OnRxRequested));
            _SubscribeBool(TxRequestId, nameof(_OnTxRequested));
        }

        public void _OnRxRequested()
        {
            if (!transceiver) return;

            var requested = _ReadBool(RxRequestId);
            if (requested == transceiver.RxPower) return;

            if (requested) transceiver.RxOn();
            else transceiver.RxOff();
        }

        public void _OnTxRequested()
        {
            if (!transceiver) return;

            var requested = _ReadBool(TxRequestId);
            if (requested == transceiver.TxPower) return;

            if (requested) transceiver.TxOn();
            else transceiver.TxOff();
        }

        #region SFEXT_URC_VHF CallbackBehaviours

        public void OnUpdateChannel() => PublishStatus();

        public void OnStartReceive() => PublishStatus();

        public void OnStopReceive() => PublishStatus();

        public void OnStartTransmit() => PublishStatus();

        public void OnStopTransmit() => PublishStatus();

        public void ChannelTransmitting() => PublishStatus();

        public void ChannelNotTransmitting() => PublishStatus();

        #endregion

        private void PublishStatus()
        {
            if (!transceiver) return;

            _WriteAndNotifyBool(RxPowerId, transceiver.RxPower);
            _WriteAndNotifyBool(TxPowerId, transceiver.TxPower);
            _WriteAndNotifyInt(ChannelKhzId, transceiver.Channel);
        }
    }
}
