using System;
using Xamarin.Forms;
using System.Threading.Tasks;
using Android.Bluetooth;
using Android.Bluetooth.LE;
using Android.Runtime;
using System.Collections.Generic;
using Android.OS;
using Musculus.Infrastructure;

/* Based on "Making Android BLE work" by Martijn van Welie in Medium (2019):
 * Part 1: https://medium.com/@martijn.van.welie/making-android-ble-work-part-1-a736dcd53b02
 * Part 2: https://medium.com/@martijn.van.welie/making-android-ble-work-part-2-47a3cdaade07
 * Part 3: https://medium.com/@martijn.van.welie/making-android-ble-work-part-3-117d3a8aee23
 */

[assembly: Dependency(typeof(Musculus.Droid.BLEImplementation))]
namespace Musculus.Droid
{
    public class BLEImplementation : IDisposable, IBluetoothLE
    {
        private readonly ParcelUuid _sensusServiceUUID = ParcelUuid.FromString("6e4c6bb0-3309-483a-9944-804fd3f16f10");
        private readonly ParcelUuid _accelerationDataCharacteristicUUID = ParcelUuid.FromString("e3d5dc41-e121-4b67-a889-77e240c1495e");
        private readonly ParcelUuid _commandCharacteristicUUID = ParcelUuid.FromString("0a004aa4-3c15-4764-bf7f-bcf41044ed94");
        private readonly ParcelUuid _samplingFrequencyCharacteristicUUID = ParcelUuid.FromString("b09ba3f6-e7e6-4f76-a969-e0c8da3167c2");
        private readonly ParcelUuid _notifyCharacteristicDescriptorUUID = ParcelUuid.FromString("00002902-0000-1000-8000-00805f9b34fb");
        private CustomScanCallback ScanCallback;
        private CustomBluetoothGattCallback BluetoothGattCallback;

        private BluetoothAdapter Adapter = null;
        private BluetoothLeScanner Scanner = null;
        private BluetoothGatt GattServer = null;
        private BluetoothDevice Peripheral = null;
        private BluetoothGattService Service = null;
        private BluetoothGattCharacteristic AccelerometerDataCharacteristic = null;
        private BluetoothGattCharacteristic CommandCharacteristic = null;
        private BluetoothGattCharacteristic SamplingFrequencyCharacteristic = null;
        private double SamplingFrequency = -1.0;

        public bool IsScanning = false;
        public event EventHandler DataReceived;
        public event EventHandler PeriphericalStatusChanged;

        private class CustomScanCallback : ScanCallback
        {
            private readonly BLEImplementation _bleImplementationInstance;

            public CustomScanCallback(BLEImplementation bleImplementationInstance) : base()
            {
                _bleImplementationInstance = bleImplementationInstance;
            }

            public override void OnScanResult([GeneratedEnum] ScanCallbackType callbackType, ScanResult result)
            {
                Console.WriteLine($"INFO: Succesful BLE scan.");

                if (_bleImplementationInstance.Peripheral == null)
                {
                    IList<ScanResult> results = new List<ScanResult>();
                    results.Add(result);

                    _bleImplementationInstance.FindSensusDevice(results);
                }
                else
                {
                    Console.WriteLine($"WARNING: Peripheral is already assigned (not null).");
                }
            }

            public override void OnScanFailed([GeneratedEnum] ScanFailure errorCode)
            {
                Console.WriteLine($"ERROR: Failed to scan. Error code: {errorCode}");

                var newDeviceStatus = DeviceManager.Status.Error;
                var args = new PeriphericalStatusChangedEventArgs(newDeviceStatus);
                _bleImplementationInstance.PeriphericalStatusChanged?.Invoke(_bleImplementationInstance, args);
                _bleImplementationInstance.CloseConnection();
            }
        };

        private class CustomBluetoothGattCallback : BluetoothGattCallback
        {
            private readonly BLEImplementation _bleImplementationInstance;

