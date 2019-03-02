using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;

namespace TradingClient.Views
{
    /// <summary>
    /// Interaction logic for VolumeBar.xaml
    /// </summary>
    public partial class VolumeBar
    {
        public Brush FillBrush
        {
            get { return (Brush)GetValue(FillBrushProperty); }
            set { SetValue(FillBrushProperty, value); }
        }

        public decimal Value
        {
            get { return (decimal)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public int Scale
        {
            get { return (int)GetValue(ScaleProperty); }
            set
            { SetValue(ScaleProperty, value); }
        }

        public HorizontalAlignment Alignment
        {
             get { return (HorizontalAlignment)GetValue(AlignmentProperty); }
             set { SetValue(AlignmentProperty, value); }
        }
        

        public VolumeBar()
        {
            var scale = DependencyPropertyDescriptor.FromProperty(ScaleProperty, typeof(VolumeBar));
            scale.AddValueChanged(this, ScaleChanged);

            var fill = DependencyPropertyDescriptor.FromProperty(FillBrushProperty, typeof(VolumeBar));
            fill.AddValueChanged(this, FillChanged);

            var alignment = DependencyPropertyDescriptor.FromProperty(AlignmentProperty, typeof(VolumeBar));
            alignment.AddValueChanged(this, AlignmentChanged);

            InitializeComponent();
        }

        private void AlignmentChanged(object sender, EventArgs e)
        {
            Bar.HorizontalAlignment = Alignment;
        }

        private void FillChanged(object sender, EventArgs eventArgs)
        {
            Bar.Background = FillBrush;
        }

        private void ScaleChanged(object sender, EventArgs eventArgs)
        {
            if (ConteinerControl.ActualWidth < 10)
            {
                Bar.Width = 0;
            }
            else
            {
                Bar.Width = (ConteinerControl.ActualWidth-6)/100*Scale;
            }

            Bar.ToolTip = String.Format("{0} - {1}/100", Value.ToString("0"), Scale);
        }

        public static readonly DependencyProperty FillBrushProperty =
             DependencyProperty.Register("FillBrush", typeof(Brush),
             typeof(VolumeBar));

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(decimal),
            typeof(VolumeBar));

        public static readonly DependencyProperty ScaleProperty =
           DependencyProperty.Register("Scale", typeof(int),
           typeof(VolumeBar));

        public static readonly DependencyProperty AlignmentProperty =
             DependencyProperty.Register("Alignment", typeof(HorizontalAlignment),
             typeof(VolumeBar));

        private void VolumeBar_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (ConteinerControl.ActualWidth < 10)
            {
                Bar.Width = 0;
            }
            else
            {
                Bar.Width = (ConteinerControl.ActualWidth - 6) / 100 * Scale;
            }
        }
    }
}
