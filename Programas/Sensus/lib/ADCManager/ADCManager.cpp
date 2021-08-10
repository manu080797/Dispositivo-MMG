#include <ADCManager.h>
#include <Testing.h>
#include <FIRFilterCoefficients.h>
#include <ADCCalibration.h>

TaskHandle_t FilterStage1TaskHandle;
TaskHandle_t FilterStage2TaskHandle;
QueueHandle_t FilterInputQueue1 = NULL;
QueueHandle_t FilterInputQueue2 = NULL;
uint32_t  LocalAdcCounter = 0;
portMUX_TYPE DRAM_ATTR AdcBufferMux = portMUX_INITIALIZER_UNLOCKED;

// Stage 1 filter static memory
static float Delay1_Z1[FILTER_SIZE];
static float Delay1_Z2[FILTER_SIZE];
static float Input1_Z1[INPUT_SIZE];
static float Input1_Z2[INPUT_SIZE];
static float Output1_Z1[OUTPUT_SIZE];
static float Output1_Z2[OUTPUT_SIZE];

// Stage 2 filter static memory
static float Delay2_Z1[FILTER_SIZE];
static float Delay2_Z2[FILTER_SIZE];
static float Input2_Z1[INPUT_SIZE];
static float Input2_Z2[INPUT_SIZE];
static float Output2_Z1[OUTPUT_SIZE];
static float Output2_Z2[OUTPUT_SIZE];

bool ADCManager::InitializateAcquisition(QueueHandle_t communicationsQueue)
{
    // Set-up Harware Timer
    InitializateTimer();
    Serial.println("INFO: Initializated Hardware Timer.");

    // Set-up ADC
    InitializateADC();
    Serial.println("INFO: Initializated ADC.");

    // Set-up DAC
    InitializateDAC();
    Serial.println("INFO: Initializated DAC.");

    // Set-up Filters
    InitializateFilter();
    Serial.println("INFO: Initializated Filters.");

    // Check for unhandled errors
    if (pTimer == NULL)
    {
        Serial.println("ERROR: Could not create Timer (is NULL).");
        // Returns false if error detected while initializating
        return false;
    }
    if (communicationsQueue == NULL)
    {
        Serial.println("ERROR: Communications queue is NULL.");
        // Returns false if error detected while initializating
        return false;
    }

    // Set communications queue
    CommunicationsQueue = communicationsQueue;

    // Bind instance queue handles to static filter queues (to make queues availabe to parent class of ADCManager)
    Queue1 = FilterInputQueue1;
    Queue2 = FilterInputQueue2;

    // Print Configuration to console
    Serial.printf("\nINFO: Acquisition configuration:\n");
    Serial.printf("      ADC Sampling Rate = %6.1f Hz\n", 1000000.0/SAMPLING_PERIOD);
    Serial.printf("      Decimation = %d\n", DECIMATION*DECIMATION);
    Serial.printf("      Decimated Sampling Rate = %4.1f Hz\n", 1000000.0/SAMPLING_PERIOD/DECIMATION/DECIMATION);
    Serial.printf("      Filter window length = %d\n\n", FILTER_SIZE);

    // Returns true if succesfull initialization
    return true;
}

void ADCManager::Start()
{
    timerAlarmEnable(pTimer);
    Serial.println("INFO: Started Hardware Timer.");
}

void ADCManager::Stop()
{
    timerAlarmDisable(pTimer);
    Serial.println("INFO: Stoped Hardware Timer.");

    xQueueReset(FilterInputQueue1);
    xQueueReset(FilterInputQueue2);
    Serial.println("INFO: Reseted Filter queues.");

    LocalAdcCounter = 0;
    AdcCounter = 0;
    FilterCounter1 = 0;
    FilterCounter2 = 0;
    Serial.println("INFO: Reseted counters.");
}