            public CustomBluetoothGattCallback(BLEImplementation bleImplementationInstance) : base()
            {
                _bleImplementationInstance = bleImplementationInstance;
            }

            public override void OnServicesDiscovered(BluetoothGatt gatt, [GeneratedEnum] GattStatus status)
            {
                if (_bleImplementationInstance.Service == null)
                {
                    if (status != GattStatus.Success)
                    {
                        Console.WriteLine($"ERROR: Service discovery failed. GattStatus={status}");

                        var newDeviceStatus = DeviceManager.Status.Error;
                        var args = new PeriphericalStatusChangedEventArgs(newDeviceStatus);
                        _bleImplementationInstance.PeriphericalStatusChanged?.Invoke(_bleImplementationInstance, args);
                        _bleImplementationInstance.CloseConnection();

                        return;
                    }
                    _bleImplementationInstance.FindSensusService();
                }
            }

            public override void OnCharacteristicChanged(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic)
            {
                /* Android Bluetooth stack reuses characteristics fields.
                 * If we get a new notification while processing, we miss the previous data.
                 * To prevent this, we make a copy of the data.
                 */
                int length = characteristic.GetValue().Length;
                byte[] data = new byte[length];
                Array.Copy(characteristic.GetValue(), data, length);

                _bleImplementationInstance.DataReceived?.Invoke(this, new DataReceivedEventArgs(data));
            }

            public override void OnCharacteristicRead(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic, GattStatus status)
            {
                if (characteristic.Uuid == _bleImplementationInstance.SamplingFrequencyCharacteristic.Uuid)
                {
                    var rawSamplingFrequency = _bleImplementationInstance.SamplingFrequencyCharacteristic.GetValue();

                    if (rawSamplingFrequency != null)
                    {
                        int lenght = rawSamplingFrequency.Length;

                        // ESP-32 is a Litle Endian CPU, we must reverse bytes if client is not little-endian
                        if (!BitConverter.IsLittleEndian)
                        {
                            Array.Reverse(rawSamplingFrequency, 0, lenght);
                        }

                        _bleImplementationInstance.SamplingFrequency = BitConverter.ToSingle(rawSamplingFrequency, 0);

                        // Enable notifications in AccelerometerDataCharacteristic to finish connection setup
                        _bleImplementationInstance.SetNotify(true, _bleImplementationInstance.AccelerometerDataCharacteristic);
                    }
                    else
                    {
                        var newDeviceStatus = DeviceManager.Status.Error;
                        var args = new PeriphericalStatusChangedEventArgs(newDeviceStatus);
                        _bleImplementationInstance.PeriphericalStatusChanged?.Invoke(_bleImplementationInstance, args);
                        _bleImplementationInstance.CloseConnection();
                    }
                }
            }

