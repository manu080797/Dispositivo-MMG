using System;
using System.Collections.ObjectModel;
using Musculus.Models;
using Musculus.ViewModels;
using Xamarin.Forms;

namespace Musculus.Views
{
    public partial class NewExerciseView : ContentPage
    {
        public NewExerciseView(ObservableCollection<Exercise> exerciseList)
        {
            InitializeComponent();
            BindingContext = new NewExerciseViewModel(exerciseList);
        }

        void AddExerciseButtonClicked(object sender, EventArgs e)
        {
            Navigation.PopModalAsync();
        }
    }
}
