using SaccFlightAndVehicles;
using UdonSharp;

namespace VAU.V320NeoNext.Runtime.Bus.Adapter
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public sealed class AvionicsBusSaccAirVehicleAdapter : AbstractAvionicsBusClient
    {
        // ReSharper disable once InconsistentNaming
        public SaccAirVehicle SAVControl;

        private void FixedUpdate()
        {
            _WriteFloat(AvionicsBusFloatDataIds.Sim_Frequent_IndicatedAirSpeedInMeter, SAVControl.AirSpeed);
            _WriteBool(AvionicsBusBoolDataIds.Sim_Frequent_Grounded, SAVControl.Taxiing);
            _WriteFloat(AvionicsBusFloatDataIds.V32NN_Frequent_Engine_Both_ThrustLever, SAVControl.ThrottleInput);
        }
    }
}