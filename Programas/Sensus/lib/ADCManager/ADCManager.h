#ifndef   ADCMANAGER_H
#define   ADCMANAGER_H

#include <Arduino.h>
#include <soc/sens_struct.h>
#include <driver/adc.h>
#include <driver/dac.h>
#include <dsps_fir.h>

// ADC1 Channels
#define Z1_CHANNEL 6
#define Z2_CHANNEL 7

// DAC Channels
#define Z1_VREF_CHANNEL DAC_CHANNEL_1
#define Z2_VREF_CHANNEL DAC_CHANNEL_2

// Acquisition configuration
#define SAMPLING_PERIOD 39 //Minimum 39 us (=1/Fs)
#define DECIMATION 8 // Filter decimation factor
#define OUTPUT_SIZE 100 // Communications queue size
#define INPUT_SIZE OUTPUT_SIZE*DECIMATION // Filter input queue size

struct AccelerationData
{
    float Z1;
    float Z2;
};
struct AdcData
{
    double Z1;
    double Z2;
};

class ADCManager
{
    public:
        bool InitializateAcquisition(QueueHandle_t communicationsQueue);

        void Start();
        void Stop();
        void Clean();

        int AdcCounter = 0;
        int FilterCounter1 = 0;
        int FilterCounter2 = 0;

        QueueHandle_t Queue1 = NULL;
        QueueHandle_t Queue2 = NULL;

    private:
        hw_timer_t * pTimer = NULL;
        QueueHandle_t CommunicationsQueue = NULL;

        void InitializateTimer();
        void InitializateADC();
        void InitializateDAC();
        void InitializateFilter();

        static void IRAM_ATTR OnTimerTick();
        static void FilterStage1(void *pvParameters);
        static void FilterStage2(void *pvParameters);
};

#endif