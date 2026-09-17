using UdonSharp;
using UnityEngine;
using VAU.V320NeoNext.Runtime.Bus;

namespace VAU.V320NeoNext.Runtime.Systems.RadioAltimeter {
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DefaultExecutionOrder(2030)]
    public class RadioAltimeter : AbstractAvionicsBusClient {
        public LayerMask groundLayers = -1;
        public QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
        public Transform groundDetector, offsetTransform;

        private float _maxRange; // unit: nm
        private float _offset;

        public float radioAltitude { get; private set; }
        public bool isAvailable => !Mathf.Approximately(radioAltitude, _maxRange);

        private void Start() {
            _maxRange = 2500f;
            _offset = Vector3.Dot(groundDetector.up, offsetTransform.position - groundDetector.position);
        }

        private void FixedUpdate() {
            radioAltitude = GetRadioAltitude();
            _WriteFloat(AvionicsBusFloatDataIds.V32NN_Frequent_RA_1_RadioAltitude, radioAltitude);
        }

        private float GetRadioAltitude() {
            var position = groundDetector.position;
            if (Physics.Raycast(position, Vector3.down, out var hit, _maxRange * 0.3048f, groundLayers,
                    queryTriggerInteraction))
                return (hit.distance + _offset) * 3.28084f;

            return _maxRange;
        }
    }
}