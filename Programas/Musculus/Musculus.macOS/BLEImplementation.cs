// WARNING: this file is not up to date. See Android implementation to fix this file.
// NOTE: this file can be shared with iOS implementation because macOS and iOS use the same API for Bluetooth LE.

using System;
using System.Threading.Tasks;
using MonoMac.CoreBluetooth;
using Xamarin.Forms;
using Musculus.Infrastructure;

// Inspirado en el tutorial de https://wojciechkulik.pl/xamarin-ios/how-to-communicate-with-bluetooth-low-energy-devices-on-ios

[assembly: Dependency(typeof(Musculus.Apple.BLEImplementation))]
namespace Musculus.Apple
{
    public class BLEImplementation : IDisposable, IBluetoothLE
    {
        private readonly CBCentralManager _manager = new CBCentralManager();

        // TODO: mover estas definiciones a código multiplataforma
        private readonly CBUUID _sensusServiceUUID = CBUUID.FromString("6e4c6bb0-3309-483a-9944-804fd3f16f10");
        private readonly CBUUID _accelerationDataCharacteristicUUID = CBUUID.FromString("e3d5dc41-e121-4b67-a889-77e240c1495e");
        private readonly CBUUID _commandCharacteristicUUID = CBUUID.FromString("0a004aa4-3c15-4764-bf7f-bcf41044ed94");
        private readonly int _scanDurationInMiliseconds = 10000;

        private CBPeripheral Peripheral = null;
        private CBService Service = null;
        private CBCharacteristic AccelerometerDataCharacteristic = null;
        private CBCharacteristic CommandCharacteristic = null;
        private bool IsScanning = false;
        private Int64 OrderingIndex = 0;

        public event EventHandler DataReceived;


        public BLEImplementation()
        {
        }

        public void Dispose()
        {
            Stop();
        }

        public void StartConnection()
        {
            OrderingIndex = 0;

            if (_manager.State != CBCentralManagerState.PoweredOn)
            {
                Console.WriteLine($"ERROR: CBManager state is different from PoweredOn state. Unable to start. State={_manager.State}");
            }
            else
            {
                Console.WriteLine("INFO: Scanning started");

                _manager.DisconnectedPeripheral += DisconnectedPeripheral;
                _manager.DiscoveredPeripheral += DiscoveredPeripheral;

                IsScanning = true;
                _manager.ScanForPeripherals(new CBUUID[0]);
                Task.Delay(_scanDurationInMiliseconds).Wait();

                try
                {
                    _manager.StopScan();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ERROR: Failed to stop scanning. {ex.Message}");
                }
                IsScanning = false;

                if (Peripheral != null)
                {
                    Console.WriteLine("INFO: Scanning stopped and found device.");
                }
                else
                {
                    Console.WriteLine("ERROR: Scan timeout. Could not find the device.");
                    Stop();
                }
            }
        }

        public void SendStopCommand()
        {
            WriteCommand("stop");
        }

        public void CloseConnection()
        {
            Stop();
        }

        private void DiscoveredPeripheral(object sender, CBDiscoveredPeripheralEventArgs args)
        {
            Console.WriteLine($"INFO: Discovered {args.Peripheral.Name} - {args.Peripheral.Identifier?.Description}");

            if (args.Peripheral.Name?.Substring(0, 6) == "Sensus")
            {
                Peripheral = args.Peripheral;

                try
                {
                    // We cannot perform other operations while scanning
                    if (IsScanning)
                    {
                        try
                        {
                            _manager.StopScan();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"ERROR: Failed to stop scanning. {ex.Message}");
                        }

                        IsScanning = false;
                    }

                    _manager.ConnectedPeripheral += ConnectedPeripheral;
                    _manager.ConnectPeripheral(Peripheral);
                    Console.WriteLine($"INFO: Connecting to {Peripheral.Name}");
                    _manager.DiscoveredPeripheral -= DiscoveredPeripheral;
                }
                catch
                {
                    Console.WriteLine($"ERROR: Failed to connect with {Peripheral.Name}");
                }
            }
        }

        private void ConnectedPeripheral(object sender, EventArgs args)
        {
            Console.WriteLine($"INFO: Connected to {Peripheral.Name}");
            Peripheral.DiscoveredService += DiscoveredService;
            Peripheral.DiscoverServices();
        }

