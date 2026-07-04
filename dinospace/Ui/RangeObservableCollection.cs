using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace dinospace
{
    // ObservableCollection that can be refilled in one shot. Clear()+Add()
    // in a loop fires a change event per item, which makes CollectionView
    // re-layout dozens of times and stutter when switching filters. ReplaceAll
    // raises a single Reset, so the list re-renders exactly once.
    public class RangeObservableCollection<T> : ObservableCollection<T>
    {
        public void ReplaceAll(IEnumerable<T> items)
        {
            Items.Clear();
            foreach (var i in items) Items.Add(i);
            OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs(nameof(Count)));
            OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("Item[]"));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }
}
