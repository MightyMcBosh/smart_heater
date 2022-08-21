using System;
using System.Collections;
using System.Text;
using System.Threading;
using System.Diagnostics;
using GHIElectronics.TinyCLR.Devices.I2c;
using GHIElectronics.TinyCLR.Devices.Gpio;



//#define AHT_TASK_SIZE 4096

//#define AHT_TASK_PRIORITY 3
//#define AHT_ADDR 0x38
///// PUlled from Adafruit drivers
//#define AHTX0_CMD_CALIBRATE 0xE1     ///< Calibration command
//#define AHTX0_CMD_TRIGGER 0xAC       ///< Trigger reading command
//#define AHTX0_CMD_0 0x33
//#define AHTX0_CMD_1 0x00
//#define AHTX0_CMD_SOFTRESET 0xBA     ///< Soft reset command
//#define AHTX0_STATUS_BUSY 0x80       ///< Status bit for busy
//#define AHTX0_STATUS_CALIBRATED 0x08 ///< Status bit for calibrated


/*
 *
 * 	Large amount of code adapted from the I2C Driver for the Adafruit AHT10 / AHT20 Humidity and Temperature
 *Sensor library
 *
 * 	This is a library for the Adafruit AHT20 breakout:
 * 	https://www.adafruit.com/products/4566
 *
 * 	Adafruit invests time and resources providing this open source code,
 *  please support Adafruit and open-source hardware by purchasing products from
 * 	Adafruit!
 *
 *
 *	BSD license (see license.txt)
 */



///comments pulled from my C code
///


namespace I2c_Webserver_Application
{

    public class AHT20
    {

        //i2c parameters
        public const byte ADDR = 0x38;
        public const byte CMD_INIT = 0xBE; 
        public const byte CMD_GET_STATUS = 0x71; 
        public const byte CMD_CALIBRATE = 0xE1;
        public const byte CMD_TRIGGER = 0xAC;
        public const byte CMD_0 = 0x33;
        public const byte CMD_1 = 0x00;
        public const byte SOFT_RESET = 0xBA;
        public const byte STATUS_BUSY = 0x80;
        public const byte STATUS_CALIBRATED = 0x08;
        public const byte READ = 0x00;
        public const byte WRITE = 0x01;


        private Thread _thread;
        private byte[] _status = new byte[1];
        private byte[] _data = new byte[6];

        bool _needsCal => ((_status[0] >> 3) & 0xFF) == 0;
        bool _busy => ((_status[0] >> 7) & 0xFF) == 1;

        private I2cDevice _device;
        private I2cController _controller;

        //reports temperature in Fahrenheit.
        public double Temperature { get; private set; }
        public double Humidity { get; private set; }

        /// <summary>
        /// builds logical AHT20 driver object
        /// </summary>
        /// <param name="i2c"></param>
        public AHT20(I2cController i2c, bool autoStart)
        {
            _controller = i2c;
            var settings = new I2cConnectionSettings(ADDR, I2cMode.Master, I2cAddressFormat.SevenBit);
            _device = _controller.GetDevice(settings); 

            if(autoStart)
            {
                Thread.Sleep(40); 
                Initialize();
                Thread.Sleep(40);
                Start(); 
            }
        }


        /// <summary>
        /// the constructor builds the driver in software, this sends the initilization command sequence to the AHT20 sensor
        /// </summary>
        public void Initialize()
        {
            ///Send commands
            ///
            _status[0] = 0x00;
            _device.Write(new byte[] { SOFT_RESET });
            _device.Write(new byte[] {CMD_INIT});
            _device.WriteRead(new byte[] { CMD_GET_STATUS }, _status);

            if (_needsCal)
            {
                Debug.WriteLine("Needs Calibration");
                //not sure how we're supposed to calibrated the damned thing, doesn't say in the datasheet
                // no explanation as to what these numbers actually do
                _device.Write(new byte[] { CMD_CALIBRATE,0x08,0x00 });
                
                while(!getStatusBusy())
                {
                    Thread.Sleep(20); 
                }

                _device.WriteRead(new byte[] { CMD_GET_STATUS }, _status);

                if (!_needsCal)
                {
                    Debug.WriteLine("Calibration Success");
                }

            }
              

            Debug.WriteLine($"Received status  - {_status[0]}"); 
        }


        /// <summary>
        /// if the thread is already running, do nothing. 
        /// 
        /// if the thread isn't running, start it. 
        /// </summary>
        public void Start()
        {
            if (_thread != null)
            {
                return;
            }

            _thread = new Thread(runSensor);
            _thread.Start(); 
        }



        /// <summary>
        /// Private helper methods
        /// </summary>
        private void runSensor()
        {
            while(true)
            {
                readSensor(); 
                Thread.Sleep(20); 
            }
        }






        byte[] tempCommand = new byte[] { CMD_TRIGGER, CMD_0, CMD_1 };
        /// <summary>
        /// helper method in case we ever need to call it outside of the main loop
        /// </summary>
        private void readSensor()
        {
            uint TempH, TempT;
            _device.Write(tempCommand);
            bool read_ok = false;
            while (!read_ok)
            {
                Thread.Sleep(80);

                _device.WriteRead(new byte[] { CMD_GET_STATUS }, _status);
                read_ok = !_busy; 
            }

            _device.Read(_data);
            _status[0] = _data[0];


            ////borrowed this from the Adafruit libraries, wasn't sure how they ordered the bits - the data sheet wassnt clear; 
            TempH = _data[1];
            TempH <<= 8;
            TempH |= _data[2];
            TempH <<= 4;
            TempH |= ((uint)_data[3] >> 4);

            Humidity = ((double)TempH * 100) / 0x100000;


            TempT = (uint)_data[3] & 0x0F;
            TempT <<= 8;
            TempT |= _data[4];
            TempT <<= 8;
            TempT |= _data[5];


            Temperature = ((double)TempT * 200 / 0x100000) - 50;
        }

        bool getStatusBusy()
        {
            _device.WriteRead(new byte[] { CMD_GET_STATUS }, _status);
             return (_status[0] & STATUS_BUSY) != 0;
        }
    }
}