            public override void OnConnectionStateChange(BluetoothGatt gatt, [GeneratedEnum] GattStatus status, [GeneratedEnum] ProfileState newState)
            {
                base.OnConnectionStateChange(gatt, status, newState);

                if (status == GattStatus.Success)
                {
                    if (newState == ProfileState.Connected)
                    {
                        Bond bondState = _bleImplementationInstance.Peripheral.BondState;

                        if (bondState == Bond.Bonded || bondState == Bond.None)
                        {
                            Console.WriteLine($"INFO: Succesful bonding to device.");

                            // We let the Bluetooth stack finish pending work (this might not be necessary).
                            //Task.Delay(1000).Wait();

                            bool succesfulServiceDiscovery = _bleImplementationInstance.GattServer.DiscoverServices();
                            if (!succesfulServiceDiscovery)
                            {
                                Console.WriteLine($"ERROR: Failed to discover services.");

                                var newDeviceStatus = DeviceManager.Status.Error;
                                var args = new PeriphericalStatusChangedEventArgs(newDeviceStatus);
                                _bleImplementationInstance.PeriphericalStatusChanged?.Invoke(_bleImplementationInstance, args);
                                _bleImplementationInstance.CloseConnection();

                                return;
                            }
                        }
                        else if (bondState == Bond.Bonding)
                        {
                            /* Device is bonding. Cannot call DiscoverServices().
                             * If no encryption is used there is no bondig process so we should never get here.
                             */
                            throw new NotSupportedException();
                        }
                        else
                        {
                            throw new ArgumentOutOfRangeException();
                        }
                    }
                    else if (newState == ProfileState.Disconnected)
                    {
                        var newDeviceStatus = DeviceManager.Status.Disconnected;
                        var args = new PeriphericalStatusChangedEventArgs(newDeviceStatus);
                        _bleImplementationInstance.PeriphericalStatusChanged?.Invoke(_bleImplementationInstance, args);

                        return;
                    }
                    else
                    {
                        // We're connecting or disconnecting so we ignore the status.
                        return;
                    }
                }
                else
                {
                    // An error happened...figure out what happened! ...
                    Console.WriteLine($"ERROR: Lost connection with device because of internal error. ProfileState={newState} GattStatus={status}");

                    var newDeviceStatus = DeviceManager.Status.Error;
                    var args = new PeriphericalStatusChangedEventArgs(newDeviceStatus);
                    _bleImplementationInstance.PeriphericalStatusChanged?.Invoke(_bleImplementationInstance, args);
                    _bleImplementationInstance.CloseConnection();

                    return;
                }
            }

            public override void OnDescriptorWrite(BluetoothGatt gatt, BluetoothGattDescriptor descriptor, [GeneratedEnum] GattStatus status)
            {
                if (descriptor.Uuid.ToString() == _bleImplementationInstance._notifyCharacteristicDescriptorUUID.Uuid.ToString())
                {
                    Console.WriteLine($"INFO: Finished connection setup.");

                    var newDeviceStatus = DeviceManager.Status.Connected;
                    var args = new PeriphericalStatusChangedEventArgs(newDeviceStatus);
                    _bleImplementationInstance.PeriphericalStatusChanged?.Invoke(_bleImplementationInstance, args);
                }
            }
        }

        public BLEImplementation()
        {
            ScanCallback = new CustomScanCallback(this);
            BluetoothGattCallback = new CustomBluetoothGattCallback(this);
        }

        public void StartConnection(int scanDurationInMiliseconds)
        {
            //CloseConnection();
            if (Scanner != null)
            {
                Scanner.StopScan(ScanCallback);
            }

            Adapter = BluetoothAdapter.DefaultAdapter;
            Scanner = BluetoothAdapter.DefaultAdapter.BluetoothLeScanner;

            if (!Adapter.IsEnabled)
            {
                Console.WriteLine($"ERROR: Bluetooth is turned off.");

                var newDeviceStatus = DeviceManager.Status.Disconnected;
                var args = new PeriphericalStatusChangedEventArgs(newDeviceStatus);
                PeriphericalStatusChanged?.Invoke(this, args);
                CloseConnection();

                return;
            }

            if (Scanner != null)
            {
                IsScanning = true;
                Scanner.StartScan(ScanCallback);
                Console.WriteLine($"INFO: Started to scan Bluetooth LE adapters.");

                Task.Delay(scanDurationInMiliseconds).Wait();

                // When we find a Sensus device we stop scanning to connect.
                // If scan times out and IsScanning=true, it means that we could not find a Sensus device.
                if (IsScanning)
                {
                    Scanner.StopScan(ScanCallback);
                    Console.WriteLine("INFO: Scan timeout. Could not find the device.");

                    var newDeviceStatus = DeviceManager.Status.Disconnected;
                    var args = new PeriphericalStatusChangedEventArgs(newDeviceStatus);
                    PeriphericalStatusChanged?.Invoke(this, args);
                    CloseConnection();

                    return;
                }
            }
            else
            {
                Console.WriteLine($"ERROR: Scanner is null.");

                var newDeviceStatus = DeviceManager.Status.Error;
                var args = new PeriphericalStatusChangedEventArgs(newDeviceStatus);
                PeriphericalStatusChanged?.Invoke(this, args);
                CloseConnection();

                return;
            }
        }

