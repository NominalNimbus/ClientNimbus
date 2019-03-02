using System.Windows;
using TradingClient.Interfaces;

namespace TradingClient.UIManager
{
    public static class EnumHelper
    {
        public static MessageBoxButton GetButtons4Message(MsgBoxButton buttons)
        {
            switch (buttons)
            {
                case MsgBoxButton.OK:
                    return MessageBoxButton.OK;
                case MsgBoxButton.OKCancel:
                    return MessageBoxButton.OKCancel;
                case MsgBoxButton.YesNoCancel:
                    return MessageBoxButton.YesNoCancel;
                case MsgBoxButton.YesNo:
                    return MessageBoxButton.YesNo;
                default: return MessageBoxButton.OK;
            }
        }

        public static MessageBoxImage GetIcon4Message(MsgBoxIcon icon)
        {
            switch (icon)
            {
                case MsgBoxIcon.Error:
                    return MessageBoxImage.Error;
                case MsgBoxIcon.Question:
                    return MessageBoxImage.Question;
                case MsgBoxIcon.Warning:
                    return MessageBoxImage.Warning;
                case MsgBoxIcon.Information:
                    return MessageBoxImage.Information;
                default:
                    return MessageBoxImage.None;
            }
        }

        public static DlgResult GetDialogResult(MessageBoxResult result)
        {
            switch (result)
            {
                case MessageBoxResult.OK:
                    return DlgResult.OK;
                case MessageBoxResult.Cancel:
                    return DlgResult.Cancel;
                case MessageBoxResult.Yes:
                    return DlgResult.Yes;
                case MessageBoxResult.No:
                    return DlgResult.No;
                default:
                    return DlgResult.None;
            }
        }
    }
}
