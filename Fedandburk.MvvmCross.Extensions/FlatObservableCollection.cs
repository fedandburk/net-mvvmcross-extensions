using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fedandburk.Common.Extensions;
using MvvmCross.WeakSubscription;

namespace Fedandburk.MvvmCross.Extensions;

/// <summary>
/// Represents a dynamic data collection that transforms two-dimensional observable collection into the flatten one
/// and provides notifications when items get added, removed, or when the whole list is refreshed.
/// </summary>
/// <typeparam name="TItem">The type of elements in the two-dimensional collection.</typeparam>
[SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
[SuppressMessage("ReSharper", "CoVariantArrayConversion")]
// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
public class FlatObservableCollection<TItem> : IReadOnlyList<TItem>, INotifyCollectionChanged, IDisposable
{
    private readonly IDictionary<IEnumerable<TItem>, IDisposable> _sectionSubscriptions;
    private readonly IEnumerable<IEnumerable<TItem>> _sourceItems;
    private readonly IList<TItem> _items;

    private IDisposable? _sectionsSubscription;
    private TItem[][]? _cachedSourceItems;

    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    public int Count => _items.Count;

    public TItem this[int index] => _items[index];

    public FlatObservableCollection(IEnumerable<IEnumerable<TItem>> items)
    {
        _sourceItems = items ?? throw new ArgumentNullException(nameof(items));

        _items = new List<TItem>();

        _sectionSubscriptions = new Dictionary<IEnumerable<TItem>, IDisposable>();

        if (_sourceItems is INotifyCollectionChanged collectionChanged)
        {
            _sectionsSubscription = collectionChanged.WeakSubscribe(OnSectionsChanged);
        }

        ResetSections();

        CacheSourceItems();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    private void CacheSourceItems()
    {
        _cachedSourceItems = _sourceItems.Select(item => item.ToArray()).ToArray();
    }

    private int GetFlatItemIndex(IEnumerable<IEnumerable<TItem>> items, int section, int index)
    {
        return GetFlatSectionIndex(items, section) + index;
    }

    private int GetFlatSectionIndex(IEnumerable<IEnumerable<TItem>> sections, int section)
    {
        return sections.Take(section).Sum(item => GetSectionItems(item).Length);
    }

    private void OnSectionsChanged(object? _, NotifyCollectionChangedEventArgs args)
    {
        var newArgs = UpdateSections(args);

        CacheSourceItems();

        if (newArgs != null)
        {
            RaiseCollectionChanged(newArgs);
        }
    }

    private void OnSectionChanged(object? sender, NotifyCollectionChangedEventArgs args)
    {
        if (sender is not IEnumerable<TItem> section)
        {
            throw new InvalidOperationException($"Sender should be {nameof(IEnumerable<TItem>)}");
        }

        var index = _sourceItems.IndexOf(section);

        if (index < 0)
        {
            throw new InvalidOperationException("Section position not found");
        }

        var newArgs = UpdateSection(index, args);

        CacheSourceItems();

        if (newArgs != null)
        {
            RaiseCollectionChanged(newArgs);
        }
    }

    private NotifyCollectionChangedEventArgs? UpdateSections(NotifyCollectionChangedEventArgs args)
    {
        return args.Action switch
        {
            NotifyCollectionChangedAction.Add => AddSections(args),
            NotifyCollectionChangedAction.Remove => RemoveSections(args),
            NotifyCollectionChangedAction.Move => MoveSections(args),
            NotifyCollectionChangedAction.Replace => ResetSections(),
            NotifyCollectionChangedAction.Reset => ResetSections(),
            _ => throw new ArgumentOutOfRangeException(nameof(NotifyCollectionChangedEventArgs))
        };
    }

    private NotifyCollectionChangedEventArgs? UpdateSection(int sectionIndex, NotifyCollectionChangedEventArgs args)
    {
        switch (args.Action)
        {
            case NotifyCollectionChangedAction.Add:
                return AddItems(sectionIndex, args);
            case NotifyCollectionChangedAction.Remove:
                return RemoveItems(sectionIndex, args);
            case NotifyCollectionChangedAction.Move:
                return MoveItems(sectionIndex, args);
            case NotifyCollectionChangedAction.Replace:
            case NotifyCollectionChangedAction.Reset:
                return ResetItems(sectionIndex);
            default:
                throw new ArgumentOutOfRangeException(nameof(NotifyCollectionChangedEventArgs));
        }
    }

    private void RaiseCollectionChanged(NotifyCollectionChangedEventArgs args)
    {
        CollectionChanged?.Invoke(this, args);
    }

    private TItem[] AddSections(int index, params IEnumerable<TItem>[] sections)
    {
        var items = new List<TItem>();

        foreach (var section in sections)
        {
            if (section is INotifyCollectionChanged collectionChanged)
            {
                _sectionSubscriptions[section] = collectionChanged.WeakSubscribe(OnSectionChanged);
            }

            var sectionItems = GetSectionItems(section);

            AddItems(index, 0, sectionItems);

            items.AddRange(sectionItems);

            index++;
        }

        return items.ToArray();
    }

    private TItem[] RemoveSections(params IEnumerable<TItem>[] sections)
    {
        var items = new List<TItem>();

        foreach (var section in sections)
        {
            if (_sectionSubscriptions.ContainsKey(section))
            {
                _sectionSubscriptions[section].Dispose();
                _sectionSubscriptions.Remove(section);
            }

            var sectionItems = GetSectionItems(section);

            RemoveItems(sectionItems);

            items.AddRange(sectionItems);
        }

        return items.ToArray();
    }

    private void AddItems(int sectionIndex, int itemIndex, params TItem[] items)
    {
        var flatIndex = GetFlatItemIndex(_sourceItems, sectionIndex, itemIndex);

        foreach (var item in items)
        {
            _items.Insert(flatIndex, item);

            flatIndex++;
        }
    }

    private void RemoveItems(params TItem[] items)
    {
        foreach (var item in items)
        {
            _items.Remove(item);
        }
    }

    private NotifyCollectionChangedEventArgs? AddSections(NotifyCollectionChangedEventArgs args)
    {
        if (args.NewItems == null)
        {
            return null;
        }

        var index = args.NewStartingIndex;
        var items = args.NewItems.OfType<IEnumerable<TItem>>().ToArray();

        var startingIndex = GetFlatSectionIndex(_sourceItems, index);

        var changedItems = AddSections(index, items);

        return new NotifyCollectionChangedEventArgs(
            NotifyCollectionChangedAction.Add,
            changedItems,
            startingIndex
        );
    }

    private NotifyCollectionChangedEventArgs? RemoveSections(NotifyCollectionChangedEventArgs args)
    {
        var cachedSourceItems = _cachedSourceItems;

        if (cachedSourceItems.IsNullOrEmpty())
        {
            return null;
        }

        if (args.OldItems == null)
        {
            return null;
        }

        var index = args.OldStartingIndex;
        var items = args.OldItems.OfType<IEnumerable<TItem>>().ToArray();

        var startingIndex = GetFlatSectionIndex(cachedSourceItems!, index);

        var changedItems = RemoveSections(items);

        return new NotifyCollectionChangedEventArgs(
            NotifyCollectionChangedAction.Remove,
            changedItems,
            startingIndex
        );
    }

    private NotifyCollectionChangedEventArgs? MoveSections(NotifyCollectionChangedEventArgs args)
    {
        var cachedSourceItems = _cachedSourceItems;

        if (cachedSourceItems.IsNullOrEmpty())
        {
            return null;
        }

        if (args.NewItems == null)
        {
            return null;
        }

        var index = args.NewStartingIndex;
        var items = args.NewItems.OfType<IEnumerable<TItem>>().ToArray();

        var oldStartingIndex = GetFlatSectionIndex(cachedSourceItems!, args.OldStartingIndex);
        var newStartingIndex = GetFlatSectionIndex(_sourceItems, index);

        var changedItems = RemoveSections(items);
        AddSections(index, items);

        return new NotifyCollectionChangedEventArgs(
            NotifyCollectionChangedAction.Move,
            changedItems,
            newStartingIndex,
            oldStartingIndex
        );
    }

    private NotifyCollectionChangedEventArgs ResetSections()
    {
        var cachedSourceItems = _cachedSourceItems;

        if (!cachedSourceItems.IsNullOrEmpty())
        {
            RemoveSections(cachedSourceItems!);
        }

        AddSections(0, _sourceItems.ToArray());

        return new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
    }

    private NotifyCollectionChangedEventArgs? AddItems(int sectionIndex, NotifyCollectionChangedEventArgs args)
    {
        if (args.NewItems == null)
        {
            return null;
        }

        var itemIndex = args.NewStartingIndex;
        var items = args.NewItems.OfType<TItem>().ToArray();

        var startingIndex = GetFlatItemIndex(_sourceItems, sectionIndex, itemIndex);

        AddItems(sectionIndex, itemIndex, items);

        return new NotifyCollectionChangedEventArgs(
            NotifyCollectionChangedAction.Add,
            items,
            startingIndex
        );
    }

    private NotifyCollectionChangedEventArgs? RemoveItems(int sectionIndex, NotifyCollectionChangedEventArgs args)
    {
        if (args.OldItems == null)
        {
            return null;
        }

        var itemIndex = args.OldStartingIndex;
        var items = args.OldItems.OfType<TItem>().ToArray();

        var startingIndex = GetFlatItemIndex(_sourceItems, sectionIndex, itemIndex);

        RemoveItems(items);

        return new NotifyCollectionChangedEventArgs(
            NotifyCollectionChangedAction.Remove,
            items,
            startingIndex
        );
    }

    private NotifyCollectionChangedEventArgs? MoveItems(int sectionIndex, NotifyCollectionChangedEventArgs args)
    {
        if (args.NewItems == null)
        {
            return null;
        }

        var index = args.NewStartingIndex;
        var items = args.NewItems.OfType<TItem>().ToArray();

        var oldStartingIndex = GetFlatItemIndex(_sourceItems, sectionIndex, args.OldStartingIndex);
        var newStartingIndex = GetFlatItemIndex(_sourceItems, sectionIndex, index);

        RemoveItems(items);
        AddItems(sectionIndex, index, items);

        return new NotifyCollectionChangedEventArgs(
            NotifyCollectionChangedAction.Move,
            items,
            newStartingIndex,
            oldStartingIndex
        );
    }

    private NotifyCollectionChangedEventArgs ResetItems(int sectionIndex)
    {
        var cachedSourceItems = _cachedSourceItems;

        if (!cachedSourceItems.IsNullOrEmpty())
        {
            RemoveItems(cachedSourceItems![sectionIndex]);
        }

        AddItems(sectionIndex, 0, _sourceItems.ElementAt(sectionIndex).ToArray());

        return new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
    }

    protected virtual TItem[] GetSectionItems(IEnumerable<TItem> section)
    {
        return section.ToArray();
    }

    public IEnumerator<TItem> GetEnumerator()
    {
        return _items.GetEnumerator();
    }

    public void Dispose()
    {
        _cachedSourceItems = null;

        _sectionsSubscription?.Dispose();
        _sectionsSubscription = null;

        foreach (var sectionSubscription in _sectionSubscriptions.Values)
        {
            sectionSubscription.Dispose();
        }

        _sectionSubscriptions.Clear();
    }
}