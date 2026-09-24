using UdonSharp;
using VAU.V320NeoNext.Runtime.Bus;
using VRC.SDKBase;

namespace VAU.V320NeoNext.Runtime.Systems.LegacyAutoBrake
{
    /// <summary>
    /// 只负责 <c>V32NN_Infrequent_AutoBrake_Sync_Mode</c>（档位状态本身，0=Off 1=Low 2=Med 3=Max）
    /// 的网络镜像：本机 bus 变化 → 抢所有权 + 序列化；收到远端数据 → 写回本机 bus。
    /// <para>AutoBrake 系统与本类一样直接读写这个变量，因此档位状态就是总线上的状态。</para>
    /// 不持有任何飞机系统引用，也不做 system ↔ bus 转换。
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public sealed class AutoBrakeAvionicsBusSync : AbstractAvionicsBusClient
    {
        private const AvionicsBusIntDataIds ModeRequestId =
            AvionicsBusIntDataIds.V32NN_Infrequent_AutoBrake_Sync_Mode;

        [UdonSynced] private int _mode;

        private bool _isApplyingSyncedData;

        protected override void _OnAvionicsBusPostStart()
        {
            _SubscribeInt(ModeRequestId, nameof(_OnCommandChanged));
        }

        public void _OnCommandChanged()
        {
            if (_isApplyingSyncedData) return;

            // 不复用「值相同就跳过」的短路：同一个目标值也可能是新的一次按键
            if (!Networking.IsOwner(gameObject)) Networking.SetOwner(Networking.LocalPlayer, gameObject);

            _mode = _ReadInt(ModeRequestId);
            RequestSerialization();
        }

        public override void OnDeserialization()
        {
            _isApplyingSyncedData = true;
            _WriteAndNotifyInt(ModeRequestId, _mode);
            _isApplyingSyncedData = false;
        }
    }
}
