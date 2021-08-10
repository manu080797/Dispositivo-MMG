using System;
using System.Collections.ObjectModel;
using Musculus.Models;
using Musculus.ViewModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Musculus.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class HomeView : ContentPage
    {
        public HomeView()
        {
            InitializeComponent();
        }

        private void StartButtonClicked(object sender, EventArgs e)
        {
            ObservableCollection<Exercise> copyExerciseList = new ObservableCollection<Exercise>();

            ((HomeViewModel)BindingContext).BackupExerciseList();

            var exerciseList = ((HomeViewModel)BindingContext).ExerciseList;

            for (int i = 0; i < exerciseList.Count; i++)
            {
                copyExerciseList.Add(exerciseList[i]);
            }

            Navigation.PushModalAsync(new ExerciseView(copyExerciseList));
        }

        private void AddExerciseButtonClicked(object sender, EventArgs e)
        {
            var exerciseList = ((HomeViewModel)BindingContext).ExerciseList;
            Navigation.PushModalAsync(new NewExerciseView(exerciseList));
        }
    }
}
