using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Musculus.Models;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace Musculus.Infrastructure
{
    public class AcquisitionManager
    {
        private const string START_SIGNAL = "START SIGNAL";
        private const string STOP_SIGNAL = "STOP SIGNAL";
        private Int64 LastIndex = 0;
        public List<AccelerationData> DataList = new List<AccelerationData>();
        public IBluetoothLE BLEService;

        public AcquisitionManager()
        {
            BLEService = DependencyService.Get<IBluetoothLE>();
            BLEService.DataReceived += OnReadingReceived;
        }

        public void SaveToDisk()
        {
            int index = 0;

            string folder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string filePath = $"{folder}/Sensus Data {index}.csv";

            if (DataList.Count == 0)
            {
                Console.WriteLine("Error: No measurements to save.");
                return;
            }

            while (File.Exists(filePath))
            {
                index += 1;
                filePath = $"{folder}/Sensus Data {index}.csv";
            }

            using (var writer = new StreamWriter(filePath))
            {
                writer.WriteLine($"Index; Z1; Z2");

                AccelerationData accelerationData;
                for (int i = 0; i < DataList.Count; i++)
                {
                    accelerationData = DataList[i];
                    writer.WriteLine($"{i}; {accelerationData.Z1}; {accelerationData.Z2}");
                    if (i % 1000 == 0)
                    {
                        writer.Flush();
                    }
                }
                writer.Close();
            }

            Console.WriteLine($"INFO: Saved measurements in {filePath}.");
        }

        public async void Share()
        {
            string folder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string filePath = $"{folder}/Sensus Shared Data.csv";

            if (DataList.Count == 0)
            {
                Console.WriteLine("Error: No measurements to save.");
                return;
            }

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            using (var writer = new StreamWriter(filePath))
            {
                writer.WriteLine($"Index; Z1; Z2");
                
                AccelerationData accelerationData;
                for (int i = 0; i < DataList.Count; i++)
                {
                    accelerationData = DataList[i];
                    writer.WriteLine($"{i}; {accelerationData.Z1}; {accelerationData.Z2}");
                    if (i % 1000 == 0)
                    {
                        writer.Flush();
                    }
                }
                writer.Close();
            }

            await Xamarin.Essentials.Share.RequestAsync(new ShareFileRequest
            {
                Title = "Share Sensus data in CSV format.",
                File = new ShareFile(filePath)
            });

            File.Delete(filePath);

            Console.WriteLine($"INFO: Shared Sensus data.");
        }

        private void OnReadingReceived(object sender, EventArgs args)
        {
            var receivedData = (DataReceivedEventArgs)args;
            byte[] rawData = receivedData.Data;

            if (receivedData.Index % 1000 == 0)
            {
                Console.WriteLine($"DEBUG: New reading. Lenght: {rawData.Length} – Index: {receivedData.Index} – Raw: {BitConverter.ToString(rawData)}");
            }

            DecodeRawData(rawData, receivedData.Index);
        }

        private void DecodeRawData(byte[] rawData, Int64 orderingIndex)
        {
            int lenght = rawData.Length;
            string dataString = Encoding.ASCII.GetString(rawData, 0, lenght);

            if (dataString == START_SIGNAL)
            {
                Console.WriteLine($"INFO: Received START SIGNAL.");
            }
            else if (dataString == STOP_SIGNAL)
            {
                Console.WriteLine($"INFO: Received STOP SIGNAL.");
                BLEService.CloseConnection();
            }
            else
            {
                // Each measurement is a 32-bit float, 2 signal -> 64 bits per reading -> 8 bytes per reading
                if (lenght % 8 == 0)
                {
                    for (int i = 0; i < lenght; i += 8)
                    {
                        DataList.Add(GetMeasurement(rawData, i));

                        if (LastIndex+1 != orderingIndex)
                        {
                            Console.WriteLine($"WARNING: Wrong BLE packet ordering.");
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"ERROR: Received invalid data. Did not receive a bytes array with length multiple of 8 bytes.");
                }
            }

            LastIndex = orderingIndex;
        }

        private AccelerationData GetMeasurement(byte[] byteArray, int startIdx)
        {
            float z1 = BitConverter.ToSingle(byteArray, startIdx);
            float z2 = BitConverter.ToSingle(byteArray, startIdx + 4);

            // ESP-32 is a Litle Endian CPU
            if (!BitConverter.IsLittleEndian)
            {
                byte[] z1Bytes = BitConverter.GetBytes(z1);
                byte[] z2Bytes = BitConverter.GetBytes(z2);

                Array.Reverse(z1Bytes, 0, z1Bytes.Length);
                Array.Reverse(z2Bytes, 0, z2Bytes.Length);

                z1 = BitConverter.ToSingle(z1Bytes, 0);
                z2 = BitConverter.ToSingle(z1Bytes, 0);
            }

            return new AccelerationData(z1, z2);
        }

        public void Clear()
        {
            DataList.Clear();
        }

        public int Count()
        {
            return DataList.Count;
        }

        public void Stop()
        {
            BLEService.SendStopCommand();
        }

        public void Start()
        {
            BLEService.StartConnection();
        }

    }
}
