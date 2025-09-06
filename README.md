# Hubitat PC Activity Sensor

## Description
When you are in an office on a computer, you're not moving a lot. This project translates user activity on a computer (with the keyboard and mouse) to a virtual motion sensor in Hubitat to be used for presence in that room. It was originally inspired by [sburke781's PC_VirtualMotionSensor](https://github.com/sburke781/hubitat/tree/master/PC_VirtualMotionSensor) but I ran into some limitations with that - primarily that it stays active if the computer goes offline, and I found it was sending more network traffic than needed. I primarily wrote this for my own usage, but I'm making it available for others.

## How Does it Work
I wrote a small console program in C# that runs hidden on the PC being monitored, detecting the number of seconds since the last user input was recorded, e.g. mouse movement or keyboard strokes. The program sends periodic pulses of user activity to a Virtual Motion Sensor device made available in Hubitat through the Maker API over your local network.

My program only sends these activity pulses once every two minutes, or when a user goes from inactive to active (for responsiveness). It also detects long gaps, which would be if the PC was asleep, and sends activity after those, when the PC wakes up. Notably, in a big change from sburke's solution, my program does not send "inactive" pulses. It relies on the driver on Hubitat to go inactive over time not receiving active pulses. This allows it to detect if a PC shuts down or goes to sleep.

After that, the Virtual Motion Sensor device can be used like any other motion sensor, including in the built-in Motion Lighting app to keep a motion lighting rule active, or in a Rule Machine rule.

## Requirements
My program is written in .NET so this solution only works on Windows at this time. You'll also need to set it up to run on startup through the Task Scheduler, so you'll need to be an administrator on the PC you're using it on if you want it to run automatically.

## Installation and Configuration
Keep a Notepad window open during this setup process to paste in a handful of details used later in the configuration file. Download the latest release, or compile the program yourself from source.

1. Virtual Motion Sensor Unlimited
	* The motion sensor we're going to use needs a custom driver you will find in the release.
	* This driver allows us to use much longer timeout times for the PC activity, which will be necessary.
	* [Refer to the documentation](https://docs2.hubitat.com/en/how-to/install-custom-drivers) on how to install custom drivers, and install Virtual Motion Sensor Unlimited.
2. Hubitat Virtual Motion Sensor Unlimited Device
   * Open the **Devices** page for your Hubitat Hub and click **Add Virtual Device**.
   * Enter a **Device Name**.
   * In the **Type** drop-down list select the **Virtual Motion Sensor Unlimited** driver.
   * Click **Save Device** to create the new motion sensor.
   * The **Device Setup Page** is then displayed.  Note down the **Device Id**, included at the end of the URL for the page, it should be a number.
   
3. Maker API
   * In the Apps section of the Hubitat Hub, add the newly created device to an existing Maker API install or install and configure a new Maker API installation.
   * From Maker API app, take a copy of the example **Command URL**.
   
4. The Program
   * Create a folder on the PC being monitored, e.g. C:\HomeAutomation\Hubitat\HubitatPCActivitySensor
   * Download and extract two files to this folder:
      * HubitatPCActivitySensor.exe
      * config.json

5. Configuration
   * Open the local copy of the **config.json** file in Notepad or similar application.
   * Set the **HubIp**, **AppId** and **AccessToken** settings using the example command URL in step 3.
   * Set the **DeviceId** from step 2.
   * All of the following bullets in section 5 here are optional, the configuration values have sane defaults so the sensor should just work out of the box without tweaking these unless you really want to...
   * Set **PulseIntervalSeconds**, this is how often the keepalive pulse will be sent while the PC is active. It needs to be less than **IdleThresholdSeconds**, which is how far back in time the program detects activity...and less than the inactive time of the device. The default values I have chosen keep from spamming the network but also keep the motion sensor responsive.
   * The time for the sensor to go inactive after you stop using the PC will be between **IdleThresholdSeconds** and **IdleThresholdSeconds** plus the sensor's inactive time. By default this is 2.5 to 5 minutes after you stop using the PC.
   * Set **EnableLogging** to true if you want more verbose logs in the console window. (I do not save them to disk, it would be spammy.) Just keep in mind you have to unhide the window to see them.
   
6. Task Scheduler Task   
   * From the **Windows Start Menu**, type **Task Scheduler** and press enter.
   * In the right-hand panel, click **Create Task** (not "Basic Task").
	* Under the **General** tab:
		* Name it something like "**Hubitat PC Activity Sensor**". Optionally give it a description.
		* Select "**Run only when user is logged on**".
		* Do not check "**Run with highest privileges**."
		* Check "**Hidden**" at the bottom to make sure our program is...well...hidden.
	* Go to the **Triggers** tab:
		* Click **New**...
		* Set **Begin the task** to **At log on**.
		* Leave everything else as-is and click **OK**.
	* Go to the **Actions** tab:
		* Click **New**...
		* Set **Action** to **Start a program**.
		* In the **Program/script** box, click **Browse** and select **HubitatPCActivitySensor.exe**.
		* Set **Start in (optional)** to the folder containing **HubitatPCActivitySensor.exe**.
		* Click **OK**.
	* Go to the **Conditions** tab:
		* Uncheck **Start the task only if the computer is on AC power** (optional).
	* Click **OK** to save the task.
	
7. Use the motion sensor in motion lighting or Rule Machine rules.

8. Optional Cleanup of Settings
   * Turn off the **Enable Logging** preference for the Virtual Motion Sensor device on the Hubitat hub to reduce the amount of logs being generated.
   * Set **EnableLogging** in **config.json** to false.