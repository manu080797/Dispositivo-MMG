#include <BLEManager.h>

// Start Bluetooth Low Energy GATT server
// See example in: https://randomnerdtutorials.com/esp32-bluetooth-low-energy-ble-arduino-ide/
// See arduino-esp32 bluetooth documentation in: https://github.com/nkolban/esp32-snippets/blob/master/Documentation/BLE%20C%2B%2B%20Guide.pdf
// See ArduinoBLE documentation in: https://www.arduino.cc/en/Reference/ArduinoBLE
// See low level documentation in: https://github.com/espressif/esp-idf/blob/c77c4ccf6c43ab09fd89e7c907bf5cf2a3499e3b/examples/bluetooth/bluedroid/ble/gatt_server/tutorial/Gatt_Server_Example_Walkthrough.md
bool BLEManager::InitializateCommunications(bool* pDeviceConnected, bool* pAcquireFlag, bool* pInternalError) 
{
  // Initializate BLE stack
  BLEDevice::init("Sensus");
  Serial.println("INFO: Initializated BLE class.");

  // Create GATT server
  pServer = BLEDevice::createServer();
  pServer->setCallbacks(new ServerCallbacks(pDeviceConnected));
  Serial.println("INFO: Created GATT server.");

  // Create Service
  pService = pServer->createService(SERVICE_UUID);
  Serial.println("INFO: Created Sensus service.");

  // Create Caracteristics
  pCommand_Characteristic = pService->createCharacteristic(COMMAND_CHARACTERISTIC_UUID, BLECharacteristic::PROPERTY_WRITE);
  pCommand_Characteristic->setValue("NO COMMAND RECEIVED");
  pCommand_Characteristic->setCallbacks(new CommandCharacteristicCallbacks(this, pAcquireFlag));

  pData_Characteristic = pService->createCharacteristic(DATA_CHARACTERISTIC_UUID, BLECharacteristic::PROPERTY_NOTIFY);
  pData_Characteristic->setValue("NAN");
  pData_Characteristic->addDescriptor(new BLE2902());
  pData_Characteristic->setCallbacks(new DataCharacteristicCallbacks(this, pInternalError));

  pSamplingFrequency_Characteristic = pService->createCharacteristic(SAMPLINGFREQUENCY_CHARACTERISTIC_UUID, BLECharacteristic::PROPERTY_READ);
  float samplingFrequency = 1000000.0/SAMPLING_PERIOD/DECIMATION/DECIMATION;
  pSamplingFrequency_Characteristic->setValue(samplingFrequency);
  Serial.println("INFO: Created data, command and sampling frequency characteristics.");

  // Start GATT server
  pService->start();
  Serial.println("INFO: Started GATT server.");

  // Set and start advertiser
  pAdvertising = BLEDevice::getAdvertising();
  pAdvertising->addServiceUUID(SERVICE_UUID);
  // TODO: Find out what does the follwing lines do.
  pAdvertising->setScanResponse(true);
  pAdvertising->setMinPreferred(0x06);  // functions that apparently help with iPhone connections issue
  pAdvertising->setMinPreferred(0x12);
  //
  pServer->startAdvertising();
  Serial.println("INFO: Advertising...");

  if (pCommand_Characteristic == NULL || pData_Characteristic == NULL || pServer == NULL || pService == NULL || pAdvertising == NULL)
  {
    Serial.println("ERROR: NULL pointer detected in BLE stack initialization.");
    return false;
  }

  // Initializate communications queue
  Queue = xQueueCreate(BLEQUEUE_SIZE, sizeof(Data));
  QueueSize = BLEQUEUE_SIZE;
  Serial.printf("INFO: Created BLEQueue. Size: %d (%d Bytes)\n", BLEQUEUE_SIZE, BLEQUEUE_SIZE*sizeof(Data));
  if (Queue == NULL)
  {
    Serial.println("ERROR: NULL pointer detected in BLE Queue handle.");
    return false;
  }

  // Initilizate BLESender Task
  int stackSize = 4000; // TODO: determine how much is the minimum necessary stack size
  int taskPriority = 2;
  int core = 0;
  BaseType_t status = xTaskCreatePinnedToCore(BLESender,
                                              "BLESender", 
                                              stackSize, 
                                              (void*)this, 
                                              taskPriority, 
                                              &BLESenderTaskHandle, 
                                              core);
  Serial.printf("INFO: Created BLESender task. status=%d\n", status);
  
  return true;
}

void BLEManager::BLESender(void *pvParameters)
{
  BLEManager *pBLEManager = (BLEManager*) pvParameters;

  for (;;)
  {
    ulTaskNotifyTake(pdTRUE, portMAX_DELAY);

    for (int i = 0; i < SAMPLES_PER_BLE_PACKET * 8; i += 8)
    {
      // Get last element in queue
      xQueueReceive(pBLEManager->Queue, &pBLEManager->Data, portMAX_DELAY);

      // Encode Data struct as byte array of lenght 8
      memcpy(&(pBLEManager->PackedData[i]), &pBLEManager->Data.Z1, 4);
      memcpy(&(pBLEManager->PackedData[i + 4]), &pBLEManager->Data.Z2, 4);
    }
    pBLEManager->Counter += 1;

    // Send byte array to BLE stack and notify BLE central device that new data is available in Data BLE characteristic
    pBLEManager->pData_Characteristic->setValue(pBLEManager->PackedData, SAMPLES_PER_BLE_PACKET * 8);
    pBLEManager->pData_Characteristic->notify();

    // Reset watchdog
    vTaskDelay(1); 
  }
}

CommandCharacteristicCallbacks::CommandCharacteristicCallbacks(BLEManager* pBLEManager, bool* pAcquireFlag)
{
  pBLEManagerInstance = pBLEManager;
  pAcquire = pAcquireFlag;
}

void CommandCharacteristicCallbacks::onWrite(BLECharacteristic* pCharacteristic)
{
  Serial.print("INFO: New command received: ");
  Serial.println(pCharacteristic->getValue().c_str());

  if (pCharacteristic->getValue() == START_COMMAND)
  {
    xTaskNotifyGive(pBLEManagerInstance->BLESenderTaskHandle);
    *pAcquire = true;
  }
  else if (pCharacteristic->getValue() == STOP_COMMAND)
  {
    *pAcquire = false;
  }
}

DataCharacteristicCallbacks::DataCharacteristicCallbacks(BLEManager* pBLEManager, bool* pInternalErrorFlag)
{
  pInternalError = pInternalErrorFlag;
  pBLEManagerInstance = pBLEManager;
}

void DataCharacteristicCallbacks::onStatus(BLECharacteristic* pCharacteristic, Status s, uint32_t code)
{
  if (s == Status::SUCCESS_NOTIFY || s == Status::SUCCESS_INDICATE)
  {
      xTaskNotifyGive(pBLEManagerInstance->BLESenderTaskHandle);
  }
  else
  {
      Serial.printf("ERROR: Failed to notify or indicate new data value. Status=%d Code=%d\n",s,code);
      *pInternalError = true;
  }
}

ServerCallbacks::ServerCallbacks(bool* pDeviceConnectedFlag)
{
  pDeviceConnected = pDeviceConnectedFlag;
}

void ServerCallbacks::onConnect(BLEServer* pServer)
{
  Serial.println("INFO: Device connected.");
  *pDeviceConnected = true;
}

void ServerCallbacks::onDisconnect(BLEServer* pServer)
{
  Serial.println("INFO: Device disconnected.");
  *pDeviceConnected = false;
}