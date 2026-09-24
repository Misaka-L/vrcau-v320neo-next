using UdonSharp;
using VAU.V320NeoNext.Runtime.Bus;
using VAU.V320NeoNext.Runtime.Systems.FlightControl.SaccExt;

namespace VAU.V320NeoNext.Runtime.Systems.FlightControl
{
    /// <summary>
    /// 襟翼的 system ↔ bus 转换：
    /// 把 <c>V32NN_Infrequent_Flap_RequestedLeverIndex</c> 应用到
    /// <see cref="DFUNC_a320_FlapController"/>，并把**原始值**（手柄档位 / 指令档位 /
    /// 实际档位 / slat 与 flap 角度）发布到 bus。
    /// <para>
    /// 状态文本属于 UI 特有逻辑，由 <c>FlapFlightMenuController</c> 自己根据这些原始值生成，
    /// 本类不做任何文案处理。
    /// </para>
    /// <para>
    /// leverIndex 由 <see cref="DFUNC_a320_FlapController"/> 自己维护网络同步，总线不重复同步。
    /// 这些值在襟翼移动期间每帧都变，所以只写不 Notify，由 bridge 轮询。
    /// </para>
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public sealed class FlapControllerAvionicsBusAdapter : AbstractAvionicsBusClient
    {
        public DFUNC_a320_FlapController flapController;

        private const AvionicsBusIntDataIds LeverIndexRequestId =
            AvionicsBusIntDataIds.V32NN_Infrequent_Flap_RequestedLeverIndex;

        private const AvionicsBusIntDataIds LeverIndexStatusId =
            AvionicsBusIntDataIds.V32NN_Frequent_Flap_LeverIndex;

        private const AvionicsBusIntDataIds FlapLevelerPositionId =
            AvionicsBusIntDataIds.V32NN_Frequent_Flap_LevelerPosition;

        private const AvionicsBusIntDataIds FlapActualPositionId =
            AvionicsBusIntDataIds.V32NN_Frequent_Flap_SFCC_1_ActualFlapPosition;

        private const AvionicsBusFloatDataIds SlatAngleId =
            AvionicsBusFloatDataIds.V32NN_Frequent_Flap_SlatAngle;

        private const AvionicsBusFloatDataIds FlapAngleId =
            AvionicsBusFloatDataIds.V32NN_Frequent_Flap_FlapAngle;

        private int _lastLeverIndex, _lastFlapLevelerPosition, _lastFlapActualPosition;
        private float _lastSlatAngle, _lastFlapAngle;
        private bool _hasPublished;

        protected override void _OnAvionicsBusPostStart()
        {
            _SubscribeInt(LeverIndexRequestId, nameof(_OnLeverIndexRequested));
        }

        /// <summary>
        /// leverIndex 由 <see cref="DFUNC_a320_FlapController"/> 自己维护网络同步，
        /// 总线不重复同步；只有按下菜单的客户端会收到通知，因此无需 owner 判定。
        /// </summary>
        public void _OnLeverIndexRequested()
        {
            if (!flapController) return;

            var requested = _ReadInt(LeverIndexRequestId);
            if (requested == flapController.leverIndex) return;

            // SetLeverIndex() 自身不序列化，需要显式请求（非 owner 时为空操作，由系统自己的同步收敛）
            flapController.SetLeverIndex(requested);
            flapController.RequestSerialization();
        }

        /// <summary>
        /// 只发布原始值，不 Notify（bridge 轮询）。detentIndex 为负表示
        /// 「正在向 -detentIndex-1 移动」，原样发布。
        /// </summary>
        private void Update()
        {
            if (!flapController) return;

            var leverIndex = flapController.leverIndex;
            var flapLevelerPosition = flapController.targetDetentIndex;
            var flapActualPosition = flapController.detentIndex;
            var slatAngle = flapController.slatAngle;
            var flapAngle = flapController.flapAngle;

            if (_hasPublished
                && leverIndex == _lastLeverIndex
                && flapLevelerPosition == _lastFlapLevelerPosition
                && flapActualPosition == _lastFlapActualPosition
                && slatAngle == _lastSlatAngle
                && flapAngle == _lastFlapAngle)
                return;

            _hasPublished = true;
            _lastLeverIndex = leverIndex;
            _lastFlapLevelerPosition = flapLevelerPosition;
            _lastFlapActualPosition = flapActualPosition;
            _lastSlatAngle = slatAngle;
            _lastFlapAngle = flapAngle;

            _WriteInt(LeverIndexStatusId, leverIndex);
            _WriteInt(FlapLevelerPositionId, flapLevelerPosition);
            _WriteInt(FlapActualPositionId, flapActualPosition);
            _WriteFloat(SlatAngleId, slatAngle);
            _WriteFloat(FlapAngleId, flapAngle);
        }
    }
}