        public void SendStartCommand()
        {
            DeviceManager.Status newDeviceStatus;

            if (WriteCommand("start"))
            {
                newDeviceStatus = DeviceManager.Status.Acquiring;
                var args = new PeriphericalStatusChangedEventArgs(newDeviceStatus);
                PeriphericalStatusChanged?.Invoke(this, args);
            }
            else
            {
                newDeviceStatus = DeviceManager.Status.Error;
                var args = new PeriphericalStatusChangedEventArgs(newDeviceStatus);
                PeriphericalStatusChanged?.Invoke(this, args);
                CloseConnection();
            }
        }

        public void SendStopCommand()
        {
            DeviceManager.Status newDeviceStatus;

            if (WriteCommand("stop"))
            {
                newDeviceStatus = DeviceManager.Status.Connected;
                var args = new PeriphericalStatusChangedEventArgs(newDeviceStatus);
                PeriphericalStatusChanged?.Invoke(this, args);
            }
            else
            {
                newDeviceStatus = DeviceManager.Status.Error;
                var args = new PeriphericalStatusChangedEventArgs(newDeviceStatus);
                PeriphericalStatusChanged?.Invoke(this, args);
                CloseConnection();
            }
        }

        private void FindSensusDevice(IList<ScanResult> scanResults)
        {
            foreach (var scanResult in scanResults)
            {
                Console.WriteLine($"INFO: Discovered {scanResult.Device.Name} - {scanResult.Device.Address}");

                if (scanResult.Device.Name?.Substring(0, 6) == "Sensus")
                {
                    Console.WriteLine($"INFO: Found sensus device.");

                    if (IsScanning)
                    {
                        Scanner.StopScan(ScanCallback);
                        IsScanning = false;
                    }

                    Connect(scanResult.Device);
                }
            }
        }

        private void Connect(BluetoothDevice device)
        {
            Peripheral = device;
            GattServer = Peripheral.ConnectGatt(Android.App.Application.Context, false, BluetoothGattCallback, BluetoothTransports.Le);
        }

        private void FindSensusService()
        {
            if (GattServer.Services != null)
            {
                IList<BluetoothGattService> services = GattServer.Services;
                foreach (BluetoothGattService service in services)
                {
                    Console.WriteLine($"INFO: Found service. UUID:{service.Uuid}");

                    string discoveredServiceStringUUID = service.Uuid.ToString().ToLowerInvariant();
                    string sensusServiceStringUUID = _sensusServiceUUID.ToString().ToLowerInvariant();
                    if (discoveredServiceStringUUID == sensusServiceStringUUID)
                    {
                        Console.WriteLine($"INFO: Found Sensus service.");
                        Service = service;

                        DiscoverCharacteristics();

                        return;
                    }
                }

                Console.WriteLine($"ERROR: Could not find Sensus service.");

                var newDeviceStatus = DeviceManager.Status.Error;
                var args = new PeriphericalStatusChangedEventArgs(newDeviceStatus);
                PeriphericalStatusChanged?.Invoke(this, args);
                CloseConnection();

                return;
            }
            else
            {
                Console.WriteLine($"ERROR: Service is null.");

                var newDeviceStatus = DeviceManager.Status.Error;
                var args = new PeriphericalStatusChangedEventArgs(newDeviceStatus);
                PeriphericalStatusChanged?.Invoke(this, args);
                CloseConnection();

                return;
            }
        }

