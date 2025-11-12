# UnlockingFeelings (Unity + Meta Quest)

## What is this?
A VR project that teaches/illustrates emotions with interactive scenes (Joy / Anger / Fear).  
Built with Unity, targets Meta Quest, and uses the XR Interaction Toolkit.  
Includes an optional **physical sensor + Arduino** that talks to the game over Wi-Fi.

---

## Requirements
- **Meta Quest headset** (USB-C cable or Link/Air Link).
- **Unity** version: 6000.0.31f1

---

## How to run
1. **Clone**
       git clone <repo-url>
       cd UnlockingFeelings
2. Open in Unity using the exact version above and let Unity do the first Reimport.
3. Switch to Android platform File → Build Settings… → Android → Switch Platform
4. Add scenes to Build File → Build Settings… → Scenes In Build → click Add Open Scenes and ensure your scenes are checked.
5. Run
    * Build And Run with the Quest connected by cable: File → Build Settings… → Build And Run
    * Or use Link/Air Link: open a scene and hit Play in the Editor.
 ---

## Sensors (optional)
> The game can run **without** any sensors.  
> Sensor support is optional and adds a physical object that affects the game via Wi-Fi.

### What you need
- **Arduino with Wi-Fi** (ESP32 / ESP8266 / Arduino MKR WiFi)
- **MPU-6050 IMU** sensor (accelerometer + gyroscope)
- **Breadboard + jumper wires**
- **USB cable** (for flashing)
- **Power bank** (to run the Arduino untethered later)
- **Arduino IDE** (install from arduino.cc)

### Arduino IDE setup
1. Install **Arduino IDE**.  
2. Install the **Board package** for your device:
   - ESP32: *Boards Manager → “esp32” (Espressif Systems)*
   - ESP8266: *Boards Manager → “esp8266” (ESP8266 Community)*
3. Install an **MPU-6050** library (e.g., *Adafruit MPU6050* or *i2cdevlib/MPU6050*).
4. Connect the Arduino over **USB** and select:
   - **Tools → Board**: your board
   - **Tools → Port**: the COM/tty device that appeared

### Wiring (MPU-6050 → Arduino)
**I²C pins (SDA/SCL) + power:**
- **VCC → 3.3V** (preferred for ESP32/ESP8266) or **5V** *(only if your MPU-6050 breakout supports 5V!)*  
- **GND → GND**  
- **SDA → I²C SDA**, **SCL → I²C SCL**

Typical pin examples:
- **ESP32**: SDA = **GPIO 21**, SCL = **GPIO 22**
- **ESP8266 (NodeMCU)**: SDA = **D2 (GPIO 4)**, SCL = **D1 (GPIO 5)**
- **Arduino Uno**: SDA = **A4**, SCL = **A5**

> Tip: If the sensor isn’t detected, check the I²C address (usually **0x68**) and wiring.

### Flash & connect to Wi-Fi
1. Open the provided sketch: **`sensorWifiMovement.ino`**.
2. Fill in your Wi-Fi **SSID** and **Password**.
3. **Upload** the sketch.
4. Open **Serial Monitor** (e.g., 115200 baud) and note the **IP address** the board receives.
5. **LED indicator**: if the LED is **ON/steady**, the Arduino is connected to the internet.

### Power & network
- Unplug USB from the PC and power the board from a **power bank**.  
- Make sure **both the Arduino and the Quest headset are on the **same Wi-Fi network**.  
  (If the board reboots, the IP may change—check Serial Monitor again.)

### Unity hookup
- In Unity, select the GameObject with the Arduino script (e.g., `ArduinoButtonSender` / `ArduinoSenderOnGrab`).
- In the **Inspector**, set:
  - **IP** = the IP you saw in Serial Monitor (must match exactly)
  - **Port** = the port used in the sketch
- Enter Play/Build; the physical device will now drive the in-game object.
 ---
 ## Notes
* Ensure Developer Mode is enabled on the Quest and approve USB debugging.
* Typical Unity paths used above:
    * Build Settings: File → Build Settings…
    * Scenes In Build: inside the Build Settings window
    * Switch Platform: select Android then Switch Platform

<div align="center">

<h2>Unlocking Feelings — Demo</h2>

<!-- ===== Intro Section ===== -->
<h3>&nbsp;Intro &amp; Main Menu</h3>
<div style="display:inline-block; border:1px solid #ddd; border-radius:10px; padding:10px; margin-bottom:40px;">
  <video src="https://github.com/user-attachments/assets/b257e206-ebf2-4c10-9533-836f327d5c6b"
 
  controls 
  style="max-width: 100%; width: 900px; border-radius:10px;">
  </video>
</div>

<!-- ===== Emotion Rooms ===== -->
<table align="center" style="border:none; border-collapse:collapse;">
  <tr style="background-color: transparent;">
    <td align="center" style="border:none; padding:10px; background-color: transparent;">
      <img alt="Anger Room 😡" 
           src="https://img.shields.io/badge/Anger%20Room%20😡-e63946?style=for-the-badge&labelColor=1b1f23" />
      <div style="border:1px solid #ddd; border-radius:10px; padding:10px; margin-top:8px;">
        <video src="https://github.com/user-attachments/assets/023ce8f7-f51c-4538-89c9-3c2d940c226d"
        controls 
        style="width:100%; max-width:450px; border-radius:10px;"></video>
      </div>
    </td>
    <td align="center" style="border:none; padding:10px; background-color: transparent;">
      <img alt="Joy Room 🤗" 
           src="https://img.shields.io/badge/Joy%20Room%20🤗-f4a261?style=for-the-badge&labelColor=1b1f23" />
      <div style="border:1px solid #ddd; border-radius:10px; padding:10px; margin-top:8px;">
        <video src="https://github.com/user-attachments/assets/eabddd3a-40e1-4672-abef-2c214c89374a"
        controls 
        style="width:100%; max-width:450px; border-radius:10px;"></video>
      </div>
    </td>
  </tr>

  <tr style="background-color: transparent;">
    <td align="center" style="border:none; padding:10px; background-color: transparent;">
      <img alt="Sadness Room 😢" 
           src="https://img.shields.io/badge/Sadness%20Room%20😢-1d3557?style=for-the-badge&labelColor=1b1f23" />
      <div style="border:1px solid #ddd; border-radius:10px; padding:10px; margin-top:8px;">
        <video src="https://github.com/user-attachments/assets/526f9e91-36f9-46fe-ab09-434c2db4526f"
        controls 
        style="width:100%; max-width:450px; border-radius:10px;"></video>
      </div>
    </td>
    <td align="center" style="border:none; padding:10px; background-color: transparent;">
      <img alt="Fear Room 😰" 
           src="https://img.shields.io/badge/Fear%20Room%20😰-6a0dad?style=for-the-badge&labelColor=1b1f23" />
      <div style="border:1px solid #ddd; border-radius:10px; padding:10px; margin-top:8px;">
        <video src="https://github.com/user-attachments/assets/b48b2531-b765-47c5-bf2b-0fef7b2318e8"
        controls 
        style="width:100%; max-width:450px; border-radius:10px;"></video>
      </div>
    </td>
  </tr>
</table>

</div>
