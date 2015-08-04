using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Cirrious.CrossCore.Core;
using Microsoft.Practices.ServiceLocation;

namespace WB.Core.BoundedContexts.Tester.ViewModels
{
    public class ObservableRangeCollection<T> : ObservableCollection<T>
    {
        private IMvxMainThreadDispatcher mvxMainThreadDispatcher
        {
            get { return ServiceLocator.Current.GetInstance<IMvxMainThreadDispatcher>(); }
        }

        private const string CountString = "Count";
        private const string IndexerName = "Item[]";

        public ObservableRangeCollection()
            : base()
        {
        }

        public ObservableRangeCollection(IEnumerable<T> collection)
            : base(collection)
        {
        }

        public ObservableRangeCollection(List<T> list)
            : base(list)
        {
        }

        public void RemoveRange(IEnumerable<T> collection)
        {
            if (collection == null) throw new ArgumentNullException("collection");

            var items = collection as IList<T> ?? collection.ToList();
            if (!items.Any()) return;

            this.CheckReentrancy();

            foreach (var item in items)
            {
                this.Items.Remove(item);
            }

            this.mvxMainThreadDispatcher.RequestMainThreadAction(() =>
            {
                this.OnPropertyChanged(new PropertyChangedEventArgs(CountString));
                this.OnPropertyChanged(new PropertyChangedEventArgs(IndexerName));
                this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            });
        }
    }
}