using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradingClient.ViewModelInterfaces;

namespace TradingClient.ViewModels
{
    public class CloseDocumentMessage : MessageBase
    {
        public IDocumentViewModel Document { get; private set; }

        public CloseDocumentMessage(IDocumentViewModel model)
        {
            Document = model;
        }
    }
}
