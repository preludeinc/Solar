namespace Solar
{
    // This class contains methods to display the Solar and Battery Voltage, Battery and LED Current(s) within their respective text-boxes
    class SolarCalc
    {
        private static double ResistorValue;
        private static double Vref;

        public SolarCalc()
        {
            ResistorValue = 100.0;
            Vref = 4.73;                                                    // measured reference voltage on the Arduino

        }
        public string GetSolarVoltage(int AN0)
        {
            // (analogValue * Vref) divided by the resolution of the ADC (the number steps)
            double Analog0 = AN0 * (Vref / 1024.0);
            return Analog0.ToString("0.000");                                // string is formatted to three decimal places
        }

        public string GetBatteryVoltage(int AN2)
        {
            double Analog2 = AN2 * (Vref / 1024);
            return Analog2.ToString("0.000");
        }

        public string GetBatteryCurrent(int AN1, int shuntResistor)
        {
            int shuntAnalog = AN1 - shuntResistor;                          // the resistors operate as current limiting resistors and shunt resistors
            double shuntVoltage = shuntAnalog * Vref / 1024.0;              // (shuntAnalog * Vref) divided by the number of ADC steps
            double battCurrent = (shuntVoltage / ResistorValue) * 1000.0;   // Ohm's law is applied to find current here (multiplied by 1000 to convert to mA)
            return battCurrent.ToString("0.000");                           // string is formatted to four decimal places
        }

        public string GetLEDCurrent(int AN1, int LEDAnalog)
        {
            int shuntAnalog = AN1 - LEDAnalog;
            double shuntVoltage = shuntAnalog * Vref / 1024.0;
            double LEDCurrent = (shuntVoltage / ResistorValue) * 1000.0;

            // this eliminates noise in the system
            if (LEDCurrent < 0.09)                                        // if LED current is very small, set the value equal to 0 and return 0
            {
                LEDCurrent = 0;
            }
            return LEDCurrent.ToString("0.000");
        }
    }
}
