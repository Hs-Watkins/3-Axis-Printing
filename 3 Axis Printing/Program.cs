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
        // declare the serial port
        static SerialPort port;
        static void Main(string[] args)
        {
            // Find the Devices and Begin Communicating with them via USB
            // Enter the serial number for your device
            string serialNo1 = "27505282"; // x
            string serialNo2 = "27505360"; // y
            string serialNo3 = "27505370"; // z

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
            Console.WriteLine("Is the slide removed?");
            string answer1 = Console.ReadLine();
            Console.WriteLine("Now Homing");

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

            // move z axis so you can put slide in place
            Thread MoveZThreadInit = new Thread(() => MoveZ(device3, 20,10));
            MoveZThreadInit.Start();
            MoveZThreadInit.Join();
            Console.WriteLine("Make sure power supply is on");
            Console.WriteLine("Are You Prepared?");
            string answer = Console.ReadLine();
            Console.WriteLine("Now Proceeding");

            //decimal[] Xpositions = {0, };
            //decimal[] Ypositions = {15, };
            //decimal[] Zpositions = {7.3m, };

            // positional positions - don't start any axis on 0  

            decimal[] XpositionArray = {0,0,5,5,10,10,15,15,20,20, };
            decimal[] YpositionArray = {15,25,25,15,15,25,25,15,15,25, };
            decimal[] ZpositionArray = {2.75m, 2.75m, 2.75m, 2.75m, 2.75m, 2.75m, 2.75m, 2.75m, 2.75m, 2.75m, };

            // power 1/0, start on 0
            byte[] PValuesArray = { 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, };
            int[] VelocityArray = { 5, 5, 10, 10, 15, 15, 20, 20, 20,20,  };

            // constant values
            int constantVelocity = 5;
            decimal constantZPosition = 2.75m;

            // initialise variables
            decimal zPosition = constantZPosition;
            int velocity = constantVelocity;

            //choose if you want constant values
            bool chooseConstantVelocity = false;
            bool chooseConstantZPosition = false;

            // add check to see if lengths are same


            //SerialPort port
            port = new SerialPort("COM3", 9600, Parity.None, 8, StopBits.One);

            // Iterate through XPositions and YPositions Simultaneously and Synchronously
            Console.WriteLine("Actuator is Moving");

            for (int i = 0; i < XpositionArray.Length; i++)
            {
                
                if (chooseConstantZPosition==true){
                    zPosition=constantZPosition;
                }
                else
                {
                    zPosition = ZpositionArray[i];
                }

                if (chooseConstantVelocity==true){
                    velocity=constantVelocity;
                }
                else
                {
                    velocity=VelocityArray[i];
                }
                

                Thread MoveXThread = new Thread(() => MoveX(device1, XpositionArray[i], velocity));
                Thread MoveYThread = new Thread(() => MoveY(device2, YpositionArray[i], velocity));
                Thread MoveZThread = new Thread(() => MoveZ(device3, zPosition, velocity));

                int pValue = PValuesArray[i];
                string pSend = pValue.ToString();

                port.Open();
                port.Write(pSend);
                port.Close();
                Console.WriteLine("Power Signal: {0}", PValuesArray[i]);

                // Move the Actuators
                MoveXThread.Start();
                MoveYThread.Start();
                MoveZThread.Start();

                // Wait for Move to Finish
                MoveXThread.Join();
                MoveYThread.Join();
                MoveZThread.Join();
            }

            // turn off laser
            port.Open();
            port.Write("0");
            port.Close();
            Console.WriteLine("Laser Off");

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
