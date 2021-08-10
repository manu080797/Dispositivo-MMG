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
$Comp
L Amplifier_Instrumentation:AD8236 U1
U 1 1 60185021
P 5050 3350
F 0 "U1" H 5493 3304 50  0000 L CNN
F 1 "AD8226" H 5493 3395 50  0000 L CNN
F 2 "Package_SO:SOIC-8-1EP_3.9x4.9mm_P1.27mm_EP2.29x3mm" H 4750 3350 50  0001 C CNN
F 3 "https://www.analog.com/media/en/technical-documentation/data-sheets/AD8236.pdf" H 5400 2950 50  0001 C CNN
	1    5050 3350
	-1   0    0    1   
$EndComp
$Comp
L Device:R R2
U 1 1 60186AFC
P 5800 3350
F 0 "R2" H 5870 3396 50  0000 L CNN
F 1 "5.6k" H 5870 3305 50  0000 L CNN
F 2 "Resistor_THT:R_Axial_DIN0207_L6.3mm_D2.5mm_P7.62mm_Horizontal" V 5730 3350 50  0001 C CNN
F 3 "~" H 5800 3350 50  0001 C CNN
	1    5800 3350
	1    0    0    -1  
$EndComp
$Comp
L Device:R R1
U 1 1 6018722C
P 6950 2850
F 0 "R1" V 6743 2850 50  0000 C CNN
F 1 "220k" V 6834 2850 50  0000 C CNN
F 2 "Resistor_THT:R_Axial_DIN0207_L6.3mm_D2.5mm_P7.62mm_Horizontal" V 6880 2850 50  0001 C CNN
F 3 "~" H 6950 2850 50  0001 C CNN
	1    6950 2850
	0    1    1    0   
$EndComp
Wire Wire Line
	5800 3550 5800 3500
Wire Wire Line
	5800 3150 5800 3200
$Comp
L Connector:Conn_01x05_Male J1
U 1 1 6018BFD6
P 2250 3350
F 0 "J1" H 2358 3731 50  0001 C CNN
F 1 "Conn_01x05_Male" H 2358 3640 50  0001 C CNN
F 2 "Connector_PinHeader_2.54mm:PinHeader_1x05_P2.54mm_Vertical" H 2250 3350 50  0001 C CNN
F 3 "~" H 2250 3350 50  0001 C CNN
	1    2250 3350
	1    0    0    -1  
$EndComp
$Comp
L Connector:Conn_01x05_Male J2
U 1 1 6018CBE7
P 7800 3350
F 0 "J2" H 7772 3282 50  0001 R CNN
F 1 "Conn_01x05_Male" H 7772 3373 50  0001 R CNN
F 2 "Connector_PinHeader_2.54mm:PinHeader_1x05_P2.54mm_Vertical" H 7800 3350 50  0001 C CNN
F 3 "~" H 7800 3350 50  0001 C CNN
	1    7800 3350
	-1   0    0    1   
$EndComp
Text Notes 7900 3550 0    50   ~ 0
X\nY\nZ\nSL\n0G
Text Notes 2050 3550 0    50   ~ 0
5V\n3V3\nGND\nGS\nST
Wire Wire Line
	5450 3250 5650 3250
Wire Wire Line
	5650 3250 5650 3150
Wire Wire Line
	5650 3150 5800 3150
Wire Wire Line
	5800 3550 5650 3550
Wire Wire Line
	5650 3550 5650 3450
Wire Wire Line
	5650 3450 5450 3450
Wire Wire Line
	5450 3150 5550 3150
Wire Wire Line
	7600 3350 7400 3350
Wire Wire Line
	5550 3650 5550 3550
Wire Wire Line
	5550 3550 5450 3550
$Comp
L Connector_Generic:Conn_01x04 USB_Cable_Connector1
U 1 1 601A0D7D
P 3500 2100
F 0 "USB_Cable_Connector1" V 3418 1812 50  0000 R CNN
F 1 "Conn_01x04" V 3373 1812 50  0001 R CNN
F 2 "Connector_USB:USB_A_CNCTech_1001-011-01101_Horizontal" H 3500 2100 50  0001 C CNN
F 3 "~" H 3500 2100 50  0001 C CNN
	1    3500 2100
	0    -1   -1   0   
$EndComp
Wire Wire Line
	3600 3050 4950 3050
Text Notes 3300 2000 0    50   ~ 0
5V Z Ref GND
Wire Notes Line
	2450 4900 7600 4900
