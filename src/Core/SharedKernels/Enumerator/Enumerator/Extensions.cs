using System.Runtime.CompilerServices;
using MvvmCross.Core.ViewModels;
using WB.Core.SharedKernels.DataCollection.Events.Interview.Dtos;

namespace WB.Core.SharedKernels.Enumerator
{
    internal static class Extensions
    {
        public static TReturn RaiseAndSetIfChanged<T, TReturn>(this T source, ref TReturn backingField, TReturn newValue, [CallerMemberName] string propertyName = "")
            where T : IMvxNotifyPropertyChanged
        {
            return MvxNotifyPropertyChangedExtensions.RaiseAndSetIfChanged(source, ref backingField, newValue, propertyName);
        }
    }
}