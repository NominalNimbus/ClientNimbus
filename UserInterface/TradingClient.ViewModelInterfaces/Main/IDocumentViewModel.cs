using System;
using System.Windows.Input;
using TradingClient.Interfaces;

namespace TradingClient.ViewModelInterfaces
{
    public interface IDocumentViewModel : IDisposable
    {
        #region Properties and Commands

        string Id { get; }

        string Title { get; }

        DocumentType DocumentType { get; }

        bool IsVisible { get; set; }
        
        bool IsSelected { get; set; }

        bool IsActive { get; set; }

        ICommand CloseCommand { get; }

        #endregion //Properties and Commands

        #region Methods

        byte[] SaveWorkspaceData();

        void LoadWorkspaceData(byte[] data);

        #endregion Methods
    }
}