using System.ComponentModel;

namespace Musculus.Models
{
    public class Exercise : INotifyPropertyChanged
    {
        // Property changed event handling
        public event PropertyChangedEventHandler PropertyChanged;
        private void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Bindable properties
        private double _setUpTime;
        public double SetUpTime
        {
            get => _setUpTime;
            set
            {
                if (_setUpTime != value)
                {
                    _setUpTime = value;
                    RaisePropertyChanged(nameof(SetUpTime));
                }
            }
        }

        private double _initialStaticTime;
        public double InitialStaticTime
        {
            get => _initialStaticTime;
            set
            {
                if (_initialStaticTime != value)
                {
                    _initialStaticTime = value;
                    RaisePropertyChanged(nameof(InitialStaticTime));
                }
            }
        }

        private double _transitionTimeToExercise;
        public double TransitionTimeToExercise
        {
            get => _transitionTimeToExercise;
            set
            {
                if (_transitionTimeToExercise != value)
                {
                    _transitionTimeToExercise = value;
                    RaisePropertyChanged(nameof(TransitionTimeToExercise));
                }
            }
        }

        private double _exerciseDuration;
        public double ExerciseDuration
        {
            get => _exerciseDuration;
            set
            {
                if (_exerciseDuration != value)
                {
                    _exerciseDuration = value;
                    RaisePropertyChanged(nameof(ExerciseDuration));
                }
            }
        }

        private double _transitionTimeToRest;
        public double TransitionTimeToRest
        {
            get => _transitionTimeToRest;
            set
            {
                if (_transitionTimeToRest != value)
                {
                    _transitionTimeToRest = value;
                    RaisePropertyChanged(nameof(TransitionTimeToRest));
                }
            }
        }

        private double _restTime;
        public double RestTime
        {
            get => _restTime;
            set
            {
                if (_restTime != value)
                {
                    _restTime = value;
                    RaisePropertyChanged(nameof(RestTime));
                }
            }
        }

        private double _weight;
        public double Weight
        {
            get => _weight;
            set
            {
                if (_weight != value)
                {
                    _weight = value;
                    RaisePropertyChanged(nameof(Weight));
                }
            }
        }

        private string _exerciseName;
        public string ExerciseName
        {
            get => _exerciseName;
            set
            {
                if (_exerciseName != value)
                {
                    _exerciseName = value;
                    RaisePropertyChanged(nameof(ExerciseName));
                }
            }
        }

        // Class initializators
        public Exercise()
        {
            SetUpTime = 0.0;
            InitialStaticTime = 0.0;
            TransitionTimeToExercise = 0.0;
            ExerciseDuration = 0.0;
            TransitionTimeToRest = 0.0;
            RestTime = 0.0;

            Weight = 0.0;
            ExerciseName = "";
        }
    
        public Exercise(string exerciseName,
                        double weight,
                        double setUpTime,
                        double initialStaticTime,
                        double transitionTimeToExercise,
                        double exerciseDuration,
                        double transitionTimeToRest,
                        double restTime)
        {
            SetUpTime = setUpTime;
            InitialStaticTime = initialStaticTime;
            TransitionTimeToExercise = transitionTimeToExercise;
            ExerciseDuration = exerciseDuration;
            TransitionTimeToRest = transitionTimeToRest;
            RestTime = restTime;
    
            Weight = weight;
            ExerciseName = exerciseName;
        }
    
        public Exercise(string exerciseName,
                        double weight,
                        double exerciseDuration,
                        double restTime)
        {
            ExerciseDuration = exerciseDuration;
            RestTime = restTime;
    
            SetUpTime = 0.0;
            InitialStaticTime = 0.0;
            TransitionTimeToExercise = 0.0;
            TransitionTimeToRest = 0.0;
    
            Weight = weight;
            ExerciseName = exerciseName;
        }

        public static Exercise Copy(Exercise src)
        {
            Exercise dest = new Exercise
            {
                ExerciseName = src.ExerciseName,
                Weight = src.Weight,
                SetUpTime = src.SetUpTime,
                InitialStaticTime = src.InitialStaticTime,
                TransitionTimeToExercise = src.TransitionTimeToExercise,
                ExerciseDuration = src.ExerciseDuration,
                TransitionTimeToRest = src.TransitionTimeToRest,
                RestTime = src.RestTime
            };
            return dest;
        }
    }
}
