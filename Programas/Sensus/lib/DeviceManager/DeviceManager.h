#ifndef   DEVICEMANAGER_H
#define   DEVICEMANAGER_H

#define ENABLE_DEBUG 1 // Set to 1 to enable debug messages to console, set 0 to disable

#include <Arduino.h>
#include <BLEManager.h>
#include <ADCManager.h>

enum device_status_t {Connected, Disconnected, Error, Acquiring, Initializating};

class DeviceManager
{
    public:
        void InitializateDevice();

    private:
        bool DeviceConnected = false;
        bool Acquire = false;
        bool InitializationFinished = false;
        bool InternalError = false;
        
        device_status_t DeviceStatus = Initializating;
        
        TaskHandle_t StatusMonitorTaskHandle = NULL;
        TaskHandle_t DebugLoggerTaskHandle = NULL;

        ADCManager AcquisitionManager;
        BLEManager CommunicationsManager;

        static void StatusMonitor(void *pvParameters);
        static void DebugLogger(void *pvParameters);
        std::string DeviceStatusToString();
};

#endif