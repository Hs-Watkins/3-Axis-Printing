using System;
using System.IO.Ports;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Thorlabs.MotionControl.GenericMotorCLI;
using Thorlabs.MotionControl.GenericMotorCLI.Settings;
using Thorlabs.MotionControl.KCube.DCServoCLI;
using Thorlabs.MotionControl.DeviceManagerCLI;


namespace KDC101Console
{
    class Program
    {
        SerialPort port;
        static void Main(string[] args)
        {
            // Find the Devices and Begin Communicating with them via USB
            // Enter the serial number for your device
            string serialNo1 = "27505282";
            string serialNo2 = "27505360";
            string serialNo3 = "27505370";



            DeviceManagerCLI.BuildDeviceList();



            // This creates an instance of KCubeDCServo class, passing in the Serial 
            //Number parameter.  
            KCubeDCServo device1 = KCubeDCServo.CreateKCubeDCServo(serialNo1);
            KCubeDCServo device2 = KCubeDCServo.CreateKCubeDCServo(serialNo2);
            KCubeDCServo device3 = KCubeDCServo.CreateKCubeDCServo(serialNo3);



            // We tell the user that we are opening connection to the device. 
            Console.WriteLine("Opening device {0}", serialNo1);
            Console.WriteLine("Opening device {0}", serialNo2);
            Console.WriteLine("Opening device {0}", serialNo3);



            // This connects to the device. 
            device1.Connect(serialNo1);



            // Wait for the device settings to initialize. We ask the device to 
            // throw an exception if this takes more than 5000ms (5s) to complete. 
            device1.WaitForSettingsInitialized(5000);



            // Same for Device 2.
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



            // Home both actuators at once  
            Thread Home1Thread = new Thread(() => Home1(device1));
            Thread Home2Thread = new Thread(() => Home2(device2));
            Thread Home3Thread = new Thread(() => Home2(device3));
            Console.WriteLine("Actuators are Homing");
            Home1Thread.Start();
            Home2Thread.Start();
            Home3Thread.Start();



            // Wait for the threads to complete
            Home1Thread.Join();
            Home2Thread.Join();
            Home3Thread.Join();



            //device1.SetJogVelocityParams();



            // What are the Points we want to hit?
            // decimal[] Xpositions = { 8.973m, 8.864m, 8.551m, 8.067m, 7.464m, 6.808m, 6.169m, 5.618m, 5.214m, 5m, 5m, 5.214m, 5.618m, 6.169m, 6.808m, 7.464m, 8.067m, 8.551m, 8.864m, 8.973m };
            // decimal[] Ypositions = { 1.993m, 2.643m, 3.222m, 3.668m, 3.932m, 3.986m, 3.825m, 3.465m, 2.945m, 2.322m, 1.664m, 1.041m, 0.522m, 0.162m, 0m, 0.054m, 0.319m, 0.765m, 1.344m, 1.993m };

            Console.WriteLine("Are You Prepared?");
            string answer = Console.ReadLine();
            Console.WriteLine("Now Proceeding");

            decimal[] Xpositions = { 0, 25m, 25m, 0, 0, 25m, 25m, 0, 0, 25m, 25m, 0, 0, 25m, 25m, };
            decimal[] Ypositions = { 25m, 25m, 0, 0, 25m, 25m, 0, 0, 25m, 25m, 0, 0, 25m, 25m, 0, };
            decimal[] Zpositions = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, };
            byte[] PValues = { 0, 50, 250, 200, 50, 50, 50, 50, 50, 50, 50, 50, 50, 50, 50, };
            int[] VCMValues = { 0, 50, 50, 50, 50, 50, 50, 50, 50, 50, 50, 50, 50, 50, 50, };
            int[] Velocities = { 10, 20, 30, 40, 50, 60, 70, 80, 90, 100, 110, 120, 130, 140, 150, };

            SerialPort port;
            port = new SerialPort("COM6", 9600, Parity.None, 8, StopBits.One);

            // Iterate through XPositions and YPositions Simultaneously and Synchronously
            Console.WriteLine("Actuator is Moving");
            for (int i = 0; i < Xpositions.Length; i++)
            {
                Thread MoveXThread = new Thread(() => MoveX(device1, Xpositions[i], Velocities[i]));
                Thread MoveYThread = new Thread(() => MoveY(device2, Ypositions[i], Velocities[i]));
                Thread MoveZThread = new Thread(() => MoveZ(device3, Zpositions[i], Velocities[i]));

                int firstValue = PValues[i];
                int secondValue = 0;
                int thirdValue = 0;

                if (VCMValues[i] > 0)
                {
                    secondValue = Math.Abs(VCMValues[i]);
                    thirdValue = 0;
                }

                else if (VCMValues[i] < 0)
                {
                    secondValue = 0;
                    thirdValue = Math.Abs(VCMValues[i]);
                }

                string Vals2Send = $"{firstValue},{secondValue},{thirdValue}";
                char[] charArray = Vals2Send.ToCharArray();

                port.Open();
                port.Write(Vals2Send);
                port.Close();
                Console.WriteLine("Power Signal: {0}", PValues[i]);
                Console.WriteLine("VCM Height: {0}", VCMValues[i]);

                // Move the Actuators
                MoveXThread.Start();
                MoveYThread.Start();
                MoveZThread.Start();
                //Have it raise up to the maximum z-height
                //look up C# equivalent to python's input command


                // Wait for Move to Finish
                MoveXThread.Join();
                MoveYThread.Join();
                MoveZThread.Join();
            }

            //Change the Diode Power Output
            //Make a loop that prints serial output that runs simultaneously with the motion stage
            //port = new SerialPort("COM6", 9600, Parity.None, 8, StopBits.One);
            //port.Open();
            //port.Write("Move");
            //port.Close();

            //Closing the Devices
            //Stop polling devices
            device1.StopPolling();
            device2.StopPolling();
            device3.StopPolling();



            // Shut down controller using Disconnect() to close comms
            // Then the used library
            device1.ShutDown();
            device2.ShutDown();
            device3.ShutDown();


            Console.WriteLine("Complete. Press any key to exit");
            Console.ReadKey();
        }
        static void MoveX(KCubeDCServo device1, decimal Xposition, int Velocities)
        {
            device1.SetVelocityParams(acceleration: 100, maxVelocity: Velocities);
            device1.MoveTo(Xposition, 20000);
            Console.WriteLine("Current X position: {0}", device1.Position);
        }
        static void MoveY(KCubeDCServo device2, decimal Yposition, int Velocities)
        {
            device2.SetVelocityParams(acceleration: 100, maxVelocity: Velocities);
            device2.MoveTo(Yposition, 20000);
            Console.WriteLine("Current Y position: {0}", device2.Position);
        }
        static void MoveZ(KCubeDCServo device3, decimal Zposition, int Velocities)
        {
            device3.SetVelocityParams(acceleration: 100, maxVelocity: Velocities);
            device3.MoveTo(Zposition, 20000);
            Console.WriteLine("Current Z position: {0}", device3.Position);
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
    }
}