void ADCManager::Clean()
{
    Serial.println("INFO: Cleaning ADCManager.");
    LocalAdcCounter = 0;
    AdcCounter = 0;

    if (FilterStage1TaskHandle != NULL)
    {
        vTaskDelete(FilterStage1TaskHandle);
        FilterStage1TaskHandle = NULL;
    }
    if (FilterStage2TaskHandle != NULL)
    {
        vTaskDelete(FilterStage2TaskHandle);
        FilterStage2TaskHandle = NULL;
    }
    if (FilterInputQueue1 != NULL)
    {
        vQueueDelete(FilterInputQueue1);
        FilterInputQueue1 = NULL;
    }
    if (FilterInputQueue2 != NULL)
    {
        vQueueDelete(FilterInputQueue2);
        FilterInputQueue2 = NULL;
    }
    if (pTimer != NULL)
    {
        Serial.println("INFO: Stopping hardware timer...");
        timerEnd(pTimer);
        pTimer = NULL;
    }

    InitializateTimer();
    InitializateFilter();
}

void ADCManager::InitializateTimer()
{
    /* 
        Timer tick period depends on Application Clock which runs
        at 80 MHz. The prescaler reduces the number of ticks interrumps
        by 80 (10 MHz). Then, the sampling period, set as a number of
        ticks at 10 Mhz, is given in microseconds.
    */
    int prescaler = 80;
    int timerID = 0;
    pTimer = timerBegin(timerID, prescaler, true);
    timerAttachInterrupt(pTimer, &OnTimerTick, true);
    timerAlarmWrite(pTimer, SAMPLING_PERIOD, true);
}

void ADCManager::InitializateADC()
{
    /* 
        Set ADC bit width and attenuation. Then measure once
        to make sure that ADC internal registers get set properly.
    */
    adc1_config_width(ADC_WIDTH_BIT_12);
    for (int channel = 0; channel < 8; channel++)
    {
        adc1_config_channel_atten((adc1_channel_t)channel, ADC_ATTEN_DB_11);
        int reading = adc1_get_raw((adc1_channel_t)channel);

        #if ENABLE_DEBUG == 1
        printf("DEBUG: ADC initialization. Raw reading: %d Channel: %d\n", reading, channel);
        #endif
    }
}

void ADCManager::InitializateDAC()
{
    /* 
    Initializate DAC for voltage reference
    */
    dac_output_enable(Z1_VREF_CHANNEL);
    dac_output_enable(Z2_VREF_CHANNEL);

    // When ADC is set with 11dB attenuation, the reading range is [150; 2550] mV.
    // Taking this into account, the desired Vref is 1350 mV.
    // If VCC = 3300 mV, then we need a 
    dac_output_voltage(Z1_VREF_CHANNEL, int(255/2.444444));
    dac_output_voltage(Z2_VREF_CHANNEL, int(255/2.444444));
}

void ADCManager::InitializateFilter()
{
    if (INPUT_SIZE % OUTPUT_SIZE == 0)
    {
        if (INPUT_SIZE/OUTPUT_SIZE != DECIMATION)
        {
            Serial.printf("ERROR: %d must be %d times %d.",INPUT_SIZE, DECIMATION, OUTPUT_SIZE);
        }
    }
    else
    {
        Serial.printf("ERROR: %d must be %d times %d.",INPUT_SIZE, DECIMATION, OUTPUT_SIZE);
    }

    FilterInputQueue1 = xQueueCreate((int)(INPUT_SIZE*1.5), sizeof(AdcData));
    FilterInputQueue2 = xQueueCreate((int)(INPUT_SIZE*1.5), sizeof(AccelerationData));

    if (FilterInputQueue1 == NULL)
    {
        Serial.println("ERROR: NULL pointer detected in Filter 1 Input Queue handle.");
    }
    if (FilterInputQueue2 == NULL)
    {
        Serial.println("ERROR: NULL pointer detected in Filter 2 Input Queue handle.");
    }

    int stackSize = 30000; // TODO: determine how much is the minimum necessary stack size
    int taskPriority = 1;
    int core = 1;
    xTaskCreatePinnedToCore(FilterStage1,
                          "FilterStage1",
                          stackSize,
                          (void*) this,
                          taskPriority, 
                          &FilterStage1TaskHandle, 
                          core);

    stackSize = 30000; // TODO: determine how much is the minimum necessary stack size
    taskPriority = 4;
    core = 0;
    xTaskCreatePinnedToCore(FilterStage2,
                          "FilterStage2",
                          stackSize,
                          (void*) this,
                          taskPriority, 
                          &FilterStage2TaskHandle, 
                          core);
}