        private void DiscoverCharacteristics()
        {
            if (Service.Characteristics != null)
            {
                foreach (BluetoothGattCharacteristic characteristic in Service.Characteristics)
                {
                    Console.WriteLine($"INFO: Found characteristic. UUID:{characteristic.Uuid}");

                    string discoveredCharacteristicStringUUID = characteristic.Uuid.ToString().ToLowerInvariant();
                    string accelerationDataCharacteristicStringUUID = _accelerationDataCharacteristicUUID.ToString().ToLowerInvariant();
                    string commandCharacteristicStringUUID = _commandCharacteristicUUID.ToString().ToLowerInvariant();
                    string samplingFrequencyCharacteristicStringUUID = _samplingFrequencyCharacteristicUUID.ToString().ToLowerInvariant();

                    if (discoveredCharacteristicStringUUID == accelerationDataCharacteristicStringUUID)
                    {
                        AccelerometerDataCharacteristic = characteristic;
                    }
                    else if (discoveredCharacteristicStringUUID == commandCharacteristicStringUUID)
                    {
                        CommandCharacteristic = characteristic;
                    }
                    else if (discoveredCharacteristicStringUUID == samplingFrequencyCharacteristicStringUUID)
                    {
                        SamplingFrequencyCharacteristic = characteristic;
                    }
                }

                if (AccelerometerDataCharacteristic != null && CommandCharacteristic != null && SamplingFrequencyCharacteristic != null)
                {
                    Console.WriteLine($"INFO: Found all characteristics.");
                    if (!GattServer.ReadCharacteristic(SamplingFrequencyCharacteristic))
                    {
                        var newDeviceStatus = DeviceManager.Status.Error;
                        var args = new PeriphericalStatusChangedEventArgs(newDeviceStatus);
                        PeriphericalStatusChanged?.Invoke(this, args);
                        CloseConnection();

                        return;
                    }
                }
                else
                {
                    Console.WriteLine($"ERROR: Characteristic is null.");

                    var newDeviceStatus = DeviceManager.Status.Error;
                    var args = new PeriphericalStatusChangedEventArgs(newDeviceStatus);
                    PeriphericalStatusChanged?.Invoke(this, args);
                    CloseConnection();

                    return;
                }
            }
            else
            {
                Console.WriteLine($"ERROR: Services.Characteristics is null.");

                var newDeviceStatus = DeviceManager.Status.Error;
                var args = new PeriphericalStatusChangedEventArgs(newDeviceStatus);
                PeriphericalStatusChanged?.Invoke(this, args);
                CloseConnection();

                return;
            }
        }

