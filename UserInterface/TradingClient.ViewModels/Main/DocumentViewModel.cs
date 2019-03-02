using System;
using System.Windows.Input;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using TradingClient.Interfaces;
using TradingClient.ViewModelInterfaces;

namespace TradingClient.ViewModels
{
    public class DocumentViewModel : ViewModelBase, IDocumentViewModel
    {
        #region Memebers

        private bool _isActive;
        private bool _isSelected;
        private bool _isVisible;

        #endregion //Memebers

        public DocumentViewModel()
        {
            Id = Guid.NewGuid().ToString("N");
            CloseCommand = new RelayCommand(()=> Messenger.Default.Send(new CloseDocumentMessage(this)));
        }

        #region Properties

        public string Id { get; set; }

        public virtual string Title { get; }

        public virtual DocumentType DocumentType { get; }

        public bool IsVisible
        {
            get => _isVisible;
            set => SetPropertyValue(ref _isVisible, value, nameof(IsVisible));
        }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetPropertyValue(ref _isSelected, value, nameof(IsSelected));
        }

        public bool IsActive
        {
            get => _isActive;
            set => SetPropertyValue(ref _isActive, value, nameof(IsActive));
        }

        public ICommand CloseCommand { get; }

        #endregion //Properties

        #region Methods
        
        public virtual byte[] SaveWorkspaceData() => 
            new byte[0];
        
        public virtual void LoadWorkspaceData(byte[] data)
        {

        }

        #endregion //Methods
    }
}