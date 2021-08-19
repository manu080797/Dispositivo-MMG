# Sensus
Firmware para Sensus (basado en placa de desarrollo Espressif ESP32)

### Flujo de procesamiento de datos
![image](https://user-images.githubusercontent.com/25868073/130008636-88f5d2b3-ed65-4792-b6d1-598974e68ef2.png)

### Diagrama de clases
![diagrama_clases_firmware](https://user-images.githubusercontent.com/25868073/130008796-05b00228-d15b-4bd7-995a-df950e85b042.png)

### Diagrama GRAFCET de la gestión del estado del dispositivo
![image](https://user-images.githubusercontent.com/25868073/130008675-5ff31fa7-2fa1-4266-ab03-ae93b5441134.png)

## Requerimientos:
- Visual Studio Code
- PlatformIO

## Compilación
- Abrir repositorio en Visual Studio Code con la extensión PlatformIO instalada
- Compilar el firmware Sensus usando el comando "PlaformIO: build"
- Conectar la placa de desarrollo ESP32 mediante un cable USB a la computadora
- Modificar el puerto serie/COM en el archivo platformio.ini para que coincida con el puerto asignado al ESP32 por el sistema operativo (ver documentación de PlatformIO)
- Subir el firmware compilado a la placa de desarrollo ESP32 usando el comando "PlaformIO: upload"
- Durante el proceso de subida, PlatformIO espera una conexión exitosa con el dispositivo. Durante esta espera se debe presionar el botón BOOT en la placa de desarrollo para que comienze la carga del firmware en la memoria de la ESP32.
