# Mecanomiografía: desarrollo de un dispositivo para el monitoreo del límite de fatiga muscular

## Descripción del repositorio

Este es el repositorio del proyecto integrador "Mecanomiografía: desarrollo de un dispositivo para el monitoreo del límite de fatiga muscular" para la carrera de ingeniería mecánica en el <a href="https://www.ib.edu.ar">Instituto Balseiro</a>. En la carpeta <a href="https://github.com/manu080797/Dispositivo-MMG/tree/main/Documentos">Documentos</a> se incluye una <a href="https://github.com/manu080797/Dispositivo-MMG/raw/main/Documentos/Presentación%20final.pptx.zip">presentación de PowerPoint</a> que sirve como introducción al trabajo realizado. Además, se incluye la <a href="https://github.com/manu080797/Dispositivo-MMG/raw/main/Documentos/Mecanomiograf%C3%ADa%20desarrollo%20de%20un%20dispositivo%20para%20el%20monitoreo%20del%20l%C3%ADmite%20de%20fatiga%20muscular.pdf">tesis completa</a> en formato PDF. En la carpeta <a href="https://github.com/manu080797/Dispositivo-MMG/tree/main/Programas">Programas</a> se encuentra el código de los tres programas desarrollados durante el proyecto.  En la carpeta <a href="https://github.com/manu080797/Dispositivo-MMG/tree/main/Electrónica">Electrónica</a> están los archivos de <a href="https://www.kicad.org">KiCAD</a> utilizados para diseñar y construir los circuitos electrónicos del dispositivo. Por último, en la carpeta <a href="https://github.com/manu080797/Dispositivo-MMG/tree/main/Carcasa">Carcasa</a> se incluyen archivos de <a href="https://solidedge.siemens.com/en/solutions/products/complete-product-development-portfolio/whats-new-in-solid-edge-2021/">SolidEdge 2021</a> con el modelo 3D de la carcasa del dispositivo, la cual fue construida con una impresora 3D.




## Resumen
En esta tesis se desarrolló un dispositivo de adquisición de señal para mecanomiografía de bajo costo, replicable y portátil. El mismo consta de un acelerómetro, electrónica de acondicionamiento de señal y una placa de desarrollo en la que se adquieren las señales de dos canales en simultáneo. Estas se procesan digitalmente y son transmitidas de manera inalámbrica a una aplicación multiplataforma de control y comunicación también desarrollada en este trabajo. Esta aplicación permite visualizar las señales en tiempo real y almacenar capturas. Estas se procesan fuera de línea según una metodología también diseñada en este trabajo para determinar qué parámetros de la señal se relacionan con la actividad muscular. Los parámetros se calcularon sobre la señal original, sobre bandas de frecuencia específicas y sobre la distribución espectral de potencia. Por último, se realizaron experimentos para aplicar la metodología propuesta a las señales adquiridas por el dispositivo desarrollado. Se realizaron ejercicios isométricos de extensión de rodilla sobre un brazo de reacción fijo vinculado a una celda de carga. Se midió la señal de mecanomiografía sobre el recto femoral de 5 sujetos en ejercicios a carga máxima, a distintos porcentajes de la carga máxima, y hasta el fallo de la tarea solicitada; y se realizaron pruebas de impacto sobre el músculo para evaluar las propiedades mecánicas del mismo. Se procesaron 145 parámetros, de los cuales se encontraron 12 con alta correlación con la fuerza ejercida (coeficiente de correlación r de Pearson ≥ 0,8). Con las pruebas de impacto se encontró que la frecuencia natural del músculo aumenta con la fuerza. En ejercicios al fallo de la tarea solicitada, se encontró que los 12 parámetros seleccionados mantienen el valor esperado con la correlación hallada, para la carga del ejercicio, durante al menos 10 segundos, y luego presentan desviaciones significativas, pero con sentidos variados entre sujetos. Los parámetros relacionados con la energía de la señal en la banda de 50 a 75 Hz presentaron una correlación superior al resto (r = 0,9) y desviaciones con el tiempo en los ejercicios al fallo consistentes en todos los sujetos y cargas evaluadas.

<img src="https://user-images.githubusercontent.com/25868073/129664927-af08412a-9a79-46f2-a796-411d43c921fb.png" height=400/>
<img src="https://user-images.githubusercontent.com/25868073/129664731-6fa30936-6542-42e9-aa4e-4a4c67d1687f.png" height=500/>
<img src="https://user-images.githubusercontent.com/25868073/129663985-bf55dc2e-1367-479e-8927-82dd2f3e256d.png" height=300/>
<img src="https://user-images.githubusercontent.com/25868073/129663899-db05f11d-fdac-4f9c-af90-bd7b4887c755.png" height=500/>


## Abstract
A low cost, replicable and portable signal acquisition device for mechanomiography was developed in this thesis. It comprises an accelerometer, its signal conditioning electronics and a development board that acquires the signal in two channels. The signal is digitally processed and wirelessly transmitted to a multi-platform control and communication App, which is also developed in this thesis. This App enables the realtime visualization of the signal and its storage for offline processing, using a methodology designed in this thesis to get correlated features with muscle activity. These features where computed over the original signal, some band filtered components and the power spectrum density. Finally, experiments where conducted to apply the developed methodology to the signal acquired by the device. These experiments consisted of isometric knee extension exercises against a fixed reaction arm linked to a load cell. The mechanomiogram signal was acquired from the rectus femoris muscle of 5 participants at maximum load, at intermediate loads, and to task failure. Additionally, bump tests where conducted to evaluate the muscle mechanical properties. 145 features where processed, of which only 12 showed a high correlation with force (Pearson’s r correlation coefficient ≥ 0,8). Muscle natural frequency was found to increase with higher force production. The 12 selected parameters presented a constant value during the first 10 seconds of a fatiguing exercise, but showed significant deviations from the expected value after this, with varying directions between subjects. From the selected features, those related with the signal energy at the 50-75 Hz band presented both the highest correlation (r = 0,9) and the most consistent deviations sense, in task failure exercises among all subjects and loads.
