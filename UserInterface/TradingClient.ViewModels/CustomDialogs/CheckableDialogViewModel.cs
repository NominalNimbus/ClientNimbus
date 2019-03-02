using System;
using System.Drawing;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using GalaSoft.MvvmLight.Command;

namespace TradingClient.ViewModels
{
    public class CheckableDialogViewModel : ViewModelBase
    {
        private bool _isChecked;
        private string _title;
        private string _message;

        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                if (_isChecked != value)
                {
                    _isChecked = value;
                    OnPropertyChanged("IsChecked");
                }
            }
        }

        public string Title
        {
            get => _title ?? String.Empty;
            set
            {
                if (_title != value)
                {
                    _title = value;
                    OnPropertyChanged("Title");
                }
            }
        }

        public string Message
        {
            get => _message ?? String.Empty;
            set
            {
                if (_message != value)
                {
                    _message = value;
                    OnPropertyChanged("Message");
                }
            }
        }

        public BitmapImage Icon => SystemIconToImageSource(SystemIcons.Question.ToBitmap());
        
        public ICommand ConfirmCommand { get; private set; }

        public ICommand CancelCommand { get; private set; }

        public CheckableDialogViewModel(string message, string title, bool isChecked = false)
        {
            Title = title;
            Message = message;
            IsChecked = isChecked;

            ConfirmCommand = new RelayCommand(() => DialogResult = true);
            CancelCommand = new RelayCommand(() => DialogResult = false);
        }

        private static BitmapImage SystemIconToImageSource(Bitmap bitmap)
        {
            using (var stream = new System.IO.MemoryStream())
            {
                bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                stream.Position = 0;
                var image = new BitmapImage();
                image.BeginInit();
                image.StreamSource = stream;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.EndInit();
                return image;
            }
        }
    }
}