using System;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VAU.V320NeoNext.Runtime.Bus;

namespace VAU.V320NeoNext.Runtime.InputSystem.FlightMenuController.Flap
{
    /// <summary>
    /// FlightMenu 与襟翼（DFUNC_a320_FlapController）之间的 bridge。
    /// 只读写 AvionicsBus，不持有任何飞机系统引用。
    /// <para>
    /// 菜单的 Flaps 项是 FlightMenuSliderItem：滑块的索引变量是 <c>flapLeverIndex</c>，
    /// 索引变化时回调 <c>ApplyFlapLeverIndex()</c>。
    /// </para>
    /// <para>
    /// 状态文本是 UI 特有逻辑，在这里根据总线上的原始值（档位 / 角度）生成，不放到总线上。
    /// leverIndex 的网络同步由 DFUNC_a320_FlapController 自己维护，总线不重复同步。
    /// </para>
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public sealed class FlapFlightMenuController : AbstractAvionicsBusClient
    {
        // 滑块索引变量（FlightMenuSliderItem.sliderIndexVariableName）
        [NonSerialized] public int flapLeverIndex;

        // 菜单标题 + 滑块描述（FlightMenuItemBase.titleVariableName / sliderDescriptionVariableName）
        [NonSerialized] public string flapStatusText;

        /// <summary>
        /// 用于把当前位置换算成档位文字。必须与 <c>DFUNC_a320_FlapController</c> 的
        /// <c>flapDetents</c> / <c>slatDetents</c> 保持一致。
        /// </summary>
        public float[] flapDetents = { 0, 0, 10, 15, 20, 35 };
        public float[] slatDetents = { 0, 18, 18, 22, 22, 27 };

        private int _lastSeenLeverIndex;
        private bool _hasSeenLeverIndex;
        private string _lastStatusText;

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

        private void Update()
        {
            // 只在总线值真的变化时回写 flapLeverIndex：滑块刚设好值、系统还没回读时，
            // 如果无条件覆盖会把滑块弹回旧档位。
            var leverIndex = _ReadInt(LeverIndexStatusId);
            if (!_hasSeenLeverIndex || leverIndex != _lastSeenLeverIndex)
            {
                _hasSeenLeverIndex = true;
                _lastSeenLeverIndex = leverIndex;
                flapLeverIndex = leverIndex;
            }

            var statusText = BuildStatusText();
            if (statusText != _lastStatusText)
            {
                _lastStatusText = statusText;
                flapStatusText = statusText;
            }
        }

        // FlightMenuSliderItem.onSliderIndexChangedEventName（离散动作，走事件通知）
        [PublicAPI]
        public void ApplyFlapLeverIndex()
        {
            _WriteAndNotifyInt(LeverIndexRequestId, flapLeverIndex);
        }

        #region 状态文本（原 FlapFlightMenuController 的逻辑）

        private string BuildStatusText()
        {
            var detentIndex = _ReadInt(FlapActualPositionId);
            var isMoving = detentIndex < 0;
            var targetDetentIndex = _ReadInt(FlapLevelerPositionId);

            var currentFlapPositionText = isMoving ? GetMovingFlapPositionText() : GetFlapPositionText(detentIndex);
            var targetFlapPositionText = GetFlapPositionText(targetDetentIndex);

            if (!isMoving && detentIndex == targetDetentIndex) return currentFlapPositionText;

            return currentFlapPositionText + " -> " + targetFlapPositionText;
        }

        private string GetMovingFlapPositionText()
        {
            var currentSlatAngle = _ReadFloat(SlatAngleId);
            var currentFlapAngle = _ReadFloat(FlapAngleId);

            for (var index = slatDetents.Length - 1; index >= 0; index--)
            {
                var slatAngle = slatDetents[index];
                var flapAngle = flapDetents[index];

                if (currentSlatAngle > slatAngle && currentFlapAngle > flapAngle)
                    return "> " + GetFlapPositionText(index);

                if (Mathf.Approximately(currentSlatAngle, slatAngle) && Mathf.Approximately(currentFlapAngle, flapAngle))
                    return GetFlapPositionText(index) + "~";
            }

            return "Unknown";
        }

        private string GetFlapPositionText(int index)
        {
            switch (index)
            {
                case 0:
                    return "UP";
                case 1:
                    return "1";
                case 2:
                    return "1+F";
                case 3:
                    return "2";
                case 4:
                    return "3";
                case 5:
                    return "FULL";
            }

            return index.ToString();
        }

        #endregion
    }
}
