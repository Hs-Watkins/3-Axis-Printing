using System;
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
        // Device IDs //
        static string serialNoX = "27505282"; // x
        static string serialNoY = "27505360"; // y
        static string serialNoZ = "27505370"; // z
        // Device IDs //

        // Path // beware, the x actuator seems to give up near 0, I recommend starting at (10, 10)
        static double[] XpositionArray = { 10, 10, 11, 11, 12, 12, 13, 13, 14, 14 };
        static double[] YpositionArray = { 10, 20, 20, 10, 10, 20, 20, 10, 10, 20 };
        static double[] ZpositionArray = { 4.2, 4.2, 4.2, 4.2, 4.2, 4.2, 4.2, 4.2, 4.2, 4.2 };
        static byte[] PValuesArray = { 0, 1, 0, 1, 0, 1, 0, 1, 0, 1 };
        static double[] VelocityArray = { 2.0, 2.0, 2.0, 2.0, 2.0, 2.0, 2.0, 2.0, 2.0, 2.0 };
        // Path //

        static void Main(string[] args)
        {
            // Step 1. Ensure that all of the arrays defining motion are the same length.
            Console.WriteLine("Checking path vectors. All path vectors must have the same length.");
            try { CheckArrayLengths(XpositionArray, YpositionArray, ZpositionArray, PValuesArray, VelocityArray); }
            catch (ArgumentException e)
            {
                Debug.WriteLine($"Error: {e.Message}");
                Environment.Exit(1);
            }

            // Step 2. Initialize the devices.
            // Arduino Nano
            SerialPort port = new SerialPort("COM3", 9600, Parity.None, 8, StopBits.One);
            port.Open();
            // Device Manager 
            DeviceManagerCLI.BuildDeviceList();
            // X axis
            KCubeDCServo deviceX = KCubeDCServo.CreateKCubeDCServo(serialNoX);
            Console.WriteLine("Opening device {0}", serialNoX);
            deviceX.Connect(serialNoX); // establish connection
            deviceX.WaitForSettingsInitialized(5000); // if the device takes longer than 5s to init, give up
            deviceX.StartPolling(250); // polling every 250ms
            deviceX.EnableDevice(); // should enable the device, physically (feedback on the screen)
                                    // Y axis
            KCubeDCServo deviceY = KCubeDCServo.CreateKCubeDCServo(serialNoY);
            Console.WriteLine("Opening device {0}", serialNoY);
            deviceY.Connect(serialNoY);
            deviceY.WaitForSettingsInitialized(5000);
            deviceY.StartPolling(250);
            deviceY.EnableDevice();
            // Z axis
            KCubeDCServo deviceZ = KCubeDCServo.CreateKCubeDCServo(serialNoZ);
            Console.WriteLine("Opening device {0}", serialNoZ);
            deviceZ.Connect(serialNoZ);
            deviceZ.WaitForSettingsInitialized(5000);
            deviceZ.StartPolling(250);
            deviceZ.EnableDevice();
            MotorConfiguration motorSettingsX = deviceX.LoadMotorConfiguration(deviceX.DeviceID, DeviceConfiguration.DeviceSettingsUseOptionType.UseFileSettings);
            MotorConfiguration motorSettingsY = deviceY.LoadMotorConfiguration(deviceY.DeviceID, DeviceConfiguration.DeviceSettingsUseOptionType.UseFileSettings);
            MotorConfiguration motorSettingsZ = deviceZ.LoadMotorConfiguration(deviceZ.DeviceID, DeviceConfiguration.DeviceSettingsUseOptionType.UseFileSettings);
            Thread.Sleep(500);
            Console.WriteLine("Devices Enabled");

            // Step 3. Home the actuators.
            // The homing position collides with the slide holder.
            Console.WriteLine("Is the kinematic mount removed?");
            Console.WriteLine("Press enter to continue.");
            string answer1 = Console.ReadLine();
            Console.WriteLine("Homing...");
            // Thread the homing, so it can happen simultaneously.
            Thread Home1Thread = new Thread(() => Home(deviceX));
            Thread Home2Thread = new Thread(() => Home(deviceY));
            Thread Home3Thread = new Thread(() => Home(deviceZ));
            Home1Thread.Start();
            Home2Thread.Start();
            Home3Thread.Start();
            Home1Thread.Join();
            Home2Thread.Join();
            Home3Thread.Join();
            Console.WriteLine("Homed.");
            // Move up to allow the user to replace the stage.
            Thread MoveZThreadInit = new Thread(() => Move(deviceZ, 20, 2));
            MoveZThreadInit.Start();
            MoveZThreadInit.Join();
            Console.WriteLine("Is the power supply is on?");
            Console.WriteLine("Is the kinematic mount in place?");
            Console.WriteLine("Press enter to continue.");
            string answer2 = Console.ReadLine();
            Console.WriteLine("Printing...");
            Thread ReturnZThreadInit = new Thread(() => Move(deviceZ, ZpositionArray[0], 2));
            ReturnZThreadInit.Start();
            ReturnZThreadInit.Join();

            // Step 4. Printing.
            for (int i = 0; i < XpositionArray.Length; i++)
            {
                // calculate displacements
                double xDel = Math.Abs(XpositionArray[i] - (double)deviceX.Position);
                double yDel = Math.Abs(YpositionArray[i] - (double)deviceY.Position);
                double zDel = Math.Abs(ZpositionArray[i] - (double)deviceZ.Position);
                double totalD = Math.Sqrt(xDel * xDel + yDel * yDel + zDel * zDel);
                // calculate velocities
                double velocity = VelocityArray[i];
                double xVel = (xDel / totalD) * velocity;
                double yVel = (yDel / totalD) * velocity;
                double zVel = (zDel / totalD) * velocity;
                // sending 0 is illegal. pick an arbitrary velocity (it won't move anyway)
                if (xVel == 0) { xVel = 0.4; }
                if (yVel == 0) { yVel = 0.4; }
                if (zVel == 0) { zVel = 0.4; }
                // setup the moving threads
                Thread MoveXThread = new Thread(() => Move(deviceX, XpositionArray[i], xVel));
                Thread MoveYThread = new Thread(() => Move(deviceY, YpositionArray[i], yVel));
                Thread MoveZThread = new Thread(() => Move(deviceZ, ZpositionArray[i], zVel));
                // toggle the laser
                port.Write(PValuesArray[i].ToString());
                // it's moving time
                Console.WriteLine($"Move {i} Executing\n=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=");
                Console.WriteLine($"X: from {deviceX.Position:0.####} to {XpositionArray[i]:0.####} at {xVel:0.####}");
                Console.WriteLine($"Y: from {deviceY.Position:0.####} to {YpositionArray[i]:0.####} at {yVel:0.####}");
                Console.WriteLine($"Z: from {deviceZ.Position:0.####} to {ZpositionArray[i]:0.####} at {zVel:0.####}");
                Console.WriteLine($"Total Vel: {VelocityArray[i]}");
                Console.WriteLine($"Laser: {PValuesArray[i]}");
                MoveXThread.Start();
                MoveYThread.Start();
                MoveZThread.Start();
                // Wait for Move to Finish
                MoveXThread.Join();
                MoveYThread.Join();
                MoveZThread.Join();
                Console.WriteLine("");
            }

            // Step 5. Shutdown.
            port.Write("0");
            port.Close();
            Console.WriteLine("Laser Off");

            // Raise head to allow you to remove slide
            Thread MoveZThreadEnd = new Thread(() => Move(deviceZ, 20, 2));
            MoveZThreadEnd.Start();
            MoveZThreadEnd.Join();

            //Closing the Devices
            //Stop polling devices
            deviceX.StopPolling();
            deviceY.StopPolling();
            deviceZ.StopPolling();

            // Shut down controller using Disconnect() to close comms
            deviceX.ShutDown();
            deviceY.ShutDown();
            deviceZ.ShutDown();

            Console.WriteLine("Your print is finished. Press any key to exit");
            Console.ReadKey();
        }

        static void Move(KCubeDCServo device, double pos, double velo)
        {
            device.SetVelocityParams(acceleration: 3, maxVelocity: (decimal)velo);
            device.MoveTo((decimal)pos, 200000);
        }

        static void Home(KCubeDCServo device)
        {
            device.Home(60000);
        }

        static void CheckArrayLengths(params Array[] arrays)
        {
            int length = arrays[0].Length;
            for (int i = 1; i < arrays.Length; i++)
            {
                if (arrays[i].Length != length) { throw new ArgumentException("Arrays must have the same length."); }
            }
        }
    }
}
