using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using Musculus.Infrastructure;
using Musculus.Models;
using Xamarin.Forms;
using Xamarin.Essentials;

namespace Musculus.ViewModels
{
    public class ExerciseViewModel : INotifyPropertyChanged
    {
        // Private properties
        private bool Suspended = false;
        private readonly double _forceSensivity = -0.2681; // From load cell calibration [mV/kg]
        private readonly double _initialForceOffset = 544.7; // From load cell calibration [kg]

        private int _exerciseIndex;
        public int ExerciseIndex
        {
            get => _exerciseIndex;
            set
            {
                _exerciseIndex = value;
                RemainingExercises = ExerciseList.Count - value;
            }
        }

        // Public properties
        public List<AccelerationData> DataList = new List<AccelerationData>();
        public DataBuffer DataBufferZ1 = new DataBuffer(1000);
        public DataBuffer DataBufferZ2 = new DataBuffer(1000);
        public EventHandler Finished;

        // Property changed event handling
        public event PropertyChangedEventHandler PropertyChanged;
        private void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Bindable properties
        private Exercise _currentExercise;
        public Exercise CurrentExercise
        {
            get => _currentExercise;
            set
            {
                if (_currentExercise != value)
                {
                    _currentExercise = value;
                    RaisePropertyChanged(nameof(CurrentExercise));
                }
            }
        }

        private int _currentCountDownInterval;
        public int CurrentCountDownInterval
        {
            get => _currentCountDownInterval;
            set
            {
                if (_currentCountDownInterval != value)
                {
                    _currentCountDownInterval = value;
                    RaisePropertyChanged(nameof(CurrentCountDownInterval));
                }
            }
        }

        private int _remainingExercises;
        public int RemainingExercises
        {
            get => _remainingExercises;
            set
            {
                if (_remainingExercises != value)
                {
                    _remainingExercises = value;
                    RaisePropertyChanged(nameof(_remainingExercises));
                }
            }
        }

        private string _currentExercisePhase;
        public string CurrentExercisePhase
        {
            get => _currentExercisePhase;
            set
            {
                if (_currentExercisePhase != value)
                {
                    _currentExercisePhase = value;
                    RaisePropertyChanged(nameof(CurrentExercisePhase));
                }
            }
        }

        private int _elapsedSeconds;
        public int ElapsedSeconds
        {
            get => _elapsedSeconds;
            set
            {
                if (_elapsedSeconds != value)
                {
                    _elapsedSeconds = value;
                    RaisePropertyChanged(nameof(ElapsedSeconds));
                }
            }
        }

        private string _deviceStatus;
        public string DeviceStatus
        {
            get => _deviceStatus;
            set
            {
                if (_deviceStatus != value)
                {
                    _deviceStatus = value;
                    RaisePropertyChanged(nameof(DeviceStatus));
                }
            }
        }

        private ObservableCollection<Exercise> _exerciseList = new ObservableCollection<Exercise>();
        public ObservableCollection<Exercise> ExerciseList
        {
            get { return _exerciseList; }
            set { _exerciseList = value; }
        }

        // MaxVoltage is in mV
        private int _maxVoltage;
        public int MaxVoltage
        {
            get => _maxVoltage;
            set
            {
                if (_maxVoltage != value)
                {
                    _maxVoltage = value;
                    RaisePropertyChanged(nameof(MaxVoltage));
                }
            }
        }

        // MaxForce is in kg
        private int _maxForce;
        public int MaxForce
        {
            get => _maxForce;
            set
            {
                if (_maxForce != value)
                {
                    if (value > 0.0)
                    {
                        _maxForce = value;
                        RaisePropertyChanged(nameof(MaxForce));
                    }
                }
            }
        }

        // TargetForce is in kg
        private double _targetForce;
        public double TargetForce
        {
            get => _targetForce;
            set
            {
                if (_targetForce != value)
                {
                    if (value > 0.0)
                    {
                        _targetForce = value;
                        RaisePropertyChanged(nameof(TargetForce));
                    }
                }
            }
        }

