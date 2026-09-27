using UdonSharp;
using VAU.V320NeoNext.Runtime.Bus;
using VRC.SDKBase;

namespace VAU.V320NeoNext.Runtime.Systems.FlightControl
{
    /// <summary>
    /// 只负责 <c>V32NN_Frequent_ElevatorTrim_Sync_TrimPosition</c>（配平位置，-1~1）的网络镜像：
    /// 飞机 owner 每帧把总线上的值写进 <c>[UdonSynced]</c> 字段（Continuous 同步），
    /// 收到远端数据时写回本机总线。总线上的这个变量就是共享的配平状态。
    /// <para>
    /// 配平位置每帧都可能变化（自动配平 / 长按配平），所以与刹车输入一样用 Continuous 同步，
    /// 并在 LateUpdate 采样，保证取到的是本帧最终值。
    /// </para>
    /// 不持有任何飞机系统引用，也不做 system ↔ bus 转换（见 <see cref="ElevatorTrimAvionicsBusAdapter"/>）。
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
    public sealed class ElevatorTrimAvionicsBusContinuousSync : AbstractAvionicsBusClient
    {
        private const AvionicsBusFloatDataIds TrimPositionId =
            AvionicsBusFloatDataIds.V32NN_Frequent_ElevatorTrim_Sync_TrimPosition;

        [UdonSynced] private float _trimPosition;

        protected override void _OnAvionicsBusPostStart()
        {
            _SubscribeBool(
                AvionicsBusBoolDataIds.Sim_Infrequent_HaveAircraftOwnership,
                nameof(_OnHaveAircraftOwnershipChanged));
        }

        public void _OnHaveAircraftOwnershipChanged()
        {
            var hasAircraftOwnership = _ReadBool(AvionicsBusBoolDataIds.Sim_Infrequent_HaveAircraftOwnership);
            if (hasAircraftOwnership) TakeOwnership();
            enabled = hasAircraftOwnership;
        }

        private void LateUpdate()
        {
            if (!_ReadBool(AvionicsBusBoolDataIds.Sim_Infrequent_HaveAircraftOwnership))
            {
                enabled = false;
                return;
            }

            _trimPosition = _ReadFloat(TrimPositionId);
        }

        public override void OnDeserialization()
        {
            _WriteFloat(TrimPositionId, _trimPosition);
        }

        private void TakeOwnership()
        {
            if (!Networking.GetOwner(gameObject).isLocal) Networking.SetOwner(Networking.LocalPlayer, gameObject);
        }
    }
}