Wire Notes Line
	7600 2400 2450 2400
Wire Wire Line
	3700 2300 3700 2550
Connection ~ 7400 3350
Wire Wire Line
	7400 3350 7400 3650
Wire Wire Line
	2450 3150 3400 3150
Wire Wire Line
	3600 2300 3600 3050
Wire Wire Line
	3500 2300 3500 3350
Wire Wire Line
	3500 3350 4650 3350
Wire Wire Line
	3400 2300 3400 3150
Wire Wire Line
	5150 3650 5150 3850
Wire Wire Line
	5150 3850 3400 3850
Wire Wire Line
	3400 3850 3400 3150
Connection ~ 3400 3150
$Comp
L Device:C C1
U 1 1 601BF8FC
P 6300 3100
F 0 "C1" H 6415 3146 50  0000 L CNN
F 1 "470p" H 6415 3055 50  0000 L CNN
F 2 "Capacitor_THT:C_Disc_D5.0mm_W2.5mm_P2.50mm" H 6338 2950 50  0001 C CNN
F 3 "~" H 6300 3100 50  0001 C CNN
	1    6300 3100
	1    0    0    -1  
$EndComp
Wire Wire Line
	7400 2850 7400 3350
Wire Wire Line
	6300 2850 5550 2850
Wire Wire Line
	5550 2850 5550 3150
Connection ~ 6300 2850
Wire Wire Line
	6300 2850 6300 2950
$Comp
L power:GND #PWR0101
U 1 1 601C37F0
P 6300 3350
F 0 "#PWR0101" H 6300 3100 50  0001 C CNN
F 1 "GND" H 6305 3177 50  0000 C CNN
F 2 "" H 6300 3350 50  0001 C CNN
F 3 "" H 6300 3350 50  0001 C CNN
	1    6300 3350
	1    0    0    -1  
$EndComp
Wire Wire Line
	6300 3250 6300 3350
$Comp
L power:GND #PWR0102
U 1 1 601C4C02
P 5150 2900
F 0 "#PWR0102" H 5150 2650 50  0001 C CNN
F 1 "GND" H 5155 2727 50  0000 C CNN
F 2 "" H 5150 2900 50  0001 C CNN
F 3 "" H 5150 2900 50  0001 C CNN
	1    5150 2900
	-1   0    0    1   
$EndComp
Wire Wire Line
	5150 3050 5150 2900
$Comp
L Device:C C2
U 1 1 601C5AAB
P 5150 4000
F 0 "C2" H 5265 4046 50  0000 L CNN
F 1 "100n" H 5265 3955 50  0000 L CNN
F 2 "Capacitor_THT:C_Disc_D5.0mm_W2.5mm_P2.50mm" H 5188 3850 50  0001 C CNN
F 3 "~" H 5150 4000 50  0001 C CNN
	1    5150 4000
	1    0    0    -1  
$EndComp
Connection ~ 5150 3850
$Comp
L power:GND #PWR0103
U 1 1 601C5F21
P 5150 4150
F 0 "#PWR0103" H 5150 3900 50  0001 C CNN
F 1 "GND" H 5155 3977 50  0000 C CNN
F 2 "" H 5150 4150 50  0001 C CNN
F 3 "" H 5150 4150 50  0001 C CNN
	1    5150 4150
	1    0    0    -1  
$EndComp
$Comp
L power:GND #PWR0104
U 1 1 601C87C7
P 3700 2550
F 0 "#PWR0104" H 3700 2300 50  0001 C CNN
F 1 "GND" H 3705 2377 50  0000 C CNN
F 2 "" H 3700 2550 50  0001 C CNN
F 3 "" H 3700 2550 50  0001 C CNN
	1    3700 2550
	1    0    0    -1  
$EndComp
$Comp
L power:GND #PWR0105
U 1 1 601C8947
P 2750 3350
F 0 "#PWR0105" H 2750 3100 50  0001 C CNN
F 1 "GND" H 2755 3177 50  0000 C CNN
F 2 "" H 2750 3350 50  0001 C CNN
F 3 "" H 2750 3350 50  0001 C CNN
	1    2750 3350
	1    0    0    -1  
$EndComp
Wire Wire Line
	2450 3350 2750 3350
Wire Wire Line
	7500 3450 7600 3450
