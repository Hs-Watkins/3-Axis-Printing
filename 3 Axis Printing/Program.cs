﻿using System;
using System.IO.Ports;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Thorlabs.MotionControl.GenericMotorCLI;
using Thorlabs.MotionControl.GenericMotorCLI.Settings;
using Thorlabs.MotionControl.KCube.DCServoCLI;
using Thorlabs.MotionControl.DeviceManagerCLI;
using Thorlabs.MotionControl.GenericMotorCLI.AdvancedMotor;
using System.Xml.Serialization;


namespace KDC101Console
{
    class Program
    {


        // VARIABLES TO EDIT -----------------------------------------------------------------------------------------------

        // declare if simulation
        static bool SimulationTrue = true;

        // positional positions - don't start any axis on 0  
        decimal[] XpositionArray = { 0, 0, 5, 5, 10, 10, 15, 15, 20, 20, };
        decimal[] YpositionArray = { 15, 25, 25, 15, 15, 25, 25, 15, 15, 25, };
        decimal[] ZpositionArray = { 2.75m, 2.75m, 2.75m, 2.75m, 2.75m, 2.75m, 2.75m, 2.75m, 2.75m, 2.75m, };

        // power 1/0, start on 0
        // velocity cannot = 0
        byte[] PValuesArray = { 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, };
        decimal[] VelocityArray = { 2m, 2m, 1.5m, 1.5m, 1.798m, 1.798m, 0m, 0m, 0m, 0m };

        // constant values
        // velocity cannot = 0
        decimal constantVelocity = 2;
        decimal constantZPosition = 2.75m;

        //choose if you want constant values
        bool chooseConstantVelocity = false;
        bool chooseConstantZPosition = true;


        // -------------------------------------------------------------------------------------------------------


