using System;
using JetBrains.Annotations;
using SaccFlightAndVehicles;
using UdonSharp;
using UnityEngine;
using VAU.V320NeoNext.Runtime.Bus;
using VAU.V320NeoNext.Runtime.Systems.AuxiliaryPowerUnit;
using VAU.V320NeoNext.Runtime.Systems.Engine.SaccExt;
using VRC.SDKBase;

//note:this code is original from https://github.com/esnya/EsnyaSFAddons
//to satisfy vau320's demand, add eletrical start
namespace VAU.V320NeoNext.Runtime.Systems.AutoStarter {
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    [DefaultExecutionOrder(1000)] // After SaccAirVehicle
    public class A320AutoStarter : AbstractAvionicsBusClient {
        public const byte STATE_OFF = 0;
        public const byte STATE_ElETRICAL_START = 1;
        public const byte STATE_ElETRICAL_STOP = 2;
        public const byte STATE_APU_START = 3;
        public const byte STATE_APU_STOP = 4;
        public const byte STATE_ENGINE_START = 5;
        public const byte STATE_ENGINE_STOP = 6;
        public const byte STATE_ON = 255;

        [PublicAPI] public bool isIndicatorActivated;

        public KeyCode startKey = KeyCode.LeftShift;
        public bool desktopOnly;

        [Header("Engine")]
        [Tooltip("[s]")] public float engineStartInterval = 30.0f;

        [Tooltip("[s]")] public float engineStopInterval = 30.0f;
        private SaccAirVehicle airVehicle;
        private SFEXT_AuxiliaryPowerUnit apu;
        // private YFI_ElectricalBus eletricalBus;
        private SFEXT_a320_AdvancedEngine[] engines;

        private bool holdThrottle;
        private bool initialized, isPilot, isPassenger, isOwner;

        private byte prevState;

        [Header("Runtime Local State")]
        [NonSerialized] [UdonSynced] public byte state;

        private float stateChangedTime;

        // —— FlightMenu bridge ——
        // 启动请求的状态就存在总线上（V32NN_Infrequent_AutoStart_Sync_Start），
        // 本系统直接读写同一个变量，不再维护第二份 state，也不需要额外的 adapter。
        private const AvionicsBusBoolDataIds StartId =
            AvionicsBusBoolDataIds.V32NN_Infrequent_AutoStart_Sync_Start;

        private const AvionicsBusBoolDataIds IndicatorActivatedId =
            AvionicsBusBoolDataIds.V32NN_Infrequent_AutoStart_IndicatorActivated;

        private bool _busReady;
        private bool _hasPublishedIndicatorActivated;
        private bool _lastIndicatorActivated;

        /// <summary>总线上的这一个变量就是「是否已请求启动」的状态本身。</summary>
        private bool StartRequested {
            get => _busReady && _ReadBool(StartId);
            set { if (_busReady) _WriteAndNotifyBool(StartId, value); }
        }

        protected override void _OnAvionicsBusStart() {
            _busReady = true;
        }

        /// <summary>
        /// isIndicatorActivated 本来就在 Update 里算好了，这里只在它变化时同步到总线
        /// （很少变化，走事件通知），不需要额外的 adapter。
        /// </summary>
        private void PublishIndicatorActivated() {
            if (!_busReady) return;
            if (_hasPublishedIndicatorActivated && isIndicatorActivated == _lastIndicatorActivated) return;

            _hasPublishedIndicatorActivated = true;
            _lastIndicatorActivated = isIndicatorActivated;
            _WriteAndNotifyBool(IndicatorActivatedId, isIndicatorActivated);
        }

