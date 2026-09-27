using UdonSharp;
using VAU.V320NeoNext.Runtime.Bus;
using VRC.SDKBase;

namespace VAU.V320NeoNext.Runtime.Systems.FlightControl
{
    /// <summary>
    /// Auto Trim 状态本身的网络镜像：总线上的
    /// <c>V32NN_Infrequent_ElevatorTrim_Sync_AutoTrimActive</c> 就是状态，
    /// 本类只负责 本机变化 → 抢所有权 + 序列化；收到远端数据 → 写回本机总线。
    /// <para>
    /// 配平位置**不在这里同步**：它是一个每帧都可能变化的数值，由
    /// <see cref="ElevatorTrimAvionicsBusContinuousSync"/> 用 Continuous 同步负责。
    /// </para>
    /// 不持有任何飞机系统引用，也不做 system ↔ bus 转换（系统自己读写总线，见
    /// <see cref="SaccExt.DFUNC_a320_ElevatorTrim"/>）。
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public sealed class ElevatorTrimAvionicsBusSync : AbstractAvionicsBusClient
    {
        private const AvionicsBusBoolDataIds AutoTrimActiveId =
            AvionicsBusBoolDataIds.V32NN_Infrequent_ElevatorTrim_Sync_AutoTrimActive;

        /// <summary>Auto Trim 的开局状态；只在本机还没收到过网络数据时用来初始化总线。</summary>
        public bool autoTrimActiveOnStart = true;

        /// <summary>网络同步的存储。运行期以总线上的状态为准，这里只做镜像。</summary>
        [UdonSynced] private bool _autoTrimActive;

        private bool _hasAppliedSyncedData;

        protected override void _OnAvionicsBusPostStart()
        {
            _SubscribeBool(AutoTrimActiveId, nameof(_OnCommandChanged));

            // 还没收到过网络数据：本机就是开局的那一份，把初值同时放进同步存储和总线。
            // 写总线刻意不 Notify：否则每个客户端开局都会去抢所有权并序列化，
            // 晚加入的客户端还会把别人关掉的 Auto Trim 强行打开。
            if (!_hasAppliedSyncedData) _autoTrimActive = autoTrimActiveOnStart;
            _WriteBool(AutoTrimActiveId, _autoTrimActive);
        }

        public void _OnCommandChanged()
        {
            // 不复用「值相同就跳过」的短路：同一个目标值也可能是新的一次按键
            if (!Networking.IsOwner(gameObject)) Networking.SetOwner(Networking.LocalPlayer, gameObject);

            _autoTrimActive = _ReadBool(AutoTrimActiveId);
            RequestSerialization();
        }

        public override void OnDeserialization()
        {
            // 只写总线、不 Notify：状态就在总线上，系统直接读，没有人需要这个事件
            _hasAppliedSyncedData = true;
            _WriteBool(AutoTrimActiveId, _autoTrimActive);
        }
    }
}
