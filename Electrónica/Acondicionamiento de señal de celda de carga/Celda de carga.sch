EESchema Schematic File Version 4
EELAYER 30 0
EELAYER END
$Descr A4 11693 8268
encoding utf-8
Sheet 1 1
Title ""
Date ""
Rev ""
Comp ""
Comment1 ""
Comment2 ""
Comment3 ""
Comment4 ""
$EndDescr
Wire Wire Line
	2750 4050 2850 4050
Wire Wire Line
	2750 3450 2750 4050
Wire Wire Line
	2950 3450 2750 3450
Wire Wire Line
	2950 4150 2950 4700
Wire Wire Line
	2950 3450 2950 3950
Text Notes 3000 3800 0    50   ~ 0
Iz_min = 1 mA
Text Notes 3250 3550 0    50   ~ 0
I_in = 7.14 mA
Text Notes 3000 2850 0    50   ~ 0
I_min = 9 mA
Text Notes 5200 2800 0    50   ~ 0
Resolución ADC: 1 mV aprox.\nResolución ADC con oversampling: 0,1 mV aprox.\nResolución carga: 7.7 - 77 mg aprox.
Text Notes 6500 4050 0    50   ~ 0
A=495
Text Notes 7600 4100 0    50   ~ 0
13 mV/kg
Text Notes 4250 3300 0    50   ~ 0
Load cell 2.1mV/V 200kg
Wire Notes Line
	5800 3350 4250 3350
Wire Notes Line
	5800 4600 5800 3350
Wire Notes Line
	4250 4600 5800 4600
Wire Notes Line
	4250 3350 4250 4600
Wire Wire Line
	9350 4600 6850 4600
Wire Wire Line
	9350 4200 9350 4600
Wire Wire Line
	6850 4300 6850 4600
Wire Wire Line
	8950 4200 9350 4200
Wire Wire Line
	6650 4700 8650 4700
Wire Wire Line
	4350 3550 4350 3650
$Comp
L Device:R_Variable Strain_Gauge3
U 1 1 6077A14B
P 4350 3800
F 0 "Strain_Gauge3" H 4478 3846 50  0000 L CNN
F 1 "350" H 4478 3755 50  0000 L CNN
F 2 "" V 4280 3800 50  0001 C CNN
F 3 "~" H 4350 3800 50  0001 C CNN
	1    4350 3800
	1    0    0    -1  
$EndComp
Text Notes 7600 4800 0    50   ~ 0
GND
Text Notes 3250 3450 0    50   ~ 0
V_in 2.5V
Text Notes 7600 2900 0    50   ~ 0
Vss 5V
Text Notes 5850 4300 0    50   ~ 0
V+
Text Notes 5850 3800 0    50   ~ 0
V-
Text Notes 7600 4000 0    50   ~ 0
Vout
Text Notes 7550 4600 0    50   ~ 0
Vref 500 mV
Wire Wire Line
	4700 4500 4700 4700
Wire Wire Line
	6650 4300 6650 4700
Connection ~ 6650 4700
Wire Wire Line
	4700 4700 6650 4700
Connection ~ 4700 4700
Wire Wire Line
	6650 4700 6650 4800
Wire Wire Line
	8650 4700 8650 4500
Wire Wire Line
	2950 4700 4700 4700
Wire Wire Line
	8200 4000 7150 4000
Wire Wire Line
	8200 3500 8200 4000
Wire Wire Line
	9350 3500 8200 3500
Wire Wire Line
	9350 4100 9350 3500
Wire Wire Line
	8950 4100 9350 4100
Wire Wire Line
	9150 3900 9150 2900
Wire Wire Line
	8950 3900 9150 3900
$Comp
L Connector:USB_A J1
U 1 1 606FE03B
P 8650 4100
F 0 "J1" H 8707 4567 50  0000 C CNN
F 1 "Conector_USB" H 8707 4476 50  0000 C CNN
F 2 "" H 8800 4050 50  0001 C CNN
F 3 " ~" H 8800 4050 50  0001 C CNN
	1    8650 4100
	1    0    0    -1  
$EndComp
Connection ~ 6650 2900
Wire Wire Line
	6650 2900 9150 2900
Wire Wire Line
	6950 3650 6950 3550
$Comp
L power:GND #PWR?
U 1 1 606CE2B1
P 6950 3650
F 0 "#PWR?" H 6950 3400 50  0001 C CNN
F 1 "GND" H 6955 3477 50  0000 C CNN
F 2 "" H 6950 3650 50  0001 C CNN
F 3 "" H 6950 3650 50  0001 C CNN
	1    6950 3650
	1    0    0    -1  
$EndComp
Wire Wire Line
	6650 3550 6650 2900
Connection ~ 6650 3550
$Comp
L Device:C C1
U 1 1 606CD18B
P 6800 3550
F 0 "C1" V 6548 3550 50  0000 C CNN
F 1 "0.1u" V 6639 3550 50  0000 C CNN
F 2 "" H 6838 3400 50  0001 C CNN
F 3 "~" H 6800 3550 50  0001 C CNN
	1    6800 3550
	0    1    1    0   
$EndComp
Wire Wire Line
	4700 4500 5050 4500
Connection ~ 4700 4500
Wire Wire Line
	4350 4500 4700 4500
