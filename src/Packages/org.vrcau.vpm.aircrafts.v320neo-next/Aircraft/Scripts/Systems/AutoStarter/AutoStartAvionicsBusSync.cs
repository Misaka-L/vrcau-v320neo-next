using UdonSharp;
using VAU.V320NeoNext.Runtime.Bus;
using VRC.SDKBase;

namespace VAU.V320NeoNext.Runtime.Systems.AutoStarter
{
    /// <summary>
    /// 只负责 <c>V32NN_Infrequent_AutoStart_Sync_Start</c> 的网络镜像：
    /// 本机 bus 变化 → 抢所有权 + 序列化；收到远端数据 → 写回本机 bus。
    /// 不持有任何飞机系统引用，也不做 system ↔ bus 转换（见 AutoStartAvionicsBusAdapter）。
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public sealed class AutoStartAvionicsBusSync : AbstractAvionicsBusClient
    {
        private const AvionicsBusBoolDataIds StartId =
            AvionicsBusBoolDataIds.V32NN_Infrequent_AutoStart_Sync_Start;

        [UdonSynced] private bool _start;

        private bool _isApplyingSyncedData;

        protected override void _OnAvionicsBusPostStart()
        {
            _SubscribeBool(StartId, nameof(_OnStartChanged));
        }

        public void _OnStartChanged()
        {
            if (_isApplyingSyncedData) return;

            var value = _ReadBool(StartId);
            if (value == _start) return;

            if (!Networking.IsOwner(gameObject)) Networking.SetOwner(Networking.LocalPlayer, gameObject);

            _start = value;
            RequestSerialization();
        }

        public override void OnDeserialization()
        {
            _isApplyingSyncedData = true;
            _WriteAndNotifyBool(StartId, _start);
            _isApplyingSyncedData = false;
        }
    }
}
