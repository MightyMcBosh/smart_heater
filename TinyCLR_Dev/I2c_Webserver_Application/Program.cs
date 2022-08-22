using System;
using System.Collections;
using System.Diagnostics;
using System.Text;
using System.Threading;
using GHIElectronics.TinyCLR.Devices.I2c;
using GHIElectronics.TinyCLR.Devices.Gpio;
using GHIElectronics.TinyCLR.Pins;

namespace I2c_Webserver_Application
{
    internal class Program
    {
        static void Main()
        {
            var i2c = I2cController.FromName(SC20100.I2cBus.I2c1);
            AHT20 sensor = new AHT20(i2c, true,TemperatureScale.Fahrenheit);
            Wifi.Initialize();
            var ws = new WebServer(sensor);  
            

            Thread.Sleep(2000);
            while (true)
            {
                Debug.WriteLine($"Sensor reading: Temp {sensor.Temperature} F, Humidity = {sensor.Humidity}%");
                Thread.Sleep(3000); 
            }            
        }
    }
}
