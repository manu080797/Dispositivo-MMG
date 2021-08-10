# Programas

Programas desarrollados para el dispositivo:

- Sensus: firmware de la placa de desarrollo ESP32. Adquiere la señal analógica en dos canales simultáneamente a 25,6 kHz, realiza un filtrado digital pasa-bajos en dos etapas, remuestrea las señales a 400 Hz y transmite por Bluetooth LE a la aplicación multiplataforma Musculus.

- Musculus: aplicación multiplaforma programada en C# mediante el framework Xamarin. Se comunica con la placa de desarrollo mediante Bluetooth LE. Provee de una interfaz gráfica para controlar el dispositivo de adquisición y visualizar las señales en tiempo real. También permite realizar una captura y compartirla mediante la API de compartir del sistema operativo en que se ejecuta la aplicación. El único código específico de plataforma de la aplicación es el relacionado a la comunicación Bluetooth LE. Este sólo se encuentra implementado para Android, por lo que la aplicación únicamente funciona en dicha plataforma.

- pyMMG: módulo de Python para procesamiento fuera de línea de las capturas tomadas con Musculus. Permite realizar un preprocesamiento para reducir ruido y filtrar ciertas componentes de frecuencia de la señal. Además, realiza una extracción sistemática de parámetros de la señal para luego ser procesados mediante técnicas de análisis de datos. Se incluye un notebook de Jupyter con un ejemplo de uso del módulo.