using System;
using System.Collections.Generic;
using System.Text;

namespace MPPC.App.ViewModels
{
    public static class ViewModelResolver
    {
        private static Dictionary<Type, ViewModelBase> ViewModelsCache = new Dictionary<Type, ViewModelBase>();
        public static T GetViewModel<T>() where T : ViewModelBase, new()
        {
            if (ViewModelsCache.TryGetValue(typeof(T), out ViewModelBase value))
                return (T)value;
            return (T)(ViewModelsCache[typeof(T)] = new T());
        }
    }
}
