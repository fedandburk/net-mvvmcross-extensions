using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using MvvmCross.Base;
using MvvmCross.WeakSubscription;

namespace Fedandburk.MvvmCross.Extensions;

// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
public class WrappedObservableCollection<TItem, TWrappedItem>
    : IReadOnlyList<TWrappedItem>, INotifyCollectionChanged, IDisposable
{
    private readonly Func<TItem, TWrappedItem> _factory;
    private readonly IEnumerable<TItem> _sourceItems;

    private readonly List<TWrappedItem> _items;

    private IDisposable? _subscription;

    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    public int Count => _items.Count;

    public TWrappedItem this[int index] => _items[index];

    public WrappedObservableCollection(IEnumerable<TItem> source, Func<TItem, TWrappedItem> factory)
    {
        _sourceItems = source ?? throw new ArgumentNullException(nameof(source));
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));

        _items = new List<TWrappedItem>();

        if (_sourceItems is INotifyCollectionChanged notifyCollectionChanged)
        {
            _subscription = notifyCollectionChanged.WeakSubscribe(OnSourceChanged);
        }

        // ReSharper disable once VirtualMemberCallInConstructor
        Reset();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    private void Clear()
    {
        foreach (var item in _items)
        {
            item?.DisposeIfDisposable();
        }

        _items.Clear();
    }

    private TWrappedItem GetWrappedItem(TItem item)
    {
        return _factory(item);
    }

    private TWrappedItem GetWrappedItem(int index)
    {
        return GetWrappedItem(_sourceItems.ElementAt(index));
    }

    protected virtual NotifyCollectionChangedEventArgs? Add(NotifyCollectionChangedEventArgs args)
    {
        if (args.NewItems == null)
        {
            return null;
        }

        var index = args.NewStartingIndex;
        var items = new TWrappedItem[args.NewItems.Count];

        for (var i = 0; i < items.Length; i++)
        {
            var item = GetWrappedItem(i + index);

            _items.Insert(i + index, item);

            items[i] = item;
        }

        return new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, items, index);
    }

    protected virtual NotifyCollectionChangedEventArgs? Remove(NotifyCollectionChangedEventArgs args)
    {
        if (args.OldItems == null)
        {
            return null;
        }

        var index = args.OldStartingIndex;
        var items = new TWrappedItem[args.OldItems.Count];

        for (var i = 0; i < items.Length; i++)
        {
            var item = _items[index];

            _items.RemoveAt(index);

            items[i] = item;

            item?.DisposeIfDisposable();
        }

        return new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, items, index);
    }

    protected virtual NotifyCollectionChangedEventArgs? Move(NotifyCollectionChangedEventArgs args)
    {
        if (args.OldItems == null)
        {
            return null;
        }

        var fromIndex = args.OldStartingIndex;
        var toIndex = args.NewStartingIndex;
        var items = new TWrappedItem[args.OldItems.Count];

        for (var i = 0; i < items.Length; i++)
        {
            var item = _items[i];

            _items.RemoveAt(fromIndex);

            items[i] = item;

            item?.DisposeIfDisposable();
        }

        for (var i = 0; i < items.Length; i++)
        {
            var item = GetWrappedItem(i + toIndex);

            _items.Insert(i + toIndex, item);

            items[i] = item;
        }

        return new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, items, toIndex, fromIndex);
    }

    protected virtual NotifyCollectionChangedEventArgs? Replace(NotifyCollectionChangedEventArgs args)
    {
        if (args.OldItems == null)
        {
            return null;
        }

        var fromIndex = args.OldStartingIndex;
        var toIndex = args.NewStartingIndex;
        var items = new TWrappedItem[args.OldItems.Count];

        for (var i = 0; i < items.Length; i++)
        {
            var item = _items[i];

            _items.RemoveAt(fromIndex);

            items[i] = item;

            item?.DisposeIfDisposable();
        }

        for (var i = 0; i < items.Length; i++)
        {
            var item = GetWrappedItem(i + toIndex);

            _items.Insert(i + toIndex, item);

            items[i] = item;
        }

        return new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, items, toIndex, fromIndex);
    }

    protected virtual NotifyCollectionChangedEventArgs Reset()
    {
        Clear();

        _items.AddRange(_sourceItems.Select(GetWrappedItem));

        return new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
    }

    private void OnSourceChanged(object? _, NotifyCollectionChangedEventArgs args)
    {
        var newArgs = Update(args);

        if (newArgs != null)
        {
            RaiseCollectionChanged(newArgs);
        }
    }

    private NotifyCollectionChangedEventArgs? Update(NotifyCollectionChangedEventArgs args)
    {
        return args.Action switch
        {
            NotifyCollectionChangedAction.Add => Add(args),
            NotifyCollectionChangedAction.Remove => Remove(args),
            NotifyCollectionChangedAction.Move => Move(args),
            NotifyCollectionChangedAction.Replace => Replace(args),
            NotifyCollectionChangedAction.Reset => Reset(),
            _ => throw new ArgumentOutOfRangeException(nameof(NotifyCollectionChangedEventArgs))
        };
    }

    private void RaiseCollectionChanged(NotifyCollectionChangedEventArgs args)
    {
        CollectionChanged?.Invoke(this, args);
    }

    public IEnumerator<TWrappedItem> GetEnumerator()
    {
        return _items.GetEnumerator();
    }

    public virtual void Dispose()
    {
        Clear();

        _subscription?.Dispose();
        _subscription = null;
    }
}