// Harware timer interrump handler
AdcData tmp; // Temporary data storage struct
bool FilterQueueOverflow = false;
void IRAM_ATTR ADCManager::OnTimerTick()
{
    BaseType_t xHigherPriorityTaskWoken = pdFALSE;

    // Low-level ADC1 read (Channel 1)
    SENS.sar_meas_start1.sar1_en_pad = (1 << Z1_CHANNEL); // Set channel
    while (SENS.sar_slave_addr1.meas_status != 0); // Wait for ADC to be in ready state
    SENS.sar_meas_start1.meas1_start_sar = 0; // Stop any previous conversion
    SENS.sar_meas_start1.meas1_start_sar = 1; // Start conversion
    while (SENS.sar_meas_start1.meas1_done_sar == 0); // Block until conversion finishes
    tmp.Z1 = AdcRawToMilivolt[SENS.sar_meas_start1.meas1_data_sar]; // Apply non-linear correction and save in tmp storage

    // Low-level ADC1 read (Channel 2)
    SENS.sar_meas_start1.sar1_en_pad = (1 << Z2_CHANNEL); // Set channel
    while (SENS.sar_slave_addr1.meas_status != 0); // Wait for ADC to be in ready state
    SENS.sar_meas_start1.meas1_start_sar = 0; // Stop any previous conversion
    SENS.sar_meas_start1.meas1_start_sar = 1; // Start conversion
    while (SENS.sar_meas_start1.meas1_done_sar == 0); // Block until conversion finishes
    tmp.Z2 = AdcRawToMilivolt[SENS.sar_meas_start1.meas1_data_sar]; // Apply non-linear correction and save in tmp storage

    LocalAdcCounter++;

    // Send acquired data to Filter 1 queue
    FilterQueueOverflow = !xQueueSendFromISR(FilterInputQueue1, &tmp, &xHigherPriorityTaskWoken);
}


void ADCManager::FilterStage1(void *pvParameters)
{
    ADCManager *pAdcManager = (ADCManager*) pvParameters;

    // Initializate FIR filter
    fir_f32_t firFilterZ1;
    fir_f32_t firFilterZ2;
    dsps_fird_init_f32(&firFilterZ1, FilterCoefficients, Delay1_Z1, FILTER_SIZE, DECIMATION, 0);
    dsps_fird_init_f32(&firFilterZ2, FilterCoefficients, Delay1_Z2, FILTER_SIZE, DECIMATION, 0);
    
    Serial.println("INFO: Started stage 1 filter task.");

    for (;;)
    {
        // Pull input from FilterInputQueue1
        AdcData data;
        for (int i = 0; i < INPUT_SIZE; i++)
        {
            xQueueReceive(FilterInputQueue1, &data, portMAX_DELAY);
            #if ENABLE_TEST_SIGNAL == 0
                Input1_Z1[i] = data.Z1;
                Input1_Z2[i] = data.Z2;
            #elif ENABLE_TEST_SIGNAL == 1
                Input1_Z1[i] = (uint16_t) (TestSignal[TestSignalPosition % TEST_SIGNAL_LENGHT] + Normal(gen));
                Input1_Z2[i] = (uint16_t) (TestSignal[TestSignalPosition++ % TEST_SIGNAL_LENGHT]);
            #endif

            // Check for Filter Queue overflow
            if (FilterQueueOverflow)
            {
                Serial.println("WARNING: Filter queue overflow. Posible loss of data. Try increasing sampling period.");
            }
        }
        
        // Apply FIR filter (Stage 1)
        int output1_z1_length = dsps_fird_f32_ae32(&firFilterZ1, Input1_Z1, Output1_Z1, INPUT_SIZE);
        int output1_z2_length = dsps_fird_f32_ae32(&firFilterZ2, Input1_Z2, Output1_Z2, INPUT_SIZE);

        // Check that output has correct size
        if ((output1_z1_length != OUTPUT_SIZE) or (output1_z2_length != OUTPUT_SIZE))
        {
            Serial.println("ERROR: Stage 1 FIR Filter output size is wrong. Must be equal to OUTPUT_SIZE.");
        }

        // Push output to FilterInputQueue2
        AccelerationData filteredData;
        for (int i = 0; i < OUTPUT_SIZE; i++)
        {
            filteredData.Z1 = Output1_Z1[i];
            filteredData.Z2 = Output1_Z2[i];
            pAdcManager->FilterCounter1++;
            xQueueSend(FilterInputQueue2, &filteredData, portMAX_DELAY);

            // Check for Filter Queue overflow
            if (FilterQueueOverflow)
            {
                Serial.println("WARNING: Filter queue overflow. Posible loss of data. Try increasing sampling period.");
            }
        }
    }
}