$Comp
L Device:R R4
U 1 1 601EDDB2
P 6950 3650
F 0 "R4" V 6743 3650 50  0000 C CNN
F 1 "220k" V 6834 3650 50  0000 C CNN
F 2 "Resistor_THT:R_Axial_DIN0207_L6.3mm_D2.5mm_P7.62mm_Horizontal" V 6880 3650 50  0001 C CNN
F 3 "~" H 6950 3650 50  0001 C CNN
	1    6950 3650
	0    1    1    0   
$EndComp
$Comp
L Device:C C3
U 1 1 601EDDB8
P 6300 3900
F 0 "C3" H 6050 3950 50  0000 L CNN
F 1 "470n" H 6000 3850 50  0000 L CNN
F 2 "Capacitor_THT:C_Disc_D5.0mm_W2.5mm_P2.50mm" H 6338 3750 50  0001 C CNN
F 3 "~" H 6300 3900 50  0001 C CNN
	1    6300 3900
	1    0    0    -1  
$EndComp
Wire Wire Line
	6300 3650 5550 3650
Connection ~ 6300 3650
Wire Wire Line
	6300 3650 6300 3750
$Comp
L power:GND #PWR01
U 1 1 601EDDC2
P 6300 4500
F 0 "#PWR01" H 6300 4250 50  0001 C CNN
F 1 "GND" H 6305 4327 50  0000 C CNN
F 2 "" H 6300 4500 50  0001 C CNN
F 3 "" H 6300 4500 50  0001 C CNN
	1    6300 4500
	1    0    0    -1  
$EndComp
Wire Wire Line
	6300 4400 6300 4500
$Comp
L Device:R R3
U 1 1 601F0165
P 6300 4250
F 0 "R3" V 6093 4250 50  0000 C CNN
F 1 "5.6k" V 6184 4250 50  0000 C CNN
F 2 "Resistor_THT:R_Axial_DIN0207_L6.3mm_D2.5mm_P7.62mm_Horizontal" V 6230 4250 50  0001 C CNN
F 3 "~" H 6300 4250 50  0001 C CNN
	1    6300 4250
	1    0    0    -1  
$EndComp
Wire Wire Line
	6300 4050 6300 4100
$Comp
L Device:D_Schottky D1
U 1 1 601F1144
P 6950 4000
F 0 "D1" H 6950 3900 50  0000 C CNN
F 1 "D_Schottky" H 6950 3850 50  0000 C CNN
F 2 "Diode_THT:D_DO-35_SOD27_P7.62mm_Horizontal" H 6950 4000 50  0001 C CNN
F 3 "~" H 6950 4000 50  0001 C CNN
	1    6950 4000
	1    0    0    -1  
$EndComp
$Comp
L Device:D_Schottky D2
U 1 1 601F369D
P 6950 4400
F 0 "D2" H 6950 4500 50  0000 C CNN
F 1 "D_Schottky" H 6950 4550 50  0000 C CNN
F 2 "Diode_THT:D_DO-35_SOD27_P7.62mm_Horizontal" H 6950 4400 50  0001 C CNN
F 3 "~" H 6950 4400 50  0001 C CNN
	1    6950 4400
	-1   0    0    1   
$EndComp
Wire Wire Line
	7400 3650 7100 3650
Wire Wire Line
	6800 4000 6700 4000
Wire Wire Line
	6700 4400 6800 4400
Wire Wire Line
	7100 4400 7250 4400
Wire Wire Line
	7250 4000 7100 4000
Wire Wire Line
	7250 4200 7400 4200
Connection ~ 7250 4200
Connection ~ 7400 3650
Wire Wire Line
	6300 3650 6550 3650
Wire Wire Line
	6700 4200 6550 4200
Connection ~ 6700 4200
Connection ~ 6550 3650
Wire Wire Line
	6550 3650 6800 3650
Wire Wire Line
	7500 3450 7500 4750
Wire Wire Line
	3400 4750 3400 3850
Wire Wire Line
	3400 4750 7500 4750
Connection ~ 3400 3850
Wire Wire Line
	7400 2850 7100 2850
Wire Wire Line
	6300 2850 6800 2850
Wire Wire Line
	6550 3650 6550 4200
Wire Wire Line
	6700 4000 6700 4200
Wire Wire Line
	6700 4200 6700 4400
Wire Wire Line
	7250 4200 7250 4400
Wire Wire Line
	7250 4000 7250 4200
Wire Wire Line
	7400 3650 7400 4200
Wire Notes Line
	7600 2400 7600 4900
Wire Notes Line
	2450 2400 2450 4900
Wire Notes Line
	850  5250 850  7750
$EndSCHEMATC
