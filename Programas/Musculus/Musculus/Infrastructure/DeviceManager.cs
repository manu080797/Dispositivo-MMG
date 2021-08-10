using System;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Musculus.Infrastructure
{
    public class DeviceManager
    {
        private readonly IBluetoothLE BLEService;
        private readonly TimeSpan TimeBetweenConnectionTrials = new TimeSpan(0, 0, 15);
        private readonly TimeSpan ConnectionWatchdogInterval = new TimeSpan(0, 0, 1);
        private bool ReceivedNewData = false;

        private double _samplingFrequency;
        public double SamplingFrequency
        {
            get => _samplingFrequency;
            set
            {
                if (_samplingFrequency != value)
                {
                    _samplingFrequency = value;
                    SamplingFrequencyChanged?.Invoke(this, new SamplingFrequencyChangedEventArgs(value));
                }
            }
        }
        public class SamplingFrequencyChangedEventArgs : EventArgs
        {
            public SamplingFrequencyChangedEventArgs(double newSamplingFrequency)
            {
                SamplingFrequency = newSamplingFrequency;
            }

            public double SamplingFrequency { get; set; }
        }

        public enum Status { Connected, Disconnected, Acquiring, Error };
        public Status DeviceStatus = Status.Disconnected;
        public event EventHandler DataReceived;
        public event EventHandler StatusChanged;
        public event EventHandler SamplingFrequencyChanged;
        public bool AutoConnectEnabled = true;

        public DeviceManager()
        {
            BLEService = DependencyService.Get<IBluetoothLE>();
            BLEService.PeriphericalStatusChanged += OnStatusChanged;
            BLEService.DataReceived += OnDataReceived;

            AutoConnect();

            Device.StartTimer(TimeBetweenConnectionTrials, AutoConnect);
        }

        private bool AutoConnect()
        {
            if (AutoConnectEnabled)
            {
                if (DeviceStatus == Status.Disconnected)
                {
                    Task.Run(() => BLEService.StartConnection(10000));
                }
                else if (DeviceStatus == Status.Error)
                {
                    Task.Run(() => BLEService.StartConnection(10000));
                }
            }
            return true;
        }

        private bool ConnectionWatchdog()
        {
            if (DeviceStatus == Status.Acquiring)
            {
                if (!ReceivedNewData)
                {
                    Console.WriteLine($"ERROR: Connection watchdog did not detect new data packets in {ConnectionWatchdogInterval.TotalSeconds} s. Assuming lost connection.");
                    DeviceStatus = Status.Error;
                    var args = new PeriphericalStatusChangedEventArgs(DeviceStatus);
                    OnStatusChanged(this, args);

                    return false;
                }
            }

            ReceivedNewData = false;

            return true;
        }

        public void StartConnection()
        {
            AutoConnectEnabled = true;
            Task.Run(() => BLEService.StartConnection(10000));
        }

        public void CloseConnection()
        {
            AutoConnectEnabled = false;
            BLEService.CloseConnection();
        }

        private void OnStatusChanged(object sender, EventArgs args)
        {
            Status newDeviceStatus = ((PeriphericalStatusChangedEventArgs) args).DeviceStatus;

            switch (newDeviceStatus)
            {
                case Status.Connected:
                    {
                        SamplingFrequency = BLEService.GetSamplingFrequency();
                        Console.WriteLine($"INFO: Device connected. Sampling frequency: {SamplingFrequency} Hz.");
                        
                        DeviceStatus = newDeviceStatus;
                        break;
                    }
                case Status.Disconnected:
                    {
                        if (DeviceStatus == Status.Acquiring)
                        {
                            Console.WriteLine("ERROR: Device disconnected while acquiring.");
                            DeviceStatus = Status.Error;
                        }
                        else if (DeviceStatus == Status.Connected)
                        {
                            Console.WriteLine("INFO: Device disconnected.");
                            DeviceStatus = newDeviceStatus;
                        }
                        break;
                    }
                case Status.Acquiring:
                    {
                        Console.WriteLine("INFO: Acquiring data.");
                        DeviceStatus = newDeviceStatus;

                        //Device.StartTimer(ConnectionWatchdogInterval, ConnectionWatchdog);

                        break;
                    }
                case Status.Error:
                    {
                        Console.WriteLine("ERROR: Detected error in BLE stack.");
                        DeviceStatus = newDeviceStatus;

                        break;
                    }
                default:
                    {
                        Console.WriteLine("ERROR: Unknown status detected.");
                        throw new ArgumentOutOfRangeException();
                    }
            }

            StatusChanged?.Invoke(this, args);
        }

        private void OnDataReceived(object sender, EventArgs args)
        {
            ReceivedNewData = true;
            DataReceived?.Invoke(this, args);
        }

        public void StopAcquiring()
        {
            if (DeviceStatus == Status.Acquiring)
            {
                BLEService.SendStopCommand();
            }
            else
            {
                Console.WriteLine($"WARNING: Unable to send STOP command to device. Device was not acquiring. Device status={DeviceStatus}");
            }
        }

        public void StartAcquiring()
        {
            if (DeviceStatus == Status.Connected)
            {
                BLEService.SendStartCommand();
            }
            else
            {
                if (DeviceStatus == Status.Disconnected)
                {
                    Console.WriteLine($"WARNING: Unable to send START command to device. Device is disconnected. Device status={DeviceStatus}");
                }
                else if (DeviceStatus == Status.Acquiring)
                {
                    Console.WriteLine($"WARNING: Unable to send START command to device. Device is already acquiring. Device status={DeviceStatus}");
                }
                else
                {
                    Console.WriteLine($"WARNING: Unable to send START command to device. Unkown error. Device status={DeviceStatus}");
                }
            }
        }
    }
}
