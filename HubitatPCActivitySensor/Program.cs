using System;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;

namespace HubitatPCActivitySensor
{
    static class Program
    {
        private static Config _config;
        private static DateTime _lastPulseTime = DateTime.MinValue;
        private static DateTime _lastTickTime = DateTime.Now;
        private static readonly HttpClient _httpClient = new HttpClient();
        private static bool _keepRunning = true;
        private static bool _wasActiveLastTick = true;
        
        public static int Main(string[] args)
        {
            ForceLog("Starting up! Press 'q' then enter to quit.");

            if (!LoadConfig())
            {
                ForceLog("Configuration failed to load. Make sure config.json exists and is properly formatted JSON. Press any key to exit...");
                Console.ReadKey();
                return 1;
            }
            
            // Assume when the program starts that the PC is starting, thus the user is active.
            SendActivityPulse("Sensor Starting", DateTime.Now);

            // Run a background thread to listen for user quit input without blocking the main loop.
            Thread quitThread = new Thread(ListenForUserQuit)
            {
                IsBackground = true,
            };

            quitThread.Start();
            
            while (_keepRunning)
            {
                bool isActive;
                DateTime now = DateTime.Now;
                TimeSpan timeSinceLastTick = now - _lastTickTime;
                _lastTickTime = now;

                bool tickExceededMaxDuration = timeSinceLastTick.TotalSeconds > _config.MaxAllowedTickDurationSeconds;

                if (tickExceededMaxDuration)
                {
                    // If our tick took too long, assume the PC was asleep and is waking up - meaning the user is active.
                    isActive = true;
                    SendActivityPulse("Tick Exceeded Max Duration", now);
                }
                else
                {
                    isActive = IsPCActive(_config.IdleThresholdSeconds);
                    bool hasBecomeActive = isActive && !_wasActiveLastTick;

                    if (hasBecomeActive)
                    {
                        // If the user has just become active from an inactive state, we send an immediate pulse to make the sensor more responsive. We do not do this if it goes inactive.
                        SendActivityPulse("Has Become Active", now);
                    }
                    else if (isActive)
                    {
                        bool pulseDue = (now - _lastPulseTime).TotalSeconds >= _config.PulseIntervalSeconds;

                        if (pulseDue)
                        {
                            // If we are active and maintain that state, send periodic pulses to keep the sensor from timing out.
                            SendActivityPulse("Regular Keepalive Pulse", now);
                        }
                    }
                }

                _wasActiveLastTick = isActive;
                Thread.Sleep(_config.TickTimeSeconds * 1000);
            }
            
            ForceLog("Sensor terminated by user. Press any key to exit...");
            Console.ReadKey();
            return 0;
        }

        private static bool LoadConfig()
        {
            string path = "config.json";
            
            if (!File.Exists(path))
            {
                return false;
            }

            try
            {
                string json = File.ReadAllText(path);
                _config = JsonSerializer.Deserialize<Config>(json);
            }
            catch
            {
                return false;
            }

            return true;
        }

        private static void SendActivityPulse(string reason, DateTime time)
        {
            try
            {
                string url = $"http://{_config.HubIp}/apps/api/{_config.AppId}/devices/{_config.DeviceId}/on?access_token={_config.AccessToken}";
                _httpClient.GetAsync(url).GetAwaiter().GetResult();
                Log($"Successfully sent activity pulse: {reason}");
            }
            catch (Exception e)
            {
                Log($"Error sending activity pulse: {e.Message}");
            }
            
            _lastPulseTime = time;
        }

        private static bool IsPCActive(int thresholdSeconds)
        {
            LASTINPUTINFO lastInput = new LASTINPUTINFO();
            lastInput.cbSize = (uint)Marshal.SizeOf(lastInput);
            GetLastInputInfo(ref lastInput);
            uint idleTicks = (uint)Environment.TickCount - lastInput.dwTime;
            double idleSeconds = idleTicks / 1000.0;
            return idleSeconds < thresholdSeconds;
        }
        
        [StructLayout(LayoutKind.Sequential)]
        private struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;
        }

        [DllImport("user32.dll")]
        private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);
        
        private static void ListenForUserQuit()
        {
            while (true)
            {
                string input = Console.ReadLine();

                if (input == null || input.Trim().ToLower() != "q")
                {
                    continue;
                }

                _keepRunning = false;
                break;
            }
        }
        
        private static void Log(string message, bool force = false)
        {
            if (force || _config.EnableLogging)
            {
                Console.WriteLine($"Hubitat PC Activity Sensor [{DateTime.Now}]: {message}");
            }
        }

        private static void ForceLog(string message)
        {
            Log(message, true);
        }
    }
}