namespace VAU.V320NeoNext.Runtime
{
    public static class AircraftScriptsExecutionOrder
    {
        public const int AvionicsBus = -100;
        public const int SimplePilotInputProvider = 0;
        public const int SimplePilotInputMixer = 1;
        public const int SimplePilotInputWriteToAvionicsBusActuator = 2;
        public const int FlightControlComputer = 3;
        public const int SaccJoystickOverridenActuator = 4;
        // https://github.com/Sacchan-VRC/SaccFlightAndVehicles/blob/8578813e3627b39f2f564684e865be940c6947d3/Scripts/SaccAirVehicle/SaccAirVehicle.cs#L11
        // From 1.8.1
        // ReSharper disable once UnusedMember.Global
        public const int SaccAirVehicle = 5;
        public const int Adr = 6;
        public const int Fac = 7;
    }
}