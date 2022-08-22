using System;
using System.Collections;
using System.Text;
using System.Threading;
using System.IO;
using System.Net; 
using GHIElectronics.TinyCLR.Devices.Network;
using GHIElectronics.TinyCLR.Devices.Gpio;
using GHIElectronics.TinyCLR.Networking;
using GHIElectronics.TinyCLR.Pins;
using System.Diagnostics;

namespace I2c_Webserver_Application
{
    internal class WebServer
    {
        HttpListener webServer;
        Thread webWorkerThread;
         GpioPin led;


        AHT20 sensor;
        public WebServer(AHT20 sensor)
        {
            this.sensor = sensor; 
            webServer = new HttpListener("http", 80);
            
            //this handles incoming message requests
            webWorkerThread = new Thread(() =>
            {
                while (webServer.IsListening)
                {
                    var context = webServer.GetContext();
                    ProcessInboundGetRequest(context);
                    Thread.Sleep(100); 
                }
            });



            var gpioController = GpioController.GetDefault();
            led = gpioController.OpenPin(SC20100.GpioPin.PE11);
            led.SetDriveMode(GpioPinDriveMode.Output);

            webServer.Start();
            webWorkerThread.Start(); 
        }


        private void ProcessInboundGetRequest(HttpListenerContext context)
        {
            try
            {
                switch (context.Request.HttpMethod.ToUpper())
                {
                    case "POST":
                        switch (context.Request.RawUrl.ToUpper())
                        {
                            case "/":
                                led.Write(led.Read() == GpioPinValue.High ? GpioPinValue.Low : GpioPinValue.High);
                                context.Response.StatusCode = 200;
                                context.Response.ContentType = "application/json";
                                string s = $"{{\n" +
                                    $"\"led\": {(led.Read() == GpioPinValue.High).ToString().ToLower()}\n" +
                                    $"\"temperature\": {sensor.Temperature.ToString("D2")}\n" +
                                    $"\"humidity\": {sensor.Humidity.ToString("D2")}\n" +
                                    "}"
                                    ;

                                Debug.WriteLine(s); 
                                var buffer = Encoding.UTF8.GetBytes($"{{\"led\": {(led.Read() == GpioPinValue.High).ToString().ToLower()}}}");
                                context.Response.ContentLength64 = buffer.Length;
                                context.Response.OutputStream.Write(buffer, 0, buffer.Length);

                                break;
                            default:
                                break;
                        }
                        break;
                    case "GET":
                        {
                            switch (context.Request.RawUrl.ToUpper())
                            {
                                case "/DATA":
                                    //led.Write(led.Read() == GpioPinValue.High ? GpioPinValue.Low : GpioPinValue.High);
                                    context.Response.StatusCode = 200;
                                    context.Response.ContentType = "application/json";
                                    string s = $"{{\n" +
                                        $"\"temperature\": {sensor.Temperature.ToString("D2")},\n" +
                                        $"\"humidity\": {sensor.Humidity.ToString("D2")}\n" +
                                        "}"
                                        ;

                                    Debug.WriteLine(s);
                                    var buffer = Encoding.UTF8.GetBytes(s);
                                    context.Response.ContentLength64 = buffer.Length;
                                    context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                                    break;

                                case "/":
                                    var resource = Resources.GetString(Resources.StringResources.WebPage);
                                    context.Response.StatusCode = 200;
                                    context.Response.ContentType = "text/html";
                                    buffer = Encoding.UTF8.GetBytes(resource);
                                    context.Response.ContentLength64 = buffer.Length;
                                    context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                                    break;

                            }
                            

                        }
                        break; 
                    default:
                        switch (context.Request.RawUrl.ToUpper())
                        {
                            case "/":
                                var resource = Resources.GetString(Resources.StringResources.WebPage);
                                context.Response.StatusCode = 200;
                                context.Response.ContentType = "text/html";
                                var buffer = Encoding.UTF8.GetBytes(resource);
                                context.Response.ContentLength64 = buffer.Length;
                                context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                                break;
                            default:
                                break;
                        }
                        break;
                }

            }
            finally
            {
                context.Response.OutputStream.Close();
            }
        }
    }
}