        // TargetForceBounds is in %
        private double _targetForceBounds;
        public double TargetForceBounds
        {
            get => _targetForceBounds;
            set
            {
                if (_targetForceBounds != value)
                {
                    if (value > 0.0 && value <= 100.0)
                    {
                        _targetForceBounds = value;
                        RaisePropertyChanged(nameof(TargetForceBounds));
                    }
                }
            }
        }

        // ForceOffset is in kg
        private double _forceOffset;
        public double ForceOffset
        {
            get => _forceOffset;
            set
            {
                if (_forceOffset != value)
                {
                    _forceOffset = value;
                    RaisePropertyChanged(nameof(ForceOffset));
                }
            }
        }

        // WindowLengthZ1 is in miliseconds
        private int _windowLengthZ1;
        public int WindowLengthZ1
        {
            get => _windowLengthZ1;
            set
            {
                if (_windowLengthZ1 != value)
                {
                    _windowLengthZ1 = value;
                    DataBufferZ1.Length = (int)(value / 1000.0 * SamplingFrequency);
                    DataBufferZ1.Reset();
                    RaisePropertyChanged(nameof(WindowLengthZ1));
                }
            }
        }

        // WindowLengthZ2 is in miliseconds
        private double _windowLengthZ2;
        public double WindowLengthZ2
        {
            get => _windowLengthZ2;
            set
            {
                if (_windowLengthZ2 != value)
                {
                    _windowLengthZ2 = value;
                    DataBufferZ2.Length = (int)(value / 1000.0 * SamplingFrequency);
                    DataBufferZ2.Reset();
                    RaisePropertyChanged(nameof(WindowLengthZ2));
                }
            }
        }

        // SamplingFrequency is in Hz
        private double _samplingFrequency;
        public double SamplingFrequency
        {
            get => _samplingFrequency;
            set
            {
                if (_samplingFrequency != value)
                {
                    if (value > 0)
                    {
                        _samplingFrequency = value;
                        if ((int)(value * WindowLengthZ1 / 1000.0) >= 1)
                        {
                            DataBufferZ1.Length = (int)(value * WindowLengthZ1 / 1000.0);
                        }
                        else
                        {
                            DataBufferZ1.Length = 1;
                        }
                        DataBufferZ1.Reset();

                        if ((int)(value * WindowLengthZ2 / 1000.0) >= 1)
                        {
                            DataBufferZ2.Length = (int)(value * WindowLengthZ2 / 1000.0);
                        }
                        else
                        {
                            DataBufferZ2.Length = 1;
                        }
                        DataBufferZ2.Reset();
                        RaisePropertyChanged(nameof(SamplingFrequency));
                    }
                }
            }
        }

        // Class initialization
        public ExerciseViewModel(ObservableCollection<Exercise> exerciseList)
        {
            App currentAppInstance = (App)Application.Current;
            DeviceStatus = currentAppInstance.Sensus.DeviceStatus.ToString();
            SamplingFrequency = currentAppInstance.Sensus.SamplingFrequency;

            MaxVoltage = 200;
            MaxForce = 100;
            WindowLengthZ1 = 1000;
            WindowLengthZ2 = 400;
            ForceOffset = 0;
            TargetForceBounds = 10;
            TargetForce = 30;

            ExerciseList = exerciseList;
            CurrentExercise = ExerciseList[ExerciseIndex];
            CurrentExercisePhase = "Set up";
        }

        // Private methods
        private void ElapsedSecondsTimer()
        {
            Device.StartTimer(new TimeSpan(0, 0, 1), () =>
            {
                if (!Suspended && (DeviceStatus == "Acquiring"))
                {
                    ElapsedSeconds++;
                }

                return true;
            });
        }