        private void SetNotify(bool enable, BluetoothGattCharacteristic characteristic)
        {
            if (characteristic != null)
            {
                BluetoothGattDescriptor descriptor = characteristic.GetDescriptor(_notifyCharacteristicDescriptorUUID.Uuid);
                if (descriptor != null)
                {
                    byte[] value = BitConverter.GetBytes((ushort)0x00);

                    if (enable)
                    {
                        int properties = (int)characteristic.Properties;
                        if ((properties & (int)GattProperty.Notify) > 0)
                        {
                            value = BitConverter.GetBytes((ushort)0x01);
                            Console.WriteLine($"INFO: Acclereration data characteristic has Notify property.");
                        }
                        else if ((properties & (int)GattProperty.Indicate) > 0)
                        {
                            value = BitConverter.GetBytes((ushort)0x02);
                            Console.WriteLine($"INFO: Acclereration data characteristic has Indicate property.");
                        }
                        else
                        {
                            Console.WriteLine($"ERROR: Acclereration data characteristic does not have Notify or Indicate property.");

                            var newDeviceStatus = DeviceManager.Status.Error;
                            var args = new PeriphericalStatusChangedEventArgs(newDeviceStatus);
                            PeriphericalStatusChanged?.Invoke(this, args);
                            CloseConnection();

                            return;
                        }
                    }

                    if (GattServer != null)
                    {
                        GattServer.SetCharacteristicNotification(descriptor.Characteristic, enable);
                        descriptor.SetValue(value);
                        bool result = GattServer.WriteDescriptor(descriptor);
                        if (result)
                        {
                            Console.WriteLine($"INFO: Wrote to CCC in Acceleration data characteristic. Value={BitConverter.ToString(value)}");
                        }
                        else
                        {
                            Console.WriteLine($"ERROR: Could not write CCC descriptor in Acceleration data characteristic.");

                            var newDeviceStatus = DeviceManager.Status.Error;
                            var args = new PeriphericalStatusChangedEventArgs(newDeviceStatus);
                            PeriphericalStatusChanged?.Invoke(this, args);
                            CloseConnection();

                            return;
                        }

                    }
                    else
                    {
                        Console.WriteLine($"ERROR: GattServer is null.");

                        var newDeviceStatus = DeviceManager.Status.Error;
                        var args = new PeriphericalStatusChangedEventArgs(newDeviceStatus);
                        PeriphericalStatusChanged?.Invoke(this, args);
                        CloseConnection();

                        return;
                    }
                }
                else
                {
                    Console.WriteLine($"ERROR: Notify characteristic descriptor is null.");

                    var newDeviceStatus = DeviceManager.Status.Error;
                    var args = new PeriphericalStatusChangedEventArgs(newDeviceStatus);
                    PeriphericalStatusChanged?.Invoke(this, args);
                    CloseConnection();

                    return;
                }
            }
            else
            {
                Console.WriteLine($"ERROR: Characteristic is null.");

                var newDeviceStatus = DeviceManager.Status.Error;
                var args = new PeriphericalStatusChangedEventArgs(newDeviceStatus);
                PeriphericalStatusChanged?.Invoke(this, args);
                CloseConnection();

                return;
            }
        }

        public bool WriteCommand(string command)
        {
            if (CommandCharacteristic != null)
            {
                CommandCharacteristic.SetValue(command);
                CommandCharacteristic.WriteType = GattWriteType.NoResponse;

                if (GattServer != null)
                {
                    bool succesfulWrite = GattServer.WriteCharacteristic(CommandCharacteristic);
                    if (succesfulWrite)
                    {
                        Console.WriteLine($"INFO: Wrote {command} command in device.");
                        return true;
                    }
                    else
                    {
                        Console.WriteLine($"ERROR: Could not write {command} command to device.");
                        return false;
                    }
                }
                else
                {
                    Console.WriteLine($"ERROR: GattServer is null.");
                    return false;
                }
            }
            else
            {
                Console.WriteLine($"ERROR: Command characteristic is null. Disconecting.");
                return false;
            }
        }

        public void CloseConnection()
        {
            var newDeviceStatus = DeviceManager.Status.Disconnected;
            var args = new PeriphericalStatusChangedEventArgs(newDeviceStatus);
            PeriphericalStatusChanged?.Invoke(this, args);

            if (GattServer != null)
            {
                GattServer.Disconnect();
                GattServer.Close();
                GattServer.Dispose();
                GattServer = null;
            }
            if (Peripheral != null)
            {
                Peripheral.Dispose();
                Peripheral = null;
            }

            if (Service != null)
            {
                Service.Dispose();
                Service = null;
            }

            if (AccelerometerDataCharacteristic != null)
            {
                AccelerometerDataCharacteristic.Dispose();
                AccelerometerDataCharacteristic = null;
            }

            if (CommandCharacteristic != null)
            {
                CommandCharacteristic.Dispose();
                CommandCharacteristic = null;
            }

            if (SamplingFrequencyCharacteristic != null)
            {
                SamplingFrequencyCharacteristic.Dispose();
                SamplingFrequencyCharacteristic = null;
            }

            if (Adapter != null)
            {
                Adapter.Dispose();
                Adapter = null;
            }

            if (Adapter != null)
            {
                Adapter.Dispose();
                Adapter = null;
            }
            IsScanning = false;
        }

        public double GetSamplingFrequency()
        {
            return SamplingFrequency;
        }

        public void Dispose()
        {
            CloseConnection();
        }
    }
}
