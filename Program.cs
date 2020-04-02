using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using NationalInstruments.DAQmx;
using NationalInstruments;
using System.Threading;
using System.Diagnostics;

namespace tempChamberControl
{
    static class Program
    { 
        // Instatiate input and output channels.
        public static AnalogI aIn0 = new AnalogI();
        public static AnalogI aIn1 = new AnalogI();
        public static AnalogI aIn2 = new AnalogI();
        public static DigitalO dOut = new DigitalO();
        // Initialise the device name for quick-change between machines.
        public static string device = "dev9";
        // Initialise filepaths as strings.
        public static string inputStringPath = @"H:\313 Assignment 2\tempChamberControl1\input.txt";
        public static string outputStringPath = @"H:\313 Assignment 2\tempChamberControl1\output.txt";
        // GLOBAL VARIABLE INTIALISATION
        // Initialise state monitoring variables.
        public static bool heaterOn = false;
        public static bool fanOn = false;
        public static bool automaticControlEnabled = false;
        public static bool manualControlEnabled = true; // Starts off in manual state
        // Initialise temperature storage variables
        public static double roomTemp = 0;
        public static double averageTemp = 0;
        public static double desiredTemp = 0;
        public static double therm1Temp = 0;
        public static double therm2Temp = 0;
        public static double therm3Temp = 0;
        // Initialise filter variables.
        public static int i = 0;
        public static int filterLength;
        static public double[] ringBuffer = new double[20];
        static public double[] ringWeights = new double[20];
        // Initialise control process variables.
        public static double hysteresisLowerBound = 0.1;
        public static double hysteresisUpperBound = 0.0;
        
        [STAThread]
        static void Main()
        {
            // Initial read/write of input/output files.
            setSystemFilter();
            initialiseOutputFile();
            // Open communication channels.
            aIn0.OpenChannel(device + "/ai0", "Ainport0");
            aIn1.OpenChannel(device + "/ai1", "Ainport1");
            aIn2.OpenChannel(device + "/ai2", "Ainport2");
            dOut.OpenChannel();
            // Set initial state of the temperature chamber.
            dOut.WriteData(0);
            // Initialises the base room temperature.
            calculateRoomTemperature();
            // Launches the applicatioin.
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }

        static public void controlSystem()
        {
            // Control System Functionality
            if (desiredTemp + hysteresisUpperBound < averageTemp)
            {
                // If the system is too hot, turn off the heater and turn on the fan.
                setHeater(false);
                setFan(true);
            }
            if (desiredTemp - hysteresisLowerBound > averageTemp)
            {
                // If the system is too cold, turn on the heater and turn off the fan.
                setHeater(true);
                setFan(false);
            }
            if ((desiredTemp - hysteresisLowerBound < averageTemp) && (desiredTemp + hysteresisUpperBound > averageTemp))
            {
                // If the system is inside the hysteresis zone, turn off the heater and turn off the fan.
                setHeater(false);
                setFan(false);
            }
        }
        
        static public double filterVoltage(double data)
        {
            // Voltage Filter Functionality
            // Weighted average filter is input by user through input.txt
            i++;
            // Circular buffer index calculated.
            int ringIndex = i % filterLength;
            // Store the data read by the waveform.
            ringBuffer[ringIndex] = data;
            // Weighted average functionality -- convolution.
            int currentIndex = ringIndex;
            int j = filterLength - 1;
            double sum = 0.0;
            double store = 0.0;
            double divisor = 0.0;
            while (j >= 0)
            {
                store = ringWeights[j] * ringBuffer[currentIndex];
                // If the calculated value is non-zero, add it in the weightings.
                if (store != 0)
                {
                    sum += store;
                    divisor += ringWeights[j];
                }
                // Iterate the system to the next point in the array.
                currentIndex++;
                currentIndex %= filterLength;
                j--;
            }
            // Returns the average.
            return sum / divisor;
        }

