using JetBrains.Annotations;
using SaccFlightAndVehicles;
using UdonSharp;
using UnityEngine;
using VAU.V320NeoNext.Runtime.Bus;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

namespace VAU.V320NeoNext.Runtime.Systems.LandingGear.SaccExt
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class DFUNC_a320_Brake : AbstractAvionicsBusClient
    {
        [Tooltip("Looping sound to play while brake is active")]
        public AudioSource Airbrake_snd;

        [Tooltip("Will Crash if not set")] public Animator BrakeAnimator;

        public float AirbrakeStrength = 4f;

        public bool NoPilotAlwaysParkBrake = true;

        private float AirbrakeLerper;
        private int BRAKE_STRING = Animator.StringToHash("brake");
        private float BrakeStrength;

        private bool Braking;
        private bool BrakingLastFrame;

        private SaccEntity EntityControl;
        public SaccAirVehicle SAVControl;
        private bool HasAirBrake;
        private float LastDrag;
        private float NextUpdateTime;

        private float
            NonLocalActiveDelay; //this var is for adding a min delay for disabling for non-local users to account for lag

        private float RotMultiMaxSpeedDivider;

        private const AvionicsBusBoolDataIds ParkBrakeSetId =
            AvionicsBusBoolDataIds.V32NN_Infrequent_Brake_Sync_ParkBrakeSet;

        private const AvionicsBusFloatDataIds BrakePedalInputId =
            AvionicsBusFloatDataIds.V32NN_Frequent_Brake_Sync_PedalInput;

        private const AvionicsBusFloatDataIds AutoBrakeInputId =
            AvionicsBusFloatDataIds.V32NN_Frequent_Brake_Sync_AutoBrakeInput;

        private const AvionicsBusFloatDataIds FinalBrakeInput =
            AvionicsBusFloatDataIds.V32NN_Frequent_Brake_FinalBrakeInput;

        protected override void _OnAvionicsBusStart()
        {
            _WriteAndNotifyBool(ParkBrakeSetId, true);

            HasAirBrake = AirbrakeStrength != 0;
            RotMultiMaxSpeedDivider = 1 / SAVControl.RotMultiMaxSpeed;

            var localPlayer = Networking.LocalPlayer;
            if (!localPlayer.isMaster)
                gameObject.SetActive(false);
            else
            {
                gameObject.SetActive(true);
            }
        }

        protected override void _OnAvionicsBusRespawnByLocalPlayer()
        {
            _WriteFloat(BrakePedalInputId, 0);
            _WriteFloat(AutoBrakeInputId, 0);
            _WriteFloat(FinalBrakeInput, 0);
        }

        private void Update()
        {
            var deltaTime = Time.deltaTime;

            var pedalInput = _ReadFloat(BrakePedalInputId);
            var autoBrakeInput = _ReadFloat(AutoBrakeInputId);
            var brakeInput = Mathf.Max(pedalInput, autoBrakeInput);
            _WriteFloat(FinalBrakeInput, brakeInput);

            if (SAVControl.IsOwner)
            {
                if (SAVControl.Piloting)
                {
                    var isAircraftGrounded = SAVControl.Taxiing;

                    if (!HasAirBrake && !isAircraftGrounded) brakeInput = 0;
                    //remove the drag added last frame to add the new value for this frame
                    var extradrag = SAVControl.ExtraDrag;
                    var newdrag = AirbrakeStrength * brakeInput;
                    var dragtoadd = -LastDrag + newdrag;
                    extradrag += dragtoadd;
                    LastDrag = newdrag;
                    SAVControl.ExtraDrag = extradrag;

                    //send events to other users to tell them to enable the script so they can see the animation
                    Braking = brakeInput > .02f;
                    if (Braking)
                    {
                        if (!BrakingLastFrame)
                        {
                            if (Airbrake_snd && !Airbrake_snd.isPlaying) Airbrake_snd.Play();
                            SendCustomNetworkEvent(NetworkEventTarget.Others, nameof(EnableForAnimation));
                        }

                        if (Time.time > NextUpdateTime)
                        {
                            NextUpdateTime = Time.time + .4f;
                        }
                    }

                    if (AirbrakeLerper < .03 && brakeInput < .03)
                        if (Airbrake_snd && Airbrake_snd.isPlaying)
                            Airbrake_snd.Stop();
                    BrakingLastFrame = Braking;
                }
            }
            else
            {
                //this object is enabled for non-owners only while animating
                NonLocalActiveDelay -= deltaTime;
                if (NonLocalActiveDelay < 0 && AirbrakeLerper < 0.01)
                {
                    DisableForAnimation();
                    return;
                }
            }

            AirbrakeLerper = Mathf.Lerp(AirbrakeLerper, brakeInput, 2f * deltaTime);
            BrakeAnimator.SetFloat(BRAKE_STRING, AirbrakeLerper);
            if (Airbrake_snd)
            {
                var ias = _ReadFloat(AvionicsBusFloatDataIds.Sim_Frequent_IndicatedAirSpeedInMeter)
                          / UnitConverterUtils.MetersPerSecondToKnots;
                Airbrake_snd.pitch = AirbrakeLerper * .2f + .9f;
                Airbrake_snd.volume = AirbrakeLerper * Mathf.Min(ias * RotMultiMaxSpeedDivider, 1);
            }
        }

        public void SFEXT_O_TakeOwnership()
        {
            gameObject.SetActive(true);
        }

        public void SFEXT_O_LoseOwnership()
        {
            gameObject.SetActive(false);
        }

        public void SFEXT_O_PilotExit()
        {
            _WriteFloat(BrakePedalInputId, 0);
            _WriteFloat(AutoBrakeInputId, 0);
            _WriteFloat(FinalBrakeInput, 0);
            if (NoPilotAlwaysParkBrake) _WriteAndNotifyBool(ParkBrakeSetId, true);
        }

        public void EnableForAnimation()
        {
            if (Airbrake_snd) Airbrake_snd.Play();
            gameObject.SetActive(true);
            NonLocalActiveDelay = 3;
        }

        public void DisableForAnimation()
        {
            BrakeAnimator.SetFloat(BRAKE_STRING, 0);
            AirbrakeLerper = 0;
            if (Airbrake_snd)
            {
                Airbrake_snd.pitch = 0;
                Airbrake_snd.volume = 0;
            }

            gameObject.SetActive(false);
        }

        [PublicAPI]
        public void _ToggleParkBrake()
        {
            var isParkBrakeSet = _ReadBool(ParkBrakeSetId);
            _WriteAndNotifyBool(ParkBrakeSetId, !isParkBrakeSet);
        }
    }
}