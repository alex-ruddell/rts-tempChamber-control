using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NationalInstruments.DAQmx;
using NationalInstruments;

namespace tempChamberControl
{
    class AnalogI
    {
        public bool enabled = true;
        Task analogIn = new Task();
        public string channelText;
        
        AnalogSingleChannelReader reader;
        NationalInstruments.AnalogWaveform<double> data;
        int samplesPerChannel = 2;
        
        public void OpenChannel(string device, string channel)
        {
            // Opens an analogue input channel to an NI ELVIS thermocouple device.
            channelText = channel;
            analogIn.AIChannels.CreateVoltageChannel(device, channel
                ,
                AITerminalConfiguration.Rse,
                -10.0, 10.0,
                AIVoltageUnits.Volts);

            analogIn.Timing.ConfigureSampleClock("",
                100.0,
                SampleClockActiveEdge.Rising,
                SampleQuantityMode.FiniteSamples,
                samplesPerChannel);

            reader = new AnalogSingleChannelReader(analogIn.Stream);
        }

        public double ReadTemperature()
        {
            // Reads data from the analogue input channel.
            data = reader.ReadWaveform(samplesPerChannel);
            // Converts data into a filtered voltage.
            double voltage = Program.filterVoltage(data.Samples[1].Value);
            // Calculates temperature based on voltage and set constants.
            double B = 0.0;
            double R0 = 0.0;
            double T0 = 298.15;
            if (channelText == "Ainport0")
            {
                B = 3380;
                R0 = 10000;
            }
            if (channelText == "Ainport1")
            {
                B = 4380;
                R0 = 100000;
            }
            if (channelText == "Ainport2")
            {
                B = 3960;
                R0 = 5000;
            }
            double R = (R0 * voltage) / (5.0 - voltage);
            double temp = (T0 * B) / ((T0 * Math.Log(R / R0)) + B);
            // Returns the temperature in degrees centigrade.
            return temp - 273.15;
        }

        public void Enable(bool input)
        {
            // Setter to enable the input channel thermistor.
            enabled = input;
        }

        public bool IsEnabled()
        {
            // Getter to return the state of the input channel thermistor.
            return enabled;
        }
    }
}