        private void DiscoveredService(object sender, NSErrorEventArgs args)
        {
            foreach (CBService service in Peripheral.Services)
            {
                Console.WriteLine($"INFO: Found service. UUID:{service.UUID}");

                string discoveredServiceStringUUID = service.UUID.ToString().ToLowerInvariant();
                string sensusServiceStringUUID = _sensusServiceUUID.ToString().ToLowerInvariant();
                if (discoveredServiceStringUUID == sensusServiceStringUUID)
                {
                    Console.WriteLine($"INFO: Found Sensus service.");
                    Peripheral.DiscoveredService -= DiscoveredService;

                    Service = service;

                    CBUUID[] characteristics = new[] { _commandCharacteristicUUID, _accelerationDataCharacteristicUUID };

                    Peripheral.DiscoveredCharacteristic += DiscoveredCharacteristic;
                    Peripheral.DiscoverCharacteristics(Service);
                    return;
                }
            }

            Console.WriteLine($"ERROR: Sensus service not found.");
        }

        private void DiscoveredCharacteristic(object sender, CBServiceEventArgs args)
        {
            foreach (CBCharacteristic characteristic in Service.Characteristics)
            {
                Console.WriteLine($"INFO: Found characteristic. UUID:{characteristic.UUID}");

                string discoveredCharacteristicStringUUID = characteristic.UUID.ToString().ToLowerInvariant();
                string accelerationDataCharacteristicStringUUID = _accelerationDataCharacteristicUUID.ToString().ToLowerInvariant();
                string commandCharacteristicStringUUID = _commandCharacteristicUUID.ToString().ToLowerInvariant();

                if (discoveredCharacteristicStringUUID == accelerationDataCharacteristicStringUUID)
                {
                    AccelerometerDataCharacteristic = characteristic;
                }
                else if (discoveredCharacteristicStringUUID == commandCharacteristicStringUUID)
                {
                    CommandCharacteristic = characteristic;
                }
            }

            if (AccelerometerDataCharacteristic != null && CommandCharacteristic != null)
            {
                Peripheral.DiscoveredCharacteristic -= DiscoveredCharacteristic;
                Console.WriteLine($"INFO: Found all characteristics.");
                Peripheral.UpdatedCharacterteristicValue += UpdatedCharacterteristicValue;
                Peripheral.SetNotifyValue(true, AccelerometerDataCharacteristic);
                Task.Delay(1000).Wait();
                WriteCommand("start");
            }
        }

        private void UpdatedCharacterteristicValue(object sender, CBCharacteristicEventArgs args)
        {
            byte[] characteristicValue = args.Characteristic.Value.ToArray();

            int length = characteristicValue.Length;
            byte[] data = new byte[length];
            Array.Copy(characteristicValue, data, length);

            OrderingIndex++;
            DataReceived?.Invoke(this, new DataReceivedEventArgs(data, OrderingIndex));
        }

        private void WriteCommand(string command)
        {
            if (CommandCharacteristic != null && !IsScanning)
            {
                Peripheral.WriteValue(command, CommandCharacteristic, CBCharacteristicWriteType.WithoutResponse);
                Console.WriteLine($"INFO: Wrote {command} command in device.");
            }
        }

        private void UpdatedState(object sender, EventArgs args)
        {
            Console.WriteLine($"INFO: CBManager state changed to {_manager.State}");

            if (_manager.State != CBCentralManagerState.PoweredOn)
            {
                Console.WriteLine($"ERROR: CBManager state is different from PoweredOn state. Triggering stop command.");
                Stop();
            }
        }

        // BUG: this function is not being called when peripherical disconnects
        private void DisconnectedPeripheral(object sender, EventArgs args)
        {
            Console.WriteLine($"ERROR: Device disconected. Triggering stop command.");
            Stop();
        }

        private void Stop()
        {
            Console.WriteLine("INFO: Stopping.");

            // Unsubscribe from publishers and cancel pending connections
            _manager.DiscoveredPeripheral -= DiscoveredPeripheral;
            _manager.ConnectedPeripheral -= ConnectedPeripheral;
            _manager.DisconnectedPeripheral -= DisconnectedPeripheral;
            if (Peripheral != null)
            {
                Peripheral.UpdatedCharacterteristicValue -= UpdatedCharacterteristicValue;
                Peripheral.DiscoveredCharacteristic -= DiscoveredCharacteristic;
                Peripheral.DiscoveredService -= DiscoveredService;

                if (_manager.State == CBCentralManagerState.PoweredOn)
                {
                    try
                    {
                        _manager.CancelPeripheralConnection(Peripheral);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"WARNING: Error cancelling pending connections. {ex.Message}");
                    }
                }
            }

            // Dispose Bluetooth LE objects
            Peripheral?.Dispose();
            Service?.Dispose();
            AccelerometerDataCharacteristic?.Dispose();
            CommandCharacteristic?.Dispose();

            // Reset all references to Bluetooth LE objects
            Peripheral = null;
            Service = null;
            AccelerometerDataCharacteristic = null;
            CommandCharacteristic = null;
            IsScanning = false;

            // We should raise an event, set a flag or send a message to UI here
        }
    }
}
