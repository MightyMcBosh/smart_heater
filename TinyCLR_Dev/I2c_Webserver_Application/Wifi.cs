
using GHIElectronics.TinyCLR.Devices.Gpio;
using GHIElectronics.TinyCLR.Devices.I2c;
using GHIElectronics.TinyCLR.Devices.Network;
using GHIElectronics.TinyCLR.Devices.Spi;
using GHIElectronics.TinyCLR.Devices.Uart;
using GHIElectronics.TinyCLR.Native;
using GHIElectronics.TinyCLR.Pins;
using System;
using System.Collections;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Resources;
using System.Text;
using System.Threading;
namespace I2c_Webserver_Application
{
    internal static class Wifi
    {
        static string _address = "0.0.0.0";
       

        public static void Initialize()
        {          

            var gpioController = GpioController.GetDefault();
           

            SetupWiFiConnection(gpioController);
        }

        private static void SetupWiFiConnection(GpioController gpioController)
        {
            var enablePin = gpioController.OpenPin(GHIElectronics.TinyCLR.Pins.SC20100.GpioPin.PA8);
            enablePin.SetDriveMode(GpioPinDriveMode.Output);
            enablePin.Write(GpioPinValue.High);
            var cs = gpioController.OpenPin(SC20100.GpioPin.PD15);
            WiFiNetworkInterfaceSettings wifiSettings = new WiFiNetworkInterfaceSettings()
            {
                Ssid = "The Green Dragon",
                Password = "BenandJaye",
                DhcpEnable = false,
                DynamicDnsEnable = false,
                DnsAddresses = new IPAddress[]
                {
                    IPAddress.Parse("8.8.8.8"),
                    IPAddress.Parse("8.8.4.4")
                },
                GatewayAddress = new IPAddress(new byte[] { 192, 168, 1, 1 }),
                SubnetMask = new IPAddress(new byte[] { 255, 255, 255, 0 }),
                Address = new IPAddress(new byte[] { 192, 168, 1, 100 }),
                TlsEntropy = new byte[] { 0, 1, 2, 3 },
                Mode = WiFiMode.Station,
            };
            var settings = new SpiConnectionSettings()
            {
                ChipSelectLine = cs,
                ClockFrequency = 4000000,
                Mode = SpiMode.Mode0,
                ChipSelectType = SpiChipSelectType.Gpio,
                ChipSelectHoldTime = TimeSpan.FromTicks(10),
                ChipSelectSetupTime = TimeSpan.FromTicks(10)
            };
            SpiNetworkCommunicationInterfaceSettings netInterfaceSettings = new SpiNetworkCommunicationInterfaceSettings()
            {
                SpiApiName = SC20100.SpiBus.Spi3,
                GpioApiName = SC20100.GpioPin.Id,
                SpiSettings = settings,
                InterruptPin = gpioController.OpenPin(SC20100.GpioPin.PB12),
                InterruptEdge = GpioPinEdge.FallingEdge,
                InterruptDriveMode = GpioPinDriveMode.InputPullUp,
                ResetPin = gpioController.OpenPin(SC20100.GpioPin.PB13),
                ResetActiveState = GpioPinValue.Low,
            };

            var networkController = NetworkController.FromName(SC20100.NetworkController.ATWinc15x0);
            networkController.SetInterfaceSettings(wifiSettings);
            networkController.SetCommunicationInterfaceSettings(netInterfaceSettings);
            networkController.SetAsDefaultController();
            byte[] address = new byte[4];


            networkController.NetworkAddressChanged += (NetworkController sender, NetworkAddressChangedEventArgs e) =>
            {
                var ipProperties = sender.GetIPProperties();
                address = ipProperties.Address.GetAddressBytes();                
                _address = $"{address[0]}.{address[1]}.{address[2]}.{address[3]}";
                Debug.WriteLine(_address);
            };


            enablePin.Toggle(); //power off
            Thread.Sleep(2000);
            enablePin.Toggle(); // power oni

            Thread.Sleep(2000);




            try
            {
                //Try enabling WiFi interface
                networkController.Enable();
                //Sleep for 2sec to give ample time fr wifi to connect
                Thread.Sleep(2000);

                int tout = 3;
                //Poll WiFi connection link every 5sec
                while (--tout > 0 && networkController.GetLinkConnected())
                {
                    Thread.Sleep(5000);
                }
                if (!networkController.GetLinkConnected())
                {
                    networkController.Disable();
                    throw new ArgumentException(); 
                }
                    
            }
            catch (Exception)
            {
                Debug.WriteLine("Unable to connect to AP");
                Thread.Sleep(2000);
            }

        }
    }
}