        private void Update() {
            if (!initialized) return;

            var time = Time.time;

            if (isPilot) {
                if (Input.GetKeyDown(startKey)) {
                    if (!StartRequested) {
                        holdThrottle = true;
                    }

                    StartRequested = !StartRequested;
                }

                if (holdThrottle && Input.GetKeyUp(startKey)) {
                    holdThrottle = false;
                    airVehicle.ThrottleInput = 0.375f;
                }
            }

            if (isOwner) {
                isIndicatorActivated = StartRequested;
            }
            else if (isPassenger) {
                var remoteStart = state != STATE_OFF;
                isIndicatorActivated = remoteStart;
            }

            PublishIndicatorActivated();

            var stateChanged = state != prevState;
            prevState = state;

            if (stateChanged) {
                stateChangedTime = time;
                switch (state) {
                    case STATE_OFF:
                        Debug.Log("[ZHI][AutoStarter] Off");
                        break;
                    case STATE_ElETRICAL_START:
                        Debug.Log("[ZHI][AutoStarter] Eletrical Start");
                        break;
                    case STATE_ElETRICAL_STOP:
                        Debug.Log("[ZHI][AutoStarter] Eletrical Stop");
                        break;
                    case STATE_APU_START:
                        Debug.Log("[ZHI][AutoStarter] APU Start");
                        break;
                    case STATE_APU_STOP:
                        Debug.Log("[ZHI][AutoStarter] APU Stop");
                        break;
                    case STATE_ENGINE_START:
                        Debug.Log("[ZHI][AutoStarter] Engine Start");
                        break;
                    case STATE_ENGINE_STOP:
                        Debug.Log("[ZHI][AutoStarter] Engine Stop");
                        break;
                    case STATE_ON:
                        Debug.Log("[ZHI][AutoStarter] On");
                        break;
                }
            }

            var stateTime = time - stateChangedTime;

            switch (state) {
                case STATE_OFF:
                    if (StartRequested) SetState(STATE_ElETRICAL_START);
                    break;
                case STATE_ElETRICAL_START:
                    if (isOwner) {
                        // if (stateChanged && !eletricalBus.batteryOn) eletricalBus.OnToggleBattery();
                        // if (!eletricalBus || eletricalBus.hasPower) SetState(STATE_APU_START);
                        SetState(STATE_APU_START);
                    }

                    break;
                case STATE_ElETRICAL_STOP:
                    // if (isOwner) {
                    //     if (stateChanged && eletricalBus.batteryOn) eletricalBus.OnToggleBattery();
                    //     if (!eletricalBus || !eletricalBus.hasPower) SetState(start ? STATE_ON : STATE_OFF);
                    // }
                    SetState(StartRequested ? STATE_ON : STATE_OFF);

                    break;
                case STATE_APU_START:
                    if (isOwner) {
                        if (stateChanged && apu) apu.StartAPU();
                        if (!apu || apu.started) SetState(STATE_ENGINE_START);
                    }

                    break;
                case STATE_APU_STOP:
                    if (isOwner) {
                        if (stateChanged && apu) apu.StopAPU();
                        if (!apu || apu.terminated) SetState(StartRequested ? STATE_ON : STATE_OFF);
                    }

                    break;
                case STATE_ENGINE_START:
                    if (isOwner) {
                        var starterIndex = engines.Length - Mathf.FloorToInt(stateTime / engineStartInterval) - 1;
                        if (starterIndex >= 0 && starterIndex < engines.Length) engines[starterIndex].starter = true;
                    }

                    var allEngineStarted = true;
                    foreach (var engine in engines) {
                        if (!engine) continue;
                        if (engine.n2 >= engine.minN2)
                            if (isOwner)
                                engine.fuel = true;

                        if (engine.n1 >= engine.idleN1 * 0.9f) {
                            if (isOwner) engine.starter = false;
                        }
                        else {
                            allEngineStarted = false;
                        }
                    }

                    if (allEngineStarted) SetState(STATE_APU_STOP);
                    break;
                case STATE_ENGINE_STOP:
                    var index = engines.Length - Mathf.FloorToInt(stateTime / engineStopInterval) - 1;
                    if (index < 0) {
                        SetState(STATE_ElETRICAL_STOP);
                    }
                    else if (index < engines.Length && isOwner) {
                        engines[index].starter = false;
                        engines[index].fuel = false;
                    }

                    break;

                case STATE_ON:
                    if (!StartRequested) SetState(STATE_ENGINE_STOP);
                    break;
            }

            if (isOwner && !isPilot && (state == STATE_ON || state == STATE_OFF)) gameObject.SetActive(false);
        }