void ADCManager::FilterStage2(void *pvParameters)
{
    ADCManager *pAdcManager = (ADCManager*) pvParameters;

    // Initializate FIR filter
    fir_f32_t firFilterZ1;
    fir_f32_t firFilterZ2;
    dsps_fird_init_f32(&firFilterZ1, FilterCoefficients, Delay2_Z1, FILTER_SIZE, DECIMATION, 0);
    dsps_fird_init_f32(&firFilterZ2, FilterCoefficients, Delay2_Z2, FILTER_SIZE, DECIMATION, 0);

    Serial.println("INFO: Started stage 2 filter task.");

    for (;;)
    {
        // Pull input form FilterInputQueue2
        AccelerationData data;
        for (int i = 0; i < INPUT_SIZE; i++)
        {
            xQueueReceive(FilterInputQueue2, &data, portMAX_DELAY);
            Input2_Z1[i] = data.Z1;
            Input2_Z2[i] = data.Z2;

            // Check for Filter Queue overflow
            if (FilterQueueOverflow)
            {
                Serial.println("WARNING: Filter queue overflow. Posible loss of data. Try increasing sampling period.");
            }
        }
        
        // Apply FIR filter (Stage 2)
        int output2_z1_length = dsps_fird_f32_ae32(&firFilterZ1, Input2_Z1, Output2_Z1, INPUT_SIZE);
        int output2_z2_length = dsps_fird_f32_ae32(&firFilterZ2, Input2_Z2, Output2_Z2, INPUT_SIZE);

        // Check that output has correct size
        if ((output2_z1_length != OUTPUT_SIZE) or (output2_z2_length != OUTPUT_SIZE))
        {
            Serial.println("ERROR: Stage 2 FIR Filter output size is wrong. Must be equal to OUTPUT_SIZE.");
        }

        // Push output to BLEQueue and convert to mV
        AccelerationData filteredData;
        for (int i = 0; i < OUTPUT_SIZE; i++)
        {
            filteredData.Z1 = Output2_Z1[i];
            filteredData.Z2 = Output2_Z2[i];
            pAdcManager->FilterCounter2++;
            xQueueSend(pAdcManager->CommunicationsQueue, &filteredData, portMAX_DELAY);

            // Check for Filter Queue overflow
            if (FilterQueueOverflow)
            {
                Serial.println("WARNING: Filter queue overflow. Posible loss of data. Try increasing sampling period.");
            }
        }
        pAdcManager->AdcCounter = LocalAdcCounter;
    }
}