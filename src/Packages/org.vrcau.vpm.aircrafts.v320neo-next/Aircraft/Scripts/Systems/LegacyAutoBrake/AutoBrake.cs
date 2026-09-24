using System;
using JetBrains.Annotations;
using SaccFlightAndVehicles;
using UdonSharp;
using UnityEngine;
using VAU.V320NeoNext.Runtime.Bus;
using VAU.V320NeoNext.Runtime.Systems.LandingGear.SaccExt;
using VAU.V320NeoNext.Runtime.Systems.LegacyFlightDataProvider;
using VAU.V320NeoNext.Runtime.Systems.LegacyFlightDataProvider.LegacyADRIRU;

namespace VAU.V320NeoNext.Runtime.Systems.LegacyAutoBrake
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class AutoBrake : AbstractAvionicsBusClient
    {
        public Animator indicatorAnimator;

        private DependenciesInjector _dependenciesInjector;
        private SaccAirVehicle _saccAirVehicle;
        private AircraftSystemData _aircraftSystemData;
        private ADIRU _adiru;

        public DFUNC_a320_Brake brake;

        private Vector3 _lastVelocity;

        #region PID

        private float _previousError;
        private float _integral;

        public float Kp = 0.01f;
        public float Ki = 0.01f;
        public float Kd = 0.01f;

        #endregion

        private bool _isLastFrameAircraftTouchdown;

        public bool isAutoBrakeActive
        {
            get => _isAutoBrakeActive;
            private set
            {
                if (_isAutoBrakeActive == value) return;

                Reset();
                _isAutoBrakeActive = value;

                RequestSerialization();
            }
        }

        [UdonSynced] [SerializeField] [HideInInspector]
        private bool _isAutoBrakeActive;

        #region Animation

        private readonly int AUTO_BRK_MODE = Animator.StringToHash("AutoBrkMode");
        private readonly int DECELERATION_HASH = Animator.StringToHash("Deceleration");

        #endregion

        public AutoBrakeMode currentAutoBrakeMode
        {
            get => _currentAutoBrakeMode;
            private set
            {
                if (_currentAutoBrakeMode == value) return;

                _currentAutoBrakeMode = value;

                // 总线上的那个变量就是档位状态本身：改档位＝直接更新总线上的状态。
                // 从总线跟随过来时不再写回，避免回环。
                if (!_isApplyingBusMode) _WriteAndNotifyInt(ModeStateId, ToBusValue(value));
            }
        }

        [NonSerialized] public bool isReachDecelerationRateTarget;

        [NonSerialized] private AutoBrakeMode _currentAutoBrakeMode = AutoBrakeMode.None;

        private bool _isApplyingBusMode;

        private const float _lowBrakeDecelerationRate = -1.7f; // -1.7m/s²
        private const float _medBrakeDecelerationRate = -3f; // -3m/s²

        #region FlightMenu bridge

        /// <summary>档位状态本身，由 AutoBrakeAvionicsBusSync 做网络同步。</summary>
        private const AvionicsBusIntDataIds ModeStateId =
            AvionicsBusIntDataIds.V32NN_Infrequent_AutoBrake_Sync_Mode;

        private const AvionicsBusBoolDataIds ActiveStatusId =
            AvionicsBusBoolDataIds.V32NN_Infrequent_AutoBrake_Active;

        private const AvionicsBusBoolDataIds ReachDecelTargetStatusId =
            AvionicsBusBoolDataIds.V32NN_Infrequent_AutoBrake_ReachDecelTarget;

        private bool _lastPublishedActive, _lastPublishedReachDecelerationRateTarget;
        private bool _hasPublishedStatus;

        protected override void _OnAvionicsBusPostStart()
        {
            // 不在初始化时主动跟随：总线默认值 0 就代表 Off，与系统初始档位一致
            _SubscribeInt(ModeStateId, nameof(_OnBusModeChanged));
        }

        /// <summary>总线上的档位状态变化（本地改档或远端同步过来）→ 直接跟随。</summary>
        public void _OnBusModeChanged()
        {
            _isApplyingBusMode = true;
            currentAutoBrakeMode = FromBusValue(_ReadInt(ModeStateId));
            _isApplyingBusMode = false;
        }

        // 总线编码：0=Off(None) 1=Low 2=Med 3=Max（0 作为默认值天然表示「未启用」）
        private int ToBusValue(AutoBrakeMode mode)
        {
            switch (mode)
            {
                case AutoBrakeMode.Low:
                    return 1;
                case AutoBrakeMode.Med:
                    return 2;
                case AutoBrakeMode.Max:
                    return 3;
                default:
                    return 0;
            }
        }

        private AutoBrakeMode FromBusValue(int value)
        {
            switch (value)
            {
                case 1:
                    return AutoBrakeMode.Low;
                case 2:
                    return AutoBrakeMode.Med;
                case 3:
                    return AutoBrakeMode.Max;
                default:
                    return AutoBrakeMode.None;
            }
        }

        private void Update()
        {
            var active = isAutoBrakeActive;
            var reachDecelerationRateTarget = isReachDecelerationRateTarget;

            if (_hasPublishedStatus
                && active == _lastPublishedActive
                && reachDecelerationRateTarget == _lastPublishedReachDecelerationRateTarget)
                return;

            _hasPublishedStatus = true;
            _lastPublishedActive = active;
            _lastPublishedReachDecelerationRateTarget = reachDecelerationRateTarget;

            _WriteAndNotifyBool(ActiveStatusId, active);
            _WriteAndNotifyBool(ReachDecelTargetStatusId, reachDecelerationRateTarget);
        }

        #endregion

        private void Start()
        {
            _dependenciesInjector = DependenciesInjector.GetInstance(this);
            _saccAirVehicle = _dependenciesInjector.saccAirVehicle;
            _aircraftSystemData = _dependenciesInjector.equipmentData;
            _adiru = _dependenciesInjector.adiru;

            _dependenciesInjector.systemEventBus.RegisterSaccEvent(this);
        }

        public void SFEXT_O_RespawnButton()
        {
            isAutoBrakeActive = false;
            currentAutoBrakeMode = AutoBrakeMode.None;
        }

        #region Touch Switch Event

        [PublicAPI]
        public void SelectAutoBrakeOff()
        {
            isAutoBrakeActive = false;
            currentAutoBrakeMode = AutoBrakeMode.None;
        }

        [PublicAPI]
        public void SelectAutoBrakeLow()
        {
            if (currentAutoBrakeMode != AutoBrakeMode.Low)
            {
                currentAutoBrakeMode = AutoBrakeMode.Low;
                return;
            }

            currentAutoBrakeMode = AutoBrakeMode.None;
        }

        [PublicAPI]
        public void SelectAutoBrakeMed()
        {
            if (currentAutoBrakeMode != AutoBrakeMode.Med)
            {
                currentAutoBrakeMode = AutoBrakeMode.Med;
                return;
            }

            currentAutoBrakeMode = AutoBrakeMode.None;
        }

        [PublicAPI]
        public void SelectedAutoBrakeMax()
        {
            if (!_aircraftSystemData.isAircraftGrounded)
            {
                currentAutoBrakeMode = AutoBrakeMode.None;
                return;
            }

            if (currentAutoBrakeMode != AutoBrakeMode.Max)
            {
                currentAutoBrakeMode = AutoBrakeMode.Max;
                return;
            }

            currentAutoBrakeMode = AutoBrakeMode.None;
        }

        #endregion

        #region Update

        private void LateUpdate()
        {
            var decelerationRate = GetDecelerationRate();

            if (currentAutoBrakeMode == AutoBrakeMode.Max && !_aircraftSystemData.isAircraftGrounded)
            {
                currentAutoBrakeMode = AutoBrakeMode.None;
            }

            if (_aircraftSystemData.isOwner)
            {
                UpdateAutoBrakeActive();
                UpdateAutoBrake(decelerationRate);
            }

            UpdateIndicator(decelerationRate);

            _isLastFrameAircraftTouchdown = _aircraftSystemData.isAircraftGrounded;
        }


        private void UpdateAutoBrake(float decelerationRate)
        {
            if (isAutoBrakeActive)
            {
                float brakeInput;
                if (currentAutoBrakeMode == AutoBrakeMode.Max)
                {
                    brakeInput = 1f;
                }
                else
                {
                    var targetDecelerationRate = GetTargetDecelerationRate();

                    var error = decelerationRate - targetDecelerationRate;
                    _integral += error * Time.deltaTime;
                    var derivative = (error - _previousError) / Time.deltaTime;
                    brakeInput = Kp * error + Ki * _integral + Kd * derivative;

                    _previousError = error;
                }

                _WriteFloat(
                    AvionicsBusFloatDataIds.V32NN_Frequent_Brake_Sync_AutoBrakeInput,
                    Mathf.Clamp(brakeInput, 0f, 1f)); 
            }
            else
            {
                _WriteFloat(AvionicsBusFloatDataIds.V32NN_Frequent_Brake_Sync_AutoBrakeInput, 0);
            }
        }

        private void UpdateAutoBrakeActive()
        {
            if (isAutoBrakeActive && 
                _ReadFloat(AvionicsBusFloatDataIds.V32NN_Frequent_Brake_Sync_PedalInput) > 0.1f)
            {
                isAutoBrakeActive = false;
                currentAutoBrakeMode = AutoBrakeMode.None;
                return;
            }

            if (currentAutoBrakeMode == AutoBrakeMode.None)
            {
                isAutoBrakeActive = false;
                return;
            }

            if (!_aircraftSystemData.isAircraftGrounded)
            {
                isAutoBrakeActive = false;
                return;
            }

            if (isAutoBrakeActive) return;

            switch (currentAutoBrakeMode)
            {
                case AutoBrakeMode.Low:
                    if (!_isLastFrameAircraftTouchdown)
                        isAutoBrakeActive = true;
                    break;
                case AutoBrakeMode.Med:
                    if (!_isLastFrameAircraftTouchdown)
                        isAutoBrakeActive = true;
                    break;
                case AutoBrakeMode.Max:
                    isAutoBrakeActive = _aircraftSystemData.isBothThrottleLevelerIdle && _adiru.irs.groundSpeed >= 72;
                    break;
                default:
                    isAutoBrakeActive = false;
                    break;
            }
        }

        private void UpdateIndicator(float decelerationRate)
        {
            float animationValue;
            switch (currentAutoBrakeMode)
            {
                case AutoBrakeMode.Low:
                    animationValue = 0f;
                    break;
                case AutoBrakeMode.Med:
                    animationValue = 1f;
                    break;
                case AutoBrakeMode.Max:
                    animationValue = 2f;
                    break;
                case AutoBrakeMode.None:
                default:
                    animationValue = 3f;
                    break;
            }

            indicatorAnimator.SetFloat(AUTO_BRK_MODE, animationValue / 3f);

            if (!isAutoBrakeActive)
            {
                indicatorAnimator.SetBool(DECELERATION_HASH, false);
                return;
            }

            isReachDecelerationRateTarget = IsReachDecelerationRateTarget(decelerationRate);

            indicatorAnimator.SetBool(DECELERATION_HASH, isReachDecelerationRateTarget);
        }

        #endregion

        private float GetDecelerationRate()
        {
            var velocity = _saccAirVehicle.CurrentVel;

            var acceleration = (velocity - _lastVelocity) / Time.fixedDeltaTime;
            var temp = _saccAirVehicle.transform.rotation * acceleration;

            _lastVelocity = velocity;

            return temp.z;
        }

        private float GetTargetDecelerationRate()
        {
            switch (currentAutoBrakeMode)
            {
                case AutoBrakeMode.Low:
                    return _lowBrakeDecelerationRate;
                case AutoBrakeMode.Med:
                    return _medBrakeDecelerationRate;
                case AutoBrakeMode.None:
                    return 0f;
                default:
                    return -3f;
            }
        }

        private bool IsReachDecelerationRateTarget(float decelerationRate)
        {
            if (currentAutoBrakeMode == AutoBrakeMode.Max) return true;

            var targetDecelerationRate = GetTargetDecelerationRate() * 0.8f;
            return decelerationRate < targetDecelerationRate;
        }

        public void Reset()
        {
            _previousError = 0f;
            _integral = 0f;
        }
    }

    public enum AutoBrakeMode
    {
        Low,
        Med,
        Max,
        None
    }
}