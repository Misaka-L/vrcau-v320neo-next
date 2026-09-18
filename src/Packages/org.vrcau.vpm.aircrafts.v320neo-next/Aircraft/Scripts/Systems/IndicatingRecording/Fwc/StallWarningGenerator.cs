using UdonSharp;
using UnityEngine;
using VAU.V320NeoNext.Runtime.Bus;

namespace VAU.V320NeoNext.Runtime.Systems.IndicatingRecording.Fwc
{
    // In real aircraft, the stall warning is produced by:
    // FAC calculate VSW (Stall warning speed), and sound warning is produced by FWC
    // But we don't have these systems simulate at this time
    // So just make a simple script that generate stall warning when reach specify AoA
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public sealed class StallWarningGenerator : AbstractAvionicsBusClient
    {
        public AudioSource stallWarningAudioSource;

        public float flapUpAoAThreshold = 7f;
        public float flap1AoAThreshold = 9.5f;
        public float flap2AoAThreshold = 9.5f;
        public float flap3AoAThreshold = 9.5f;
        public float flapFullAoAThreshold = 9.5f;

        private float _currentStallWarningAoAThreshold;

        private bool _shouldProduceStallWarningLastFrame;

        protected override void _OnAvionicsBusStart()
        {
            _SubscribeInt(
                AvionicsBusIntDataIds.V32NN_Infrequent_Flap_SFCC_1_ActualFlapPosition,
                nameof(_FlapActualPositionChanged)
            );

            _currentStallWarningAoAThreshold = flapUpAoAThreshold;
        }

        public void _FlapActualPositionChanged()
        {
            switch (_ReadInt(AvionicsBusIntDataIds.V32NN_Infrequent_Flap_SFCC_1_ActualFlapPosition))
            {
                case 1:
                    _currentStallWarningAoAThreshold = flap1AoAThreshold;
                    break;
                case 2:
                    _currentStallWarningAoAThreshold = flap2AoAThreshold;
                    break;
                case 3:
                    _currentStallWarningAoAThreshold = flap3AoAThreshold;
                    break;
                case 4:
                    _currentStallWarningAoAThreshold = flapFullAoAThreshold;
                    break;
                default:
                    _currentStallWarningAoAThreshold = flapUpAoAThreshold;
                    break;
            }
        }

        private void Update()
        {
            var aoa = _ReadFloat(AvionicsBusFloatDataIds.V32NN_Frequent_ADIRS_ADR_1_AoA);
            var radioAltitude = _ReadFloat(AvionicsBusFloatDataIds.V32NN_Frequent_RA_1_RadioAltitude);
            var shouldProduceStallWarning = aoa > _currentStallWarningAoAThreshold && radioAltitude > 100f;

            if (_shouldProduceStallWarningLastFrame == shouldProduceStallWarning) return;
            _shouldProduceStallWarningLastFrame = shouldProduceStallWarning;

            if (shouldProduceStallWarning)
            {
                stallWarningAudioSource.Play();
            }
            else
            {
                stallWarningAudioSource.Stop();
            }
        }
    }
}