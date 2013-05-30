using System.Windows;

namespace MusicDatabase
{
    class Dialogs
    {
        public static bool YesNoQuestion(string message)
        {
            return MessageBox.Show(message, "Music Database", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
        }

        public static MessageBoxResult YesNoCancelQuestion(string message)
        {
            return MessageBox.Show(message, "Music Database", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
        }

        public static bool Confirm(string message)
        {
            return MessageBox.Show(message, "Music Database", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK;
        }

        public static MessageBoxResult YesNoCancel(string message)
        {
            return MessageBox.Show(message, "Music Database", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
        }

        public static bool Confirm()
        {
            return Confirm("Are you sure?");
        }

        public static void Inform(string message)
        {
            MessageBox.Show(message, "Music Database", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public static void Error(string message)
        {
            MessageBox.Show(message, "Music Database", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public static void Warn(string message)
        {
            MessageBox.Show(message, "Music Database", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
