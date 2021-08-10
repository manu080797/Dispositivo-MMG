using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Linq;
using Musculus.Models;
using Musculus.ViewModels;
using SkiaSharp;
using SkiaSharp.Views.Forms;
using Xamarin.Forms;

namespace Musculus.Views
{
    public partial class ExerciseView : ContentPage
    {
        // properties
        private SKPaint StrokePaintMMG = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            Color = SKColors.Magenta,
            StrokeWidth = 5
        };

        private SKPaint StrokePaintForce = new SKPaint
        {
            Style = SKPaintStyle.StrokeAndFill,
            Color = SKColors.Yellow,
            StrokeWidth = 10
        };
        private SKPaint TextForce = new SKPaint
        {
            Color = SKColors.White,
            
            TextSize = 75,
            TextAlign = SKTextAlign.Center
        };
        private SKPaint TextBackgroundForce = new SKPaint
        {
            Color = SKColors.Black,
            Style = SKPaintStyle.StrokeAndFill,
            StrokeWidth = 1
        };
        private SKPaint StrokePaintTargetForceBounds = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            Color = SKColors.Green,
            StrokeWidth = 5
        };
        private SKPaint StrokePaintTargetForce = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            Color = SKColors.Red,
            StrokeWidth = 5
        };

        // constructor
        public ExerciseView(ObservableCollection<Exercise> exerciseList)
        {
            InitializeComponent();
            BindingContext = new ExerciseViewModel(exerciseList);

            ((ExerciseViewModel)BindingContext).Finished += PopModal;

            MaxForceSlider.Minimum = 1;
            MaxVoltageSlider.Minimum = 1;
            WindowLengthSlider.Minimum = 1;
            AveragingTimeSlider.Minimum = 1;

            Device.StartTimer(TimeSpan.FromMilliseconds(33), UpdatePlot);
        }

        // methods
        private void PopModal(object sender, EventArgs e)
        {
            Navigation.PopModalAsync();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            ((ExerciseViewModel)BindingContext).Resume();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            ((ExerciseViewModel)BindingContext).Suspend();
        }

        public void ModalPoppedCallback()
        {
            ((ExerciseViewModel)BindingContext).Suspend();
        }

        private bool UpdatePlot()
        {
            if (IsVisible)
            {
                ChartZ1.InvalidateSurface();
                ChartZ2.InvalidateSurface();
            }
            return true;
        }

        public void RenderPlotMMG(object sender, SKPaintSurfaceEventArgs args)
        {
            SKImageInfo info = args.Info;
            SKSurface surface = args.Surface;
            SKCanvas canvas = surface.Canvas;

            canvas.Clear(new SKColor(0, 0, 0));
            float height = info.Height;
            float width = info.Width;

            float scale = (height / 2.0f) / (((ExerciseViewModel)BindingContext).MaxVoltage);
            float pixelOffsetZ1 = height / 2.0f;
            float x, y, y_mean;

            var dataBufferZ1 = ((ExerciseViewModel)BindingContext).DataBufferZ1;

            SKPath pathZ1 = new SKPath();
            float length = dataBufferZ1.Length;
            if ((dataBufferZ1.Data.Count < dataBufferZ1.Length))
            {
                if (dataBufferZ1.Data.Count == 0)
                {
                    return;
                }
                length = Math.Max(dataBufferZ1.Data.Count - 1, 1);
            }
            int step = Math.Max((int)(length / width), 1);

            y_mean = 0.0f;
            for (int i = 0; i < length; i++)
            {
                y_mean += (float)dataBufferZ1.Data[i];
            }
            y_mean /= length;

            for (int i = 0; i < length; i += step)
            {
                x = i / length * width;

                try
                {
                    y = (float)dataBufferZ1.Data[i] - y_mean;
                }
                catch
                {
                    y = 0.0f;
                }

                if (y > 0)
                {
                    y = height - Math.Min(y * scale, height / 2) - pixelOffsetZ1;
                }
                else
                {
                    y = height - Math.Max(y * scale, -height / 2) - pixelOffsetZ1;
                }

                if (i == 0)
                {
                    pathZ1.MoveTo(x, y);
                }
                else
                {
                    pathZ1.LineTo(x, y);
                }
            }
            canvas.DrawPath(pathZ1, StrokePaintMMG);
        }

        public void RenderPlotForce(object sender, SKPaintSurfaceEventArgs args)
        {
            SKImageInfo info = args.Info;
            SKSurface surface = args.Surface;
            SKCanvas canvas = surface.Canvas;

            canvas.Clear(new SKColor(0, 0, 0));
            float height = info.Height;
            float width = info.Width;

            float maxForce = ((ExerciseViewModel)BindingContext).MaxForce;
            float targetForce = (float)((ExerciseViewModel)BindingContext).TargetForce;
            float targetForceBounds = (float)((ExerciseViewModel)BindingContext).TargetForceBounds;
            float upperForceBound = targetForce * (1.0f + targetForceBounds / 100.0f);
            float lowerForceBound = targetForce * (1.0f - targetForceBounds / 100.0f);
            var forceData = ((ExerciseViewModel)BindingContext).DataBufferZ2.Data;

            float force = 0.0f;
            if (forceData.Count > 0)
            {
                try
                {
                    lock (((ICollection)forceData).SyncRoot)
                    {
                        force = (float)forceData.Average();
                    }
                }
                catch
                {
                    //Console.WriteLine("WARNING: Unable to calculate average force. Posible multiple access to forceData.");
                }
            }

            SKRect rect = new SKRect(0, 0, force / maxForce * width, height);
            canvas.DrawRect(rect, StrokePaintForce);

            SKRect background = new SKRect(width / 2 - 150, height / 2 - 75, width / 2 + 150, height / 2 + 25);
            canvas.DrawRect(background, TextBackgroundForce);

            canvas.DrawLine(targetForce / maxForce * width, 0.0f, targetForce / maxForce * width, height, StrokePaintTargetForce);
            canvas.DrawLine(upperForceBound / maxForce * width, 0.0f, upperForceBound / maxForce * width, height, StrokePaintTargetForceBounds);
            canvas.DrawLine(lowerForceBound / maxForce * width, 0.0f, lowerForceBound / maxForce * width, height, StrokePaintTargetForceBounds);

            canvas.DrawText($"{Math.Round(force, 1)} kg", width / 2, height / 2, TextForce);
        }
    }
}
