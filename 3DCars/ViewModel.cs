using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace _3DCars
{
    public class ViewModel : INotifyPropertyChanging, INotifyPropertyChanged
    {
        public event PropertyChangingEventHandler PropertyChanging;

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanging([CallerMemberName] String propertyName = "")
        {
            this.PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(propertyName));
        }

        public void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
