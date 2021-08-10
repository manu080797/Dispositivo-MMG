# Sensus
Sensus firmware for ESP-32

Requirements:
- Visual Studio Code
- PlatformIO

Usage:
- Open repo in Visual Studio Code with PlatformIO extension
- Compile using "PlaformIO: build" command
- Plug ESP-32 with USB cable
- Modify upload port in platformio.ini file to match ESP-32 serial port address (see PlatformIO documentation)
- Upload to ESP-32 using "PlaformIO: upload" command
- While waiting for ESP-32 during the upload process, press the BOOT button in the development board to start writting to device memory
