using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace NetProve.ViewModels
{
    public abstract class BaseViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            if (Application.Current?.Dispatcher.CheckAccess() == true)
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            else
                Application.Current?.Dispatcher.Invoke(() =>
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name)));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(name);
            return true;
        }
    }
}
