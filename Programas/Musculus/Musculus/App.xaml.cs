using Xamarin.Forms;
using Musculus.Infrastructure;
using Musculus.Views;

namespace Musculus
{
    public partial class App : Application
    {
        public DeviceManager Sensus = new DeviceManager();

        public App()
        {
            InitializeComponent();

            ModalPopped += AppModalPopped;

            MainPage = new MainPage();
        }

        private void AppModalPopped(object sender, ModalPoppedEventArgs e)
        {
            // WORKAROUND: on Android, OnDisappering() is not called when modal page is popped.
            if (e.Modal.Title == "Exercise")
            {
                ((ExerciseView)e.Modal).ModalPoppedCallback();
            }
        }

        protected override void OnStart()
        {
            Sensus.StartConnection();
        }

        protected override void OnSleep()
        {
            Sensus.CloseConnection();
        }

        protected override void OnResume()
        {
            Sensus.StartConnection();
        }
    }
}