        private bool ExerciseTimer()
        {
            if (!Suspended && (DeviceStatus == "Acquiring"))
            {
                ElapsedSeconds = 0;

                if (CurrentExercisePhase == "Set up")
                {
                    CurrentExercisePhase = "Static";
                    CurrentCountDownInterval = (int)CurrentExercise.InitialStaticTime;
                }
                else if (CurrentExercisePhase == "Static")
                {
                    CurrentExercisePhase = "Transition to exercise";
                    CurrentCountDownInterval = (int)CurrentExercise.TransitionTimeToExercise;
                }
                else if (CurrentExercisePhase == "Transition to exercise")
                {
                    CurrentExercisePhase = "Exercise";
                    CurrentCountDownInterval = (int)CurrentExercise.ExerciseDuration;
                }
                else if (CurrentExercisePhase == "Exercise")
                {
                    CurrentExercisePhase = "Transition to rest";
                    CurrentCountDownInterval = (int)CurrentExercise.TransitionTimeToRest;
                }
                else if (CurrentExercisePhase == "Transition to rest")
                {
                    CurrentExercisePhase = "Rest";
                    CurrentCountDownInterval = (int)CurrentExercise.RestTime;
                }
                else if (CurrentExercisePhase == "Rest")
                {
                    ExerciseIndex++;
                    if (ExerciseIndex < ExerciseList.Count)
                    {
                        CurrentExercise = ExerciseList[ExerciseIndex];
                        CurrentExercisePhase = "Set up";
                        CurrentCountDownInterval = (int)CurrentExercise.SetUpTime;
                    }
                    else
                    {
                        CurrentExercisePhase = "Finished";
                        CurrentCountDownInterval = 0;

                        Console.WriteLine("INFO: Finished exercise list.");

                        Share();

                        Finished?.Invoke(this, null);
                        return false;
                    }
                }
               
                Device.StartTimer(new TimeSpan(0, 0, CurrentCountDownInterval), ExerciseTimer);
                return false;
            }

            // Clean corrupted exercise from DataList
            if (ExerciseIndex == 0)
            {
                DataList.Clear();
            }
            else
            {
                if (DataList.Count > 0)
                {
                    if (DataList[DataList.Count - 1].ExercisePhase == "Rest")
                    {
                        while (DataList[DataList.Count - 1].ExercisePhase == "Rest")
                        {
                            DataList.RemoveAt(DataList.Count - 1);
                            if (DataList.Count == 0) break;
                        }
                    }
                    if (DataList.Count > 0)
                    {
                        while (DataList[DataList.Count - 1].ExercisePhase != "Rest")
                        {
                            DataList.RemoveAt(DataList.Count - 1);
                            if (DataList.Count == 0) break;
                        }
                    }
                    ExerciseIndex--;
                }
            }

            // Reset last exercise
            CurrentExercise = ExerciseList[ExerciseIndex];
            CurrentExercisePhase = "Set up";
            CurrentCountDownInterval = (int)CurrentExercise.SetUpTime;
            ElapsedSeconds = 0;

            return false;
        }

        private void OnStatusChanged(object sender, EventArgs args)
        {
            var newDeviceStatus = ((PeriphericalStatusChangedEventArgs)args).DeviceStatus;
            DeviceStatus = newDeviceStatus.ToString();
        }

        private void OnSamplingFrequencyChanged(object sender, EventArgs args)
        {
            var newSamplingFrequency = ((DeviceManager.SamplingFrequencyChangedEventArgs)args).SamplingFrequency;
            SamplingFrequency = newSamplingFrequency;
        }

        private void OnReadingReceived(object sender, EventArgs args)
        {
            var receivedData = (DataReceivedEventArgs) args;
            byte[] rawData = receivedData.Data;
            int lenght = rawData.Length;

            // ESP-32 is a Litle Endian CPU, we must reverse bytes if client is not little-endian
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(rawData, 0, lenght);
            }

            for (int i = 0; i < lenght; i += 8)
            {
                AccelerationData data = new AccelerationData()
                {
                    Z1 = BitConverter.ToSingle(rawData, i),
                    Z2 = BitConverter.ToSingle(rawData, i + 4),
                    ExerciseName = CurrentExercise.ExerciseName,
                    Weight = CurrentExercise.Weight,
                    ExercisePhase = CurrentExercisePhase,
                    PhaseDuration = CurrentCountDownInterval
                };

                DataList.Add(data);

                DataBufferZ1.Add(data.Z1, dt: 1.0 / SamplingFrequency,
                    high_pass_filter: true);

                
                DataBufferZ2.Add(data.Z2, dt: 1.0 / SamplingFrequency,
                    rescale: _forceSensivity,
                    offset: _initialForceOffset+ForceOffset);
            }
        }