Wire Wire Line
	2950 3450 2950 3350
Connection ~ 2950 3450
Wire Wire Line
	4700 3450 2950 3450
Wire Wire Line
	2950 2900 2950 3050
Wire Wire Line
	6650 2900 2950 2900
Wire Wire Line
	6650 3700 6650 3550
Wire Wire Line
	6250 4100 6350 4100
Wire Wire Line
	6250 4150 6250 4100
Wire Wire Line
	5950 4150 6250 4150
Wire Wire Line
	6250 3850 5950 3850
Wire Wire Line
	6250 3900 6250 3850
Wire Wire Line
	6350 3900 6250 3900
$Comp
L Device:RTRIM Rg
U 1 1 606C86F6
P 5950 4000
F 0 "Rg" H 6077 4046 50  0000 L CNN
F 1 "100 5%" H 6077 3955 50  0000 L CNN
F 2 "" V 5880 4000 50  0001 C CNN
F 3 "~" H 5950 4000 50  0001 C CNN
	1    5950 4000
	1    0    0    -1  
$EndComp
Wire Wire Line
	4350 4000 4350 3950
Connection ~ 4350 4000
Wire Wire Line
	5750 4000 4350 4000
Wire Wire Line
	5750 3800 5750 4000
Wire Wire Line
	6350 3800 5750 3800
Wire Wire Line
	5050 4050 5050 3950
Connection ~ 5050 4050
Wire Wire Line
	5750 4050 5050 4050
Wire Wire Line
	5750 4200 5750 4050
Wire Wire Line
	6350 4200 5750 4200
$Comp
L Amplifier_Instrumentation:AD8422 U2
U 1 1 606C6043
P 6750 4000
F 0 "U2" H 7194 4046 50  0000 L CNN
F 1 "AD8226" H 7194 3955 50  0000 L CNN
F 2 "" H 6450 4000 50  0001 C CNN
F 3 "https://www.analog.com/media/en/technical-documentation/data-sheets/AD8422.pdf" H 7100 3600 50  0001 C CNN
	1    6750 4000
	1    0    0    -1  
$EndComp
Wire Wire Line
	4700 3550 5050 3550
Connection ~ 4700 3550
Wire Wire Line
	4700 3550 4700 3450
Wire Wire Line
	5050 4500 5050 4400
Wire Wire Line
	4350 4400 4350 4500
Wire Wire Line
	4350 4100 4350 4000
Wire Wire Line
	5050 4100 5050 4050
Wire Wire Line
	5050 3550 5050 3650
Wire Wire Line
	4350 3550 4700 3550
$Comp
L Device:R_Variable Strain_Gauge2
U 1 1 606C43EC
P 5050 4250
F 0 "Strain_Gauge2" H 5178 4296 50  0000 L CNN
F 1 "350" H 5178 4205 50  0000 L CNN
F 2 "" V 4980 4250 50  0001 C CNN
F 3 "~" H 5050 4250 50  0001 C CNN
	1    5050 4250
	1    0    0    -1  
$EndComp
$Comp
L Device:R_Variable Strain_Gauge1
U 1 1 606C3925
P 5050 3800
F 0 "Strain_Gauge1" H 5178 3846 50  0000 L CNN
F 1 "350" H 5178 3755 50  0000 L CNN
F 2 "" V 4980 3800 50  0001 C CNN
F 3 "~" H 5050 3800 50  0001 C CNN
	1    5050 3800
	1    0    0    -1  
$EndComp
$Comp
L Device:RTRIM Strain_Gauge4
U 1 1 606C277C
P 4350 4250
F 0 "Strain_Gauge4" H 4478 4296 50  0000 L CNN
F 1 "350" H 4478 4205 50  0000 L CNN
F 2 "" V 4280 4250 50  0001 C CNN
F 3 "~" H 4350 4250 50  0001 C CNN
	1    4350 4250
	1    0    0    -1  
$EndComp
$Comp
L power:GND #PWR?
U 1 1 606C0555
P 6650 4800
F 0 "#PWR?" H 6650 4550 50  0001 C CNN
F 1 "GND" H 6655 4627 50  0000 C CNN
F 2 "" H 6650 4800 50  0001 C CNN
F 3 "" H 6650 4800 50  0001 C CNN
	1    6650 4800
	1    0    0    -1  
$EndComp
$Comp
L Device:R Rs
U 1 1 606BFE0B
P 2950 3200
F 0 "Rs" H 3020 3246 50  0000 L CNN
F 1 "240 10%" H 3020 3155 50  0000 L CNN
F 2 "" V 2880 3200 50  0001 C CNN
F 3 "~" H 2950 3200 50  0001 C CNN
	1    2950 3200
	1    0    0    -1  
$EndComp
$Comp
L Reference_Voltage:TL431P U1
U 1 1 606BEAF0
P 2950 4050
F 0 "U1" V 2996 3962 50  0000 R CNN
F 1 "TL431" V 2905 3962 50  0000 R CNN
F 2 "Package_TO_SOT_SMD:SOT-23" H 2950 3850 50  0001 C CIN
F 3 "http://www.ti.com/lit/ds/symlink/lm4040-n.pdf" H 2950 4050 50  0001 C CIN
	1    2950 4050
	0    -1   -1   0   
$EndComp
$EndSCHEMATC
