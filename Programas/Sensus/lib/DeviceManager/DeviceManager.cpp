#include <DeviceManager.h>
#include <esp32-hal-cpu.h>

void DeviceManager::InitializateDevice()
{
    bool communicationsSuccesful = CommunicationsManager.InitializateCommunications(&DeviceConnected, &Acquire, &InternalError);
    bool acquisitionSuccesful = AcquisitionManager.InitializateAcquisition(CommunicationsManager.Queue);

    // Set InternalError flag to true in case of error while initializating communications or data acquisition.
    if (!communicationsSuccesful)
    {
        Serial.printf("ERROR: Internal error while initializating communications manager.\n");
        InternalError = true;
    }
    if (!acquisitionSuccesful)
    {
        Serial.printf("ERROR: Internal error while initializating acquisition manager.\n");
        InternalError = true;
    }

    int stackSize = 8000; // TODO: determine how much is the minimum necessary stack size
    int taskPriority = 0;
    int core = 0;

    BaseType_t status1 = xTaskCreatePinnedToCore(StatusMonitor,
                                                "Status Monitor", 
                                                stackSize, 
                                                (void*)this, 
                                                taskPriority, 
                                                &StatusMonitorTaskHandle, 
                                                core);
    Serial.printf("INFO: Created Status Monitor task. status=%d\n", status1);

    #if ENABLE_DEBUG == 1
    BaseType_t status2 = xTaskCreatePinnedToCore(DebugLogger,
                                                "Debug Logger", 
                                                stackSize, 
                                                (void*)this, 
                                                taskPriority, 
                                                &DebugLoggerTaskHandle, 
                                                core);
    Serial.printf("INFO: Created Debug logger task. status=%d\n", status2);
    #endif

    InitializationFinished = true;
}

// Implements GRAFCET (state transtitioning)
void DeviceManager::StatusMonitor(void *pvParameters)
{
    DeviceManager *pDeviceManager = (DeviceManager*) pvParameters;

    for (;;)
    {
        switch(pDeviceManager->DeviceStatus)
        {
            case Initializating:
                if (pDeviceManager->InternalError)
                {
                    // Tasks

                    // New state
                    pDeviceManager->DeviceStatus = Error;
                }
                else
                {
                    if (pDeviceManager->InitializationFinished)
                    {
                        // Tasks

                        // New state
                        pDeviceManager->DeviceStatus = Disconnected;
                    };
                };
                break;

            case Disconnected:
                if (pDeviceManager->InternalError)
                {
                    // Tasks

                    // New state
                    pDeviceManager->DeviceStatus = Error;
                }
                else
                {
                    if (pDeviceManager->DeviceConnected)
                    {
                        // Tasks

                        // New state
                        pDeviceManager->DeviceStatus = Connected;
                    };
                };
                break;

            case Connected:
                if (pDeviceManager->InternalError)
                {
                    // Tasks

                    // New state
                    pDeviceManager->DeviceStatus = Error;
                }
                else
                {
                    if (pDeviceManager->DeviceConnected)
                    {
                        if (pDeviceManager->Acquire)
                        {
                            // Tasks
                            pDeviceManager->AcquisitionManager.Start();

                            // New state
                            pDeviceManager->DeviceStatus = Acquiring;
                        }
                        else
                        {
                            // Nothing to do
                        }
                    }
                    else
                    {
                        // Tasks
                        // TODO: Clean communications
                        pDeviceManager->AcquisitionManager.Clean();

                        // New state
                        pDeviceManager->DeviceStatus = Disconnected;
                    };
                };
                break;

            case Acquiring:
                if (pDeviceManager->InternalError)
                {
                    // Tasks

                    // New state
                    pDeviceManager->DeviceStatus = Error;
                }
                else
                {
                    if (pDeviceManager->Acquire)
                    {
                        // Nothing to do
                    }
                    else
                    {
                        // Tasks
                        #if ENABLE_DEBUG == 1
                        pDeviceManager->CommunicationsManager.Counter = 0;
                        #endif
                        pDeviceManager->AcquisitionManager.Stop();
                        xQueueReset(pDeviceManager->CommunicationsManager.Queue);
                        
                        // New state
                        pDeviceManager->DeviceStatus = Connected;
                    }
                    if (!pDeviceManager->DeviceConnected)
                    {
                        // Tasks

                        // New state
                        pDeviceManager->DeviceStatus = Error;
                    };
                };
                break;

            case Error:
                // Tasks
                Serial.println("ERROR: Detected internal error. Rebooting...\n\n");
                ESP.restart();
                break;
        }

        // Loop every 500 ms
        vTaskDelay(portTICK_PERIOD_MS*500);
    }
}

// Print debiug messages to console
#if ENABLE_DEBUG == 1
void DeviceManager::DebugLogger(void *pvParameters)
{
    DeviceManager *pDeviceManager = (DeviceManager*) pvParameters;
    
    for (;;)
    {
        Serial.printf("DEBUG: Elements read: ADC: %d, Filter 1: %d, Filter 2: %d, BLE: %d \n",
                  pDeviceManager->AcquisitionManager.AdcCounter,
                  pDeviceManager->AcquisitionManager.FilterCounter1,
                  pDeviceManager->AcquisitionManager.FilterCounter2,
                  pDeviceManager->CommunicationsManager.Counter);

        Serial.printf("DEBUG: Last acceleration values in BLE queue: Z1=%f Z2=%f \n",
                  pDeviceManager->CommunicationsManager.Data.Z1,
                  pDeviceManager->CommunicationsManager.Data.Z2);

        Serial.printf("DEBUG: Available space in queues: Filter1: %d/%d | Filter2: %d/%d | BLE %d/%d \n",
                  uxQueueSpacesAvailable(pDeviceManager->AcquisitionManager.Queue1),
                  (int)(INPUT_SIZE*1.5),
                  uxQueueSpacesAvailable(pDeviceManager->AcquisitionManager.Queue2),
                  (int)(INPUT_SIZE*1.5),
                  uxQueueSpacesAvailable(pDeviceManager->CommunicationsManager.Queue),
                  pDeviceManager->CommunicationsManager.QueueSize);

        Serial.printf("DEBUG: Device flags: \tInitializationFinished=%s, DeviceConnected=%s, Acquire=%s, InternalError=%s \n",
                  pDeviceManager->InitializationFinished ? "true":"false",
                  pDeviceManager->DeviceConnected ? "true":"false",
                  pDeviceManager->Acquire ? "true":"false",
                  pDeviceManager->InternalError ? "true":"false");
        
        Serial.printf("DEBUG: Device status: %s \n", (pDeviceManager->DeviceStatusToString()).c_str());

        Serial.printf("DEBUG: Current free heap size: %d. Mininum free heap size registered: %d \n\n", esp_get_free_heap_size(), esp_get_minimum_free_heap_size());

        if (pDeviceManager->DeviceStatus == Acquiring)
        {
            vTaskDelay(portTICK_PERIOD_MS*2000);
        }
        else
        {
            vTaskDelay(portTICK_PERIOD_MS*10000);
        }
    }
}

std::string DeviceManager::DeviceStatusToString()
{
    std::string name = "";
    switch(DeviceStatus)
    {
        case Initializating:
            name = "Initializating";
            break;

        case Disconnected:
            name = "Disconnected";
            break;

        case Connected:
            name = "Connected";
            break;

        case Acquiring:
            name = "Acquiring";
            break;

        case Error:
            name = "Error";
            break;
    }

    return name;
}
#endif