        public async void Share()
        {
            string folder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string filePath = $"{folder}/Sensus Shared Data.csv";

            if (DataList.Count == 0)
            {
                Console.WriteLine("ERROR: No measurements to save.");
                return;
            }

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            using (var writer = new StreamWriter(filePath))
            {
                writer.WriteLine($"Exercise acceleration data");
                writer.WriteLine($"Captured with Sensus v2");
                writer.WriteLine($"Sampling frequency [Hz]; {SamplingFrequency}");
                writer.WriteLine($"Acquisition time [s]; {DataList.Count / SamplingFrequency}");
                writer.WriteLine($"Number of data points; {DataList.Count}");
                writer.WriteLine($"Date; {DateTime.Now:dd\\/MM/\\yyyy HH\\:mm\\:ss zzz}");
                writer.WriteLine($"Index; Z1 [mV]; Z2 [mV]; Exercise Name; Exercise Phase; Phase Duration [s]; Weight [kg]");

                AccelerationData accelerationData;
                for (int i = 0; i < DataList.Count; i++)
                {
                    accelerationData = DataList[i];

                    writer.WriteLine($"{i};" +
                        $" {accelerationData.Z1};" +
                        $" {accelerationData.Z2};" +
                        $" {accelerationData.ExerciseName};" +
                        $" {accelerationData.ExercisePhase};" +
                        $" {accelerationData.PhaseDuration};" +
                        $" {accelerationData.Weight}");

                    if (i % 1000 == 0)
                    {
                        writer.Flush();
                    }
                }
                writer.Flush();
                writer.Close();
            }

            try
            {
                await Xamarin.Essentials.Share.RequestAsync(new ShareFileRequest
                {
                    Title = "Share Sensus data in CSV format.",
                    File = new ShareFile(filePath)
                });
            }
            catch
            {
                Console.WriteLine($"ERROR: Error while sharing data using Android sharing API.");
                File.Delete(filePath);
                return;
            }

            File.Delete(filePath);

            Console.WriteLine($"INFO: Shared Sensus data.");
        }

        public void Suspend()
        {
            Suspended = true;

            App currentAppInstance = (App)Application.Current;
            currentAppInstance.Sensus.DataReceived -= OnReadingReceived;
            currentAppInstance.Sensus.StatusChanged -= OnStatusChanged;
            currentAppInstance.Sensus.SamplingFrequencyChanged -= OnSamplingFrequencyChanged;
            Console.WriteLine("DEBUG: (ExerciseViewModel.cs) Unsuscribed from DataReceived and StatusChanged events.");

            currentAppInstance.Sensus.StopAcquiring();
            Console.WriteLine("DEBUG: (ExerciseViewModel.cs) ExerciseViewModel suspend. Stopped acquisition.");
        }

        public void Resume()
        {
            Suspended = false;

            App currentAppInstance = (App)Application.Current;

            currentAppInstance.Sensus.DataReceived += OnReadingReceived;
            currentAppInstance.Sensus.StatusChanged += OnStatusChanged;
            currentAppInstance.Sensus.SamplingFrequencyChanged += OnSamplingFrequencyChanged;
            Console.WriteLine("DEBUG: (ExerciseViewModel.cs) Suscribed from DataReceived and StatusChanged events.");

            SamplingFrequency = currentAppInstance.Sensus.SamplingFrequency;

            currentAppInstance.Sensus.StartAcquiring();
            Console.WriteLine("DEBUG: (ExerciseViewModel.cs) ExerciseViewModel resumed. Started acquisition.");

            ExerciseTimer();
            ElapsedSecondsTimer();
        }
    }
}
