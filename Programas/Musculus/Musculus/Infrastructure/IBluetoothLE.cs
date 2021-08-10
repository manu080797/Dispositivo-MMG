using System;
using static Musculus.Infrastructure.DeviceManager;

namespace Musculus.Infrastructure
{
    public interface IBluetoothLE
    {
        void StartConnection(int scanDurationInMiliseconds);
        void SendStartCommand();
        void SendStopCommand();
        void CloseConnection();
        double GetSamplingFrequency();
        event EventHandler DataReceived;
        event EventHandler PeriphericalStatusChanged;
    }

    public class DataReceivedEventArgs : EventArgs
    {
        public DataReceivedEventArgs(byte[] data)
        {
            Data = data;
        }

        public byte[] Data { get; set; }
    }

    public class PeriphericalStatusChangedEventArgs : EventArgs
    {
        public PeriphericalStatusChangedEventArgs(Status status)
        {
            DeviceStatus = status;
        }

        public Status DeviceStatus { get; set; }
    }
}
