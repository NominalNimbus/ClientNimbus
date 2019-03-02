using System;

namespace TradingClient.UIManager
{
    internal sealed class UIBinding
    {
        #region Constructor

        public UIBinding(Type viewModel, Type view)
        {
            ViewModel = viewModel;
            View = view;
        }

        #endregion // Constructor

        #region Properties

        public Type ViewModel { get; }
        public Type View { get; }

        #endregion // Properties

    }
}
