using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NationalInstruments.DAQmx;

namespace tempChamberControl
{
    class DigitalO
    {
        Task digitalOut = new Task();
        DigitalSingleChannelWriter writer;

        public void OpenChannel()
        {
            // This function opens a new output channel to the NI ELVIS device.
            digitalOut.DOChannels.CreateChannel(Program.device +"/port0", "DigitalChn0", 
                ChannelLineGrouping.OneChannelForAllLines);

            writer = new DigitalSingleChannelWriter(digitalOut.Stream);
        }

        public void WriteData(int length)
        {
            // Sends data down an open channel to the NI ELVIS device.
            if (writer != null)
            {
                writer.WriteSingleSamplePort(true, (UInt32)length);
            }
        }
    }
}
