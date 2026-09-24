using UdonSharp;
using VAU.V320NeoNext.Runtime.Bus;
using VRC.SDKBase;

namespace VAU.V320NeoNext.Runtime.Systems.FlightControl
{
    /// <summary>
    /// 只负责 <c>V32NN_Infrequent_ElevatorTrim_Sync_AutoTrimActive</c>（Auto Trim 目标状态）的
    /// 网络镜像：本机 bus 变化 → 抢所有权 + 序列化；收到远端数据 → 写回本机 bus。
    /// <para>
    /// 目标配平位置**不在这里同步**：<c>DFUNC_a320_ElevatorTrim.trim</c> 本身是
    /// <c>[UdonSynced]</c>（Continuous），由系统自己维护网络同步，总线不重复同步。
    /// </para>
    /// 不持有任何飞机系统引用，也不做 system ↔ bus 转换（见 ElevatorTrimAvionicsBusAdapter）。
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public sealed class ElevatorTrimAvionicsBusSync : AbstractAvionicsBusClient
    {
        private const AvionicsBusBoolDataIds AutoTrimActiveRequestId =
            AvionicsBusBoolDataIds.V32NN_Infrequent_ElevatorTrim_Sync_AutoTrimActive;

        [UdonSynced] private bool _autoTrimActive;

        private bool _isApplyingSyncedData;

        protected override void _OnAvionicsBusPostStart()
        {
            _SubscribeBool(AutoTrimActiveRequestId, nameof(_OnCommandChanged));
        }

        public void _OnCommandChanged()
        {
            if (_isApplyingSyncedData) return;

            // 不复用「值相同就跳过」的短路：同一个目标值也可能是新的一次按键
            if (!Networking.IsOwner(gameObject)) Networking.SetOwner(Networking.LocalPlayer, gameObject);

            _autoTrimActive = _ReadBool(AutoTrimActiveRequestId);
            RequestSerialization();
        }

        public override void OnDeserialization()
        {
            _isApplyingSyncedData = true;
            _WriteAndNotifyBool(AutoTrimActiveRequestId, _autoTrimActive);
            _isApplyingSyncedData = false;
        }
    }
}
