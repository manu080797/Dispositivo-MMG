#include <Arduino.h>
#include <DeviceManager.h>

DeviceManager Manager;

void setup()
{
  Serial.begin(9600);
  
  //WORKAROUND: new esp-idf lowers CPU speed from 240 MHZ to 160 MHZ. Needs to be set manually to 240 MHZ.
  setCpuFrequencyMhz(240);

  Serial.printf("Welcome to Sensus v2 console! \n");
  Serial.printf("Author: Manuel Lima 2021 \n");
  
  #if ENABLE_DEBUG == 1
  Serial.printf("DEBUG: Clock speeds: CPU: %d MHZ, XTAL: %d MHZ, APB: %d MHZ \n", getCpuFrequencyMhz(), getXtalFrequencyMhz(), getApbFrequency()/1000000);
  #endif

  Manager.InitializateDevice();
}

void loop()
{
  // Don't waste CPU cyles in Arduino Main Loop
  vTaskDelete(NULL);
}