        // declare the serial port
        static SerialPort port;
        static void Main(string[] args)
        {
            // initialise simulation
            if (SimulationTrue == true)
            {
                SimulationManager.Instance.InitializeSimulations();
            }

            // initialise variables
            decimal zPosition = 1m;
            decimal velocity = 1m;
            decimal xVel = 1m;
            decimal yVel = 1m;
            decimal zVel = 1m;

            //  check to see if lengths are same
            try
            {
                if (chooseConstantVelocity && chooseConstantZPosition)
                {
                    Console.WriteLine("Both V and Z are constant");
                    Console.WriteLine("Checking X, Y, P");
                    CheckArrayLengths(XpositionArray, YpositionArray, PValuesArray);
                }
                else if (chooseConstantVelocity)
                {
                    Console.WriteLine("V is constant");
                    Console.WriteLine("Checking X, Y, Z, P");
                    CheckArrayLengths(XpositionArray, YpositionArray, ZpositionArray, PValuesArray);
                }
                else if (chooseConstantZPosition)
                {
                    Console.WriteLine("Z is constant");
                    Console.WriteLine("Checking X, Y, V, P");
                    CheckArrayLengths(XpositionArray, YpositionArray, VelocityArray, PValuesArray);
                }
                else
                {
                    Console.WriteLine("Neither V nor Z constant");
                    Console.WriteLine("Checking X, Y, Z, P, V");
                    CheckArrayLengths(XpositionArray, YpositionArray, ZpositionArray, PValuesArray, VelocityArray);
                }
                Console.WriteLine("All arrays have the same length.");
            }
            catch (ArgumentException e)
            {
                Debug.WriteLine($"Error: {e.Message}");
                Environment.Exit(1);
            }

            // Find the Devices and Begin Communicating with them via USB
            // Enter the serial number for your device

            string serialNo1 = "";
            string serialNo2 = "";
            string serialNo3 = "";

            if (SimulationTrue == true)
            {
                serialNo1 = "27000001"; // x
                serialNo2 = "27000002"; // y
                serialNo3 = "27000003"; // z
            }
            else
            {
                serialNo1 = "27505282"; // x
                serialNo2 = "27505360"; // y
                serialNo3 = "27505370"; // z
            }

            DeviceManagerCLI.BuildDeviceList();

            // Creates an instance of KCubeDCServo class, passing in the Serial number parameter.  
            KCubeDCServo device1 = KCubeDCServo.CreateKCubeDCServo(serialNo1);
            KCubeDCServo device2 = KCubeDCServo.CreateKCubeDCServo(serialNo2);
            KCubeDCServo device3 = KCubeDCServo.CreateKCubeDCServo(serialNo3);

            // We tell the user that we are opening connection to the device. 
            Console.WriteLine("Opening device {0}", serialNo1);
            Console.WriteLine("Opening device {0}", serialNo2);
            Console.WriteLine("Opening device {0}", serialNo3);

            // Connects to the device. 
            device1.Connect(serialNo1);

            // Wait for the device settings to initialize. We ask the device to 
            // throw an exception if this takes more than 5000ms (5s) to complete. 
            device1.WaitForSettingsInitialized(5000);

            // Same for Device 2 and 3
            device2.Connect(serialNo2);
            device2.WaitForSettingsInitialized(5000);
            device3.Connect(serialNo3);
            device3.WaitForSettingsInitialized(5000);

            // This calls LoadMotorConfiguration on the device to initialize the 
            // DeviceUnitConverter object required for real world unit parameters.
            MotorConfiguration motorSettings1 = device1.LoadMotorConfiguration(device1.DeviceID,
            DeviceConfiguration.DeviceSettingsUseOptionType.UseFileSettings);

            MotorConfiguration motorSettings2 = device2.LoadMotorConfiguration(device2.DeviceID,
            DeviceConfiguration.DeviceSettingsUseOptionType.UseFileSettings);

            MotorConfiguration motorSettings3 = device3.LoadMotorConfiguration(device3.DeviceID,
            DeviceConfiguration.DeviceSettingsUseOptionType.UseFileSettings);

            // This starts polling the device at intervals of 250ms (0.25s).
            device1.StartPolling(250);
            device2.StartPolling(250);
            device3.StartPolling(250);

            // We are now able to Enable the device otherwise any move is ignored. 
            // You should see a physical response from your controller. 
            device1.EnableDevice();
            device2.EnableDevice();
            device3.EnableDevice();
            Console.WriteLine("Devices Enabled");

            // Needs a delay to give time for the device to be enabled. 
            Thread.Sleep(500);
            Console.WriteLine("Is the slide removed? Press enter to continue");
            string answer1 = Console.ReadLine();
            Console.WriteLine("Now Homing");

            if (SimulationTrue == false)
            {
                // Home all actuators at once  
                Thread Home1Thread = new Thread(() => Home1(device1));
                Thread Home2Thread = new Thread(() => Home2(device2));
                Thread Home3Thread = new Thread(() => Home2(device3));
                Home1Thread.Start();
                Home2Thread.Start();
                Home3Thread.Start();

                // Wait for the homing to complete
                Home1Thread.Join();
                Home2Thread.Join();
                Home3Thread.Join();
            }


            // move z axis so you can put slide in place
            Thread MoveZThreadInit = new Thread(() => MoveZ(device3, 20, 10));
            MoveZThreadInit.Start();
            MoveZThreadInit.Join();
            Console.WriteLine("Make sure power supply is on and slide is in place.");
            Console.WriteLine("Press enter to continue");
            string answer = Console.ReadLine();
            Console.WriteLine("Now Proceeding");

            if (SimulationTrue == false)
            {
                //SerialPort port
                port = new SerialPort("COM3", 9600, Parity.None, 8, StopBits.One);
            }

            // Iterate through XPositions and YPositions Simultaneously and Synchronously
            Console.WriteLine("Actuator is Moving");

            for (int i = 0; i < XpositionArray.Length; i++)
            {

                if (chooseConstantZPosition == true)
                {
                    zPosition = constantZPosition;
                }
                else
                {
                    zPosition = ZpositionArray[i];
                }

                if (chooseConstantVelocity == true)
                {
                    velocity = constantVelocity;
                }
                else
                {
                    velocity = VelocityArray[i];
                }

                decimal xPos = Math.Abs(XpositionArray[i] - device1.Position);
                decimal yPos = Math.Abs(YpositionArray[i] - device2.Position);
                decimal zPos = Math.Abs(ZpositionArray[i] - device3.Position);

                decimal longestDist = Math.Max(xPos, Math.Max(yPos, zPos));

                xVel = (xPos / (longestDist / velocity));
                yVel = (yPos / (longestDist / velocity));
                zVel = (zPos / (longestDist / velocity));
                
                if (xVel == 0)
                {
                    xVel = 1m;
                }

                if (yVel == 0)
                {
                    yVel = 1m;
                }

                if (zVel == 0)
                {
                    zVel = 1m;
                }


                Thread MoveXThread = new Thread(() => MoveX(device1, XpositionArray[i], xVel));
                Thread MoveYThread = new Thread(() => MoveY(device2, YpositionArray[i], yVel));
                Thread MoveZThread = new Thread(() => MoveZ(device3, zPosition, zVel));

                int pValue = PValuesArray[i];
                string pSend = pValue.ToString();

                if (SimulationTrue == false)
                {
                    port.Open();
                    port.Write(pSend);
                    port.Close();
                    Console.WriteLine("Power Signal: {0}", PValuesArray[i]);
                }

                // Move the Actuators
                MoveXThread.Start();
                MoveYThread.Start();
                MoveZThread.Start();

                // Wait for Move to Finish
                MoveXThread.Join();
                MoveYThread.Join();
                MoveZThread.Join();

                Console.WriteLine("");
            }

            if (SimulationTrue == false)
            {
                // turn off laser
                port.Open();
                port.Write("0");
                port.Close();
                Console.WriteLine("Laser Off");
            }

            // Raise head to allow you to remove slide
            Thread MoveZThreadEnd = new Thread(() => MoveZ(device3, 20, 10));
            MoveZThreadEnd.Start();
            MoveZThreadEnd.Join();

            //Closing the Devices
            //Stop polling devices
            device1.StopPolling();
            device2.StopPolling();
            device3.StopPolling();

            // Shut down controller using Disconnect() to close comms
            device1.ShutDown();
            device2.ShutDown();
            device3.ShutDown();

            Console.WriteLine("Your print is finished. Press any key to exit");
            Console.ReadKey();

            if (SimulationTrue == true)
            {
                SimulationManager.Instance.UninitializeSimulations();
            }
        }
        static void MoveX(KCubeDCServo device1, decimal Xposition, decimal Velocities)
        {
            device1.SetVelocityParams(acceleration: 100, maxVelocity: Velocities);
            device1.MoveTo(Xposition, 200000);
            Console.WriteLine("Input X velocity: {0}", Velocities);
            Console.WriteLine("Final X position: {0}", device1.Position);
        }
        static void MoveY(KCubeDCServo device2, decimal Yposition, decimal Velocities)
        {
            device2.SetVelocityParams(acceleration: 100, maxVelocity: Velocities);
            device2.MoveTo(Yposition, 200000);
            Console.WriteLine("Input Y velocity: {0}", Velocities);
            Console.WriteLine("Final Y position: {0}", device2.Position);
        }
        static void MoveZ(KCubeDCServo device3, decimal Zposition, decimal Velocities)
        {
            device3.SetVelocityParams(acceleration: 100, maxVelocity: Velocities);
            device3.MoveTo(Zposition, 200000);
            Console.WriteLine("Input Z velocity: {0}", Velocities);
            Console.WriteLine("Final Z position: {0}", device3.Position);
        }

        static void Home1(KCubeDCServo device1)
        {
            device1.Home(60000);
        }
        static void Home2(KCubeDCServo device2)
        {
            device2.Home(60000);
        }
        static void Home3(KCubeDCServo device3)
        {
            device3.Home(60000);
        }

        static void CheckArrayLengths(params Array[] arrays)
        {
            int length = arrays[0].Length;
            for (int i = 1; i < arrays.Length; i++)
            {
                if (arrays[i].Length != length)
                {
                    throw new ArgumentException("Arrays must have the same length.");
                }
            }
        }
    }
}
