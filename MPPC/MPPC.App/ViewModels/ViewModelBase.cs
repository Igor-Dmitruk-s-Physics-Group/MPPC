using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Windows;

namespace MPPC.App.ViewModels
{
    public class ViewModelBase<TParent> : ViewModelBase, INotifyPropertyChanged where TParent : ViewModelBase, new()
    {
        public TParent Parent { get; set; }

        public ViewModelBase() : base()
        {
            Parent = ViewModelResolver.GetViewModel<TParent>();
        }
    }

    public class ViewModelBase : INotifyPropertyChanged
    {


        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }

             ));
        }
         

        public ViewModelBase()
        { 
        }
    }
}