        public void SFEXT_L_EntityStart() {
            var entity = GetComponentInParent<SaccEntity>();
            airVehicle = (SaccAirVehicle)GetExtention(entity, GetUdonTypeName<SaccAirVehicle>());

            // eletricalBus = (YFI_ElectricalBus)GetExtention(entity, GetUdonTypeName<YFI_ElectricalBus>());
            apu = (SFEXT_AuxiliaryPowerUnit)GetExtention(entity, GetUdonTypeName<SFEXT_AuxiliaryPowerUnit>());
            engines = (SFEXT_a320_AdvancedEngine[])GetExtentions(entity, GetUdonTypeName<SFEXT_a320_AdvancedEngine>());

            StartRequested = false;
            state = STATE_OFF;

            isIndicatorActivated = false;
            gameObject.SetActive(false);
            initialized = true;
        }

        public void SFEXT_O_PilotEnter() {
            if (desktopOnly && Networking.LocalPlayer.IsUserInVR()) return;

            isPilot = true;
            isOwner = true;
            gameObject.SetActive(true);
        }

        public void SFEXT_O_PilotExit() {
            isPilot = false;
        }

        public void SFEXT_P_PassengerEnter() {
            isPassenger = true;
        }

        public void SFEXT_P_PassengerExit() {
            isPassenger = false;
        }

        public void SFEXT_G_Explode() {
            ResetStatus();
        }

        public void SFEXT_G_RespawnButton() {
            ResetStatus();
        }

        public void SFEXT_O_TakeOwnership() {
            isOwner = true;
        }

        public void SFEXT_O_LoseOwnership() {
            isOwner = false;
        }

        private void ResetStatus() {
            state = STATE_OFF;
            StartRequested = false;
        }

        public override void PostLateUpdate() {
            if (isOwner && holdThrottle) airVehicle.ThrottleInput = 0;
        }

        [PublicAPI]
        public void _ToggleStart()
        {
            StartRequested = !StartRequested;
        }

        private void SetState(byte value) {
            if (!isOwner) return;
            state = value;
            RequestSerialization();
        }

    #region SFEXT Utilities

        private UdonSharpBehaviour GetExtention(SaccEntity entity, string udonTypeName) {
            foreach (var extention in entity.ExtensionUdonBehaviours)
                if (extention && extention.GetUdonTypeName() == udonTypeName)
                    return extention;
            foreach (var extention in entity.Dial_Functions_L)
                if (extention && extention.GetUdonTypeName() == udonTypeName)
                    return extention;
            foreach (var extention in entity.Dial_Functions_R)
                if (extention && extention.GetUdonTypeName() == udonTypeName)
                    return extention;
            return null;
        }

        private UdonSharpBehaviour[] GetExtentions(SaccEntity entity, string udonTypeName) {
            var result = new UdonSharpBehaviour[entity.ExtensionUdonBehaviours.Length + entity.Dial_Functions_L.Length +
                                                entity.Dial_Functions_R.Length];
            var count = 0;
            foreach (var extention in entity.ExtensionUdonBehaviours)
                if (extention && extention.GetUdonTypeName() == udonTypeName)
                    result[count++] = extention;
            foreach (var extention in entity.Dial_Functions_L)
                if (extention && extention.GetUdonTypeName() == udonTypeName)
                    result[count++] = extention;
            foreach (var extention in entity.Dial_Functions_R)
                if (extention && extention.GetUdonTypeName() == udonTypeName)
                    result[count++] = extention;

            var finalResult = new UdonSharpBehaviour[count];
            Array.Copy(result, finalResult, count);

            return finalResult;
        }

    #endregion
    }
}