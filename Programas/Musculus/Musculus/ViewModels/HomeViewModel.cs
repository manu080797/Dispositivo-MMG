using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Musculus.Models;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace Musculus.ViewModels
{
	public class HomeViewModel : INotifyPropertyChanged
	{
		// Property changed event handling
		public event PropertyChangedEventHandler PropertyChanged;
		private void RaisePropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		// Bindable properties
		private ObservableCollection<Exercise> _exerciseList = new ObservableCollection<Exercise>();
		public ObservableCollection<Exercise> ExerciseList
        {
			get
            {
				return _exerciseList;
            }
			set
			{
				if (_exerciseList != value)
				{
					_exerciseList = value;
					RaisePropertyChanged(nameof(ExerciseList));
				}
			}
		}

		// Commands
		public Command RemoveLastExerciseCommand { get; }
		private void RemoveLastExercise()
		{
			ExerciseList.RemoveAt(ExerciseList.Count - 1);
		}

		public Command ClearExerciseListCommand { get; }
		private void ClearExerciseList()
		{
			ExerciseList.Clear();
		}

		public Command LoadExerciseListFromFileCommand { get; }
		private async void LoadExerciseListFromFile()
		{
			List<Exercise> temporaryExerciseList = new List<Exercise>();

			try
			{
				var result = await FilePicker.PickAsync();
				if (result != null)
				{
					Console.WriteLine($"INFO: Loaded exercise list from {result.FileName}");
					if (result.FileName.EndsWith("csv", StringComparison.OrdinalIgnoreCase))
					{
						using (var reader = new StreamReader(await result.OpenReadAsync()))
						{
							string newLine;

							// Ignore header
							newLine = reader.ReadLine();

							while ((newLine = reader.ReadLine()) != null)
							{
								var fields = newLine.Split(';');

								Exercise exercise = new Exercise()
								{
									ExerciseName = fields[1],
									Weight = double.Parse(fields[2], CultureInfo.InvariantCulture),
									SetUpTime = double.Parse(fields[3], CultureInfo.InvariantCulture),
									InitialStaticTime = double.Parse(fields[4], CultureInfo.InvariantCulture),
									TransitionTimeToExercise = double.Parse(fields[5], CultureInfo.InvariantCulture),
									ExerciseDuration = double.Parse(fields[6], CultureInfo.InvariantCulture),
									TransitionTimeToRest = double.Parse(fields[7], CultureInfo.InvariantCulture),
									RestTime = double.Parse(fields[8], CultureInfo.InvariantCulture)
								};

								Console.WriteLine($"DEBUG: Read exercise {exercise.ExerciseName}, {exercise.Weight} kg, {exercise.ExerciseDuration} s");

								temporaryExerciseList.Add(exercise);
							}
						}
					}
				}
			}
			catch
			{
				return;
			}

			ExerciseList.Clear();
			foreach (var exercise in temporaryExerciseList)
			{
				ExerciseList.Add(exercise);
			}
		}

		public Command ShareExerciseListCommand { get; }
		private async void ShareExerciseListAsync()
		{
			BackupExerciseList();

			string folder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
			string filePath = $"{folder}/Exercise Data.csv";

			if (ExerciseList.Count == 0)
			{
				Console.WriteLine("Error: No exercises to save.");
				return;
			}

			if (File.Exists(filePath))
			{
				File.Delete(filePath);
			}

			using (var writer = new StreamWriter(filePath))
			{
				writer.WriteLine($"Index;" +
                    $" Name;" +
                    $" Weight [kg];" +
                    $" Setup time [s];" +
                    $" Initial static time [s];" +
                    $" Transition to exercise [s];" +
                    $" Exercise duration [s];" +
                    $" Transition to rest [s];" +
                    $" Rest [s]");

				Exercise exercise;
				for (int i = 0; i < ExerciseList.Count; i++)
				{
					exercise = ExerciseList[i];
					writer.WriteLine($"{i};"  +
                        $"{exercise.ExerciseName}; " +
						$"{exercise.Weight}; " +
						$"{exercise.SetUpTime}; " +
						$"{exercise.InitialStaticTime}; " +
						$"{exercise.TransitionTimeToExercise}; " +
						$"{exercise.ExerciseDuration}; " +
						$"{exercise.TransitionTimeToRest}; " +
						$"{exercise.RestTime}");
					if (i % 10 == 0)
					{
						writer.Flush();
					}
				}
				writer.Flush();
				writer.Close();
			}

			await Share.RequestAsync(new ShareFileRequest
			{
				Title = "Share Exercise list in CSV format.",
				File = new ShareFile(filePath)
			});

			File.Delete(filePath);
		}

        public void BackupExerciseList()
        {
			string folder = FileSystem.AppDataDirectory;
			string filePath = $"{folder}/internal__exercise_list.bak";

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            using (var writer = new StreamWriter(filePath))
            {
                writer.WriteLine($"Index;" +
                    $" Name;" +
                    $" Weight [kg];" +
                    $" Setup time [s];" +
                    $" Initial static time [s];" +
                    $" Transition to exercise [s];" +
                    $" Exercise duration [s];" +
                    $" Transition to rest [s];" +
                    $" Rest [s]");

                Exercise exercise;
                for (int i = 0; i < ExerciseList.Count; i++)
                {
                    exercise = ExerciseList[i];
                    writer.WriteLine($"{i};" +
                        $"{exercise.ExerciseName}; " +
                        $"{exercise.Weight}; " +
                        $"{exercise.SetUpTime}; " +
                        $"{exercise.InitialStaticTime}; " +
                        $"{exercise.TransitionTimeToExercise}; " +
                        $"{exercise.ExerciseDuration}; " +
                        $"{exercise.TransitionTimeToRest}; " +
                        $"{exercise.RestTime}");
                    if (i % 10 == 0)
                    {
                        writer.Flush();
                    }
                }
                writer.Flush();
                writer.Close();
            }

			Console.WriteLine($"INFO: Saved backup exercise list.");
		}

		public void LoadExerciseListBackup()
		{
			List<Exercise> temporaryExerciseList = new List<Exercise>();

			string folder = FileSystem.AppDataDirectory;
			string filePath = $"{folder}/internal__exercise_list.bak";

			if (File.Exists(filePath) != true)
			{
				Console.WriteLine("WARNING: No backup exercise list file found.");
				return;
			}

			using (var reader = new StreamReader(filePath))
			{
				string newLine;

				// Ignore header
				newLine = reader.ReadLine();

				while ((newLine = reader.ReadLine()) != null)
				{
					var fields = newLine.Split(';');

					Exercise exercise = new Exercise()
					{
						ExerciseName = fields[1],
						Weight = double.Parse(fields[2], CultureInfo.InvariantCulture),
						SetUpTime = double.Parse(fields[3], CultureInfo.InvariantCulture),
						InitialStaticTime = double.Parse(fields[4], CultureInfo.InvariantCulture),
						TransitionTimeToExercise = double.Parse(fields[5], CultureInfo.InvariantCulture),
						ExerciseDuration = double.Parse(fields[6], CultureInfo.InvariantCulture),
						TransitionTimeToRest = double.Parse(fields[7], CultureInfo.InvariantCulture),
						RestTime = double.Parse(fields[8], CultureInfo.InvariantCulture)
					};

					Console.WriteLine($"DEBUG: Read exercise {exercise.ExerciseName}, {exercise.Weight} kg, {exercise.ExerciseDuration} s");

					temporaryExerciseList.Add(exercise);
				}
			}

			ExerciseList.Clear();
            foreach (var exercise in temporaryExerciseList)
            {
				ExerciseList.Add(exercise);
			}

			Console.WriteLine($"INFO: Loaded exercise list from backup.");
		}

		// Initializate class
		public HomeViewModel()
		{
			// Commands
			LoadExerciseListFromFileCommand = new Command(LoadExerciseListFromFile);
			ShareExerciseListCommand = new Command(ShareExerciseListAsync);
			ClearExerciseListCommand = new Command(ClearExerciseList);
			RemoveLastExerciseCommand = new Command(RemoveLastExercise);

            ExerciseList.CollectionChanged += ExerciseListCollectionChanged;

			LoadExerciseListBackup();
		}

        private void ExerciseListCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
			Task.Run(BackupExerciseList);
		}
    }
}