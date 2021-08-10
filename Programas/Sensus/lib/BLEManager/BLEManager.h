#ifndef   BLEMANAGER_H
#define   BLEMANAGER_H

#include <BLEDevice.h>
#include <BLEUtils.h>
#include <BLEServer.h>
#include <BLE2902.h>
#include <ADCManager.h>
#include <Arduino.h>

#define SERVICE_UUID "6e4c6bb0-3309-483a-9944-804fd3f16f10"
#define DATA_CHARACTERISTIC_UUID "e3d5dc41-e121-4b67-a889-77e240c1495e"
#define COMMAND_CHARACTERISTIC_UUID "0a004aa4-3c15-4764-bf7f-bcf41044ed94"
#define SAMPLINGFREQUENCY_CHARACTERISTIC_UUID "b09ba3f6-e7e6-4f76-a969-e0c8da3167c2"
#define START_COMMAND "start"
#define STOP_COMMAND "stop"
#define SAMPLES_PER_BLE_PACKET 2 // macOS admits a notify packet of upto 101 bytes (12 samples per packet) and android upto 20 bytes (2 samples per packet)
#define BLEQUEUE_SIZE 100

class BLEManager
{
    public:
        TaskHandle_t BLESenderTaskHandle = NULL;
        QueueHandle_t Queue = NULL;
        int Counter = 0;
        int QueueSize = 0;
        AccelerationData Data;
        BLECharacteristic * pCommand_Characteristic;
        BLECharacteristic * pData_Characteristic;
        BLECharacteristic * pSamplingFrequency_Characteristic;

        bool InitializateCommunications(bool* pDeviceConnected, bool* pAcquireFlag, bool* pInternalError);
        void UpdateDataValue();
        
    private:
        static void BLESender(void *pvParameters);

        uint8_t PackedData[SAMPLES_PER_BLE_PACKET*8];
        
        BLEServer * pServer;
        BLEService * pService;
        BLEAdvertising * pAdvertising;
};

class CommandCharacteristicCallbacks : public BLECharacteristicCallbacks
{
    public:
        void onWrite(BLECharacteristic* pCharacteristic);
        CommandCharacteristicCallbacks(BLEManager* pBLEManager, bool* pAcquireFlag);
    
    private:
        BLEManager * pBLEManagerInstance;
        bool * pAcquire;
};

class DataCharacteristicCallbacks : public BLECharacteristicCallbacks
{
    public:
        void onStatus(BLECharacteristic* pCharacteristic, Status s, uint32_t code);
        DataCharacteristicCallbacks(BLEManager* pBLEManager, bool* pInternalErrorFlag);
    
    private:
        BLEManager * pBLEManagerInstance;
        bool * pInternalError;
};

class ServerCallbacks : public BLEServerCallbacks
{
    public:
        void onConnect(BLEServer* pServer);
        void onDisconnect(BLEServer* pServer);
        ServerCallbacks(bool* pDeviceConnectedFlag);
    
    private:
        bool * pDeviceConnected;
};

#endif