        static public void readTemperatureValues()
        {
            // Reads temperatures from the thermistor channels.
            therm1Temp = aIn0.ReadTemperature();
            therm2Temp = aIn1.ReadTemperature();
            therm3Temp = aIn2.ReadTemperature();
        }

        static public void calculateAverageTemperature()
        {
            // Calculates the average temperature based on which thermistors are deemed
            // "active" by the user.
            double sum = 0;
            double num = 0;
            if (aIn0.IsEnabled())
            {
                sum += therm1Temp;
                num++;
            }
            if (aIn1.IsEnabled())
            {
                sum += therm2Temp;
                num++;
            }
            if (aIn2.IsEnabled())
            {
                sum += therm3Temp;
                num++;
            }
            averageTemp = sum / num;
        }

        static public void calculateRoomTemperature()
        {
            // This function initialises the room temperature variable based on the initial
            // state of the system.
            readTemperatureValues();
            calculateAverageTemperature();
            roomTemp = averageTemp;
        }

        static public void calculateDesiredTemperature(double userSetPoint)
        {
            // This function calculates the target temperature set by the user.
            desiredTemp = roomTemp + userSetPoint;
        }


        static public void setSystemFilter()
        {
            // This function reads data from the input text file.
            string[] filterText = System.IO.File.ReadAllLines(inputStringPath);
            // Number of filter coefficients is equal to the number of file lines minus the number of non-coefficient lines
            int numFilterSamples = filterText.Length - 4;
            if (numFilterSamples > 20)
            {
                numFilterSamples = 20;
            }
            if (numFilterSamples < 1)
            {
                numFilterSamples = 1;
            }
            filterLength = numFilterSamples;
            // Filter coefficient weights are expected to be stored from line 5 (index 4) onwards.
            int n = 0;
            for (int k = filterLength + 3; k > 3; k--)
            {
                ringWeights[n] = Convert.ToDouble(filterText[k]);
                n++;
            }

        }
        static public void initialiseOutputFile()
        {
            // Reset the output text file and initialise the text file format. 
            System.IO.File.WriteAllText(outputStringPath, String.Empty);
            System.IO.File.AppendAllText(outputStringPath, "=========================      TEMPERATURE CHAMBER CONTROL PROCESS     =========================" + Environment.NewLine);
            System.IO.File.AppendAllText(outputStringPath, "=========================        Written by rhug194 and arud699        =========================" + Environment.NewLine);
            System.IO.File.AppendAllText(outputStringPath, "This file stores control process data per line in the form: AvgTemp<space>TargetTemp." + Environment.NewLine);
            System.IO.File.AppendAllText(outputStringPath, "Data is stored relative to the discrete time sampling period shown on the next line:" + Environment.NewLine);
            System.IO.File.AppendAllText(outputStringPath, "0.1" + Environment.NewLine);
        }

        public static void setFan(bool set)
        {
            // This function contains logic for turning the fan on and off.
            if (heaterOn)
            {
                if (set)
                {
                    dOut.WriteData(3);
                    fanOn = true;
                }
                else
                {
                    dOut.WriteData(2);
                    fanOn = false;
                }
            }
            else
            {
                if (set)
                {
                    dOut.WriteData(1);
                    fanOn = true;
                }
                else
                {
                    dOut.WriteData(0);
                    fanOn = false;
                }
            }
        }
        public static void setHeater(bool set)
        {
            // This function contains logic for turning the heater on and off.
            if (fanOn)
            {
                if (set)
                {
                    dOut.WriteData(3);
                    heaterOn = true;
                }
                else
                {
                    dOut.WriteData(1);
                    heaterOn = false;
                }
            }
            else
            {
                if (set)
                {
                    dOut.WriteData(2);
                    heaterOn = true;
                }
                else
                {
                    dOut.WriteData(0);
                    heaterOn = false;
                }
            }
        }

    }
}
