using System.Collections.ObjectModel;
using System.ComponentModel;
using Musculus.Models;
using Xamarin.Forms;

namespace Musculus.ViewModels
{
    public class NewExerciseViewModel : INotifyPropertyChanged
	{
		// Private properties
		private ObservableCollection<Exercise> ExerciseList;

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

		// Property changed event handling
		public event PropertyChangedEventHandler PropertyChanged;
		private void RaisePropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		// Commands
		public Command AddExerciseCommand { get; }
		private void AddExercise()
		{
			ExerciseList.Add(Exercise.Copy(CurrentExercise));
		}

		// Initializate class 
		public NewExerciseViewModel(ObservableCollection<Exercise> exerciseList)
        {
			// Properties
			ExerciseList = exerciseList;
			CurrentExercise = new Exercise("Extensión isométrica de rodilla", 10, 10, 120);

			// Commands
			AddExerciseCommand = new Command(AddExercise);
		}
    }
}
