using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Fedandburk.Common.Extensions;
using MvvmCross.ViewModels;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace Fedandburk.MvvmCross.Extensions.Tests;

[TestFixture]
public class FlatObservableCollectionTests
{
    private static int GetFlatItemIndex<T>(IEnumerable<IEnumerable<T>> items, int section, int index)
    {
        return GetFlatSectionIndex(items, section) + index;
    }

    private static int GetFlatSectionIndex<T>(IEnumerable<IEnumerable<T>> sections, int section)
    {
        return sections.Take(section).Sum(item => item.Count());
    }

    private static IEnumerable<T> GetFlatItems<T>(IEnumerable<IEnumerable<T>> sections)
    {
        return sections.SelectMany(item => item).ToArray();
    }

    [TestFixture]
    public class WhenCollectionCreated
    {
        [TestFixture]
        public class AndTheItemsSourceIsNull
        {
            [Test]
            public void ExceptionShouldBeThrown()
            {
                // ReSharper disable once ObjectCreationAsStatement
                Assert.Throws<ArgumentNullException>(() => new FlatObservableCollection<object>(null));
            }
        }

        [Test]
        public void ShouldContainInitialItems()
        {
            var items = new[] { new[] { 1, 2 }, new[] { 3 } };

            var sut = new FlatObservableCollection<int>(items);

            Assert.That(3, Is.EqualTo(sut.Count));
            Assert.That(sut.Contains(1), Is.True);
            Assert.That(sut.Contains(2), Is.True);
            Assert.That(sut.Contains(3), Is.True);
        }

        [Test]
        public void InitialItemsShouldBeInProvidedOrder()
        {
            var items = new[] { new[] { 1, 2 }, new[] { 3 } };

            var sut = new FlatObservableCollection<int>(items);

            Assert.That(0, Is.EqualTo(sut.IndexOf(1)));
            Assert.That(1, Is.EqualTo(sut.IndexOf(2)));
            Assert.That(2, Is.EqualTo(sut.IndexOf(3)));
        }
    }

    [TestFixture]
    public class WhenCollectionDisposed
    {
        [TestFixture]
        public class AndSourceChanged : MvxObservableCollectionTestBase
        {
            [Test]
            public void EventShouldNotBeRaised()
            {
                Setup();

                var events = new List<NotifyCollectionChangedEventArgs>();
                var items = new MvxObservableCollection<int[]>(new[] { new[] { 1, 2 }, new[] { 3 } });

                var sut = new FlatObservableCollection<int>(items);

                sut.CollectionChanged += (_, args) => events.Add(args);

                sut.Dispose();

                items.Add(new[] { 1 });
                items.Remove(new[] { 1 });

                Assert.That(0, Is.EqualTo(events.Count));
            }
        }
    }

    [TestFixture]
    public class WhenSectionsAdded : MvxObservableCollectionTestBase
    {
        [Test]
        [TestCase(new[] { 4 })]
        [TestCase(new[] { 4 }, new[] { 4 })]
        [TestCase(new[] { 4 }, new[] { 5 }, new[] { 6 })]
        public void AddEventShouldBeRaised(params int[][] newItems)
        {
            Setup();

            var events = new List<NotifyCollectionChangedEventArgs>();
            var items = new MvxObservableCollection<int[]>(new[] { new[] { 1, 2 }, new[] { 3 } });
            var flatIndex = GetFlatSectionIndex(items, items.Count);
            var flatItems = GetFlatItems(newItems);

            var sut = new FlatObservableCollection<int>(items);

            sut.CollectionChanged += (_, args) => events.Add(args);

            items.AddRange(newItems);

            Assert.That(1, Is.EqualTo(events.Count));
            Assert.That(NotifyCollectionChangedAction.Add, Is.EqualTo(events[0].Action));
            Assert.That(flatIndex, Is.EqualTo(events[0].NewStartingIndex));
            Assert.That(flatItems, Is.EqualTo(events[0].NewItems));
        }

        [Test]
        [TestCase(new[] { 4 })]
        [TestCase(new[] { 4 }, new[] { 4 })]
        [TestCase(new[] { 4 }, new[] { 5 }, new[] { 6 })]
        public void CollectionShouldBeUpdated(params int[][] newItems)
        {
            Setup();

            var items = new MvxObservableCollection<int[]>(new[] { new[] { 1, 2 }, new[] { 3 } });
            var flatItems = GetFlatItems(newItems);
            var flatCount = GetFlatItems(items).Count() + flatItems.Count();

            var sut = new FlatObservableCollection<int>(items);

            items.AddRange(newItems);

            Assert.That(flatCount, Is.EqualTo(sut.Count));
            Assert.That(flatItems, Is.EqualTo(sut.TakeLast(newItems.Length)));
        }
    }

    [TestFixture]
    public class WhenSectionInserted : MvxObservableCollectionTestBase
    {
        [Test]
        [TestCase(0, new[] { 4 })]
        [TestCase(1, new[] { 4, 4 })]
        [TestCase(2, new[] { 4, 5, 6 })]
        public void AddEventShouldBeRaised(int index, int[] newItem)
        {
            Setup();

            var events = new List<NotifyCollectionChangedEventArgs>();
            var items = new ObservableCollection<int[]>(new[] { new[] { 1, 2 }, new[] { 3 } });
            var flatIndex = GetFlatSectionIndex(items, index);
            var flatItems = GetFlatItems(new[] { newItem });

            var sut = new FlatObservableCollection<int>(items);

            sut.CollectionChanged += (_, args) => events.Add(args);

            items.Insert(index, newItem);

            Assert.That(1, Is.EqualTo(events.Count));
            Assert.That(NotifyCollectionChangedAction.Add, Is.EqualTo(events[0].Action));
            Assert.That(flatIndex, Is.EqualTo(events[0].NewStartingIndex));
            Assert.That(flatItems, Is.EqualTo(events[0].NewItems));
        }

        [Test]
        [TestCase(0, new[] { 4 })]
        [TestCase(1, new[] { 4, 4 })]
        [TestCase(2, new[] { 4, 5, 6 })]
        public void CollectionShouldBeUpdated(int index, int[] newItem)
        {
            Setup();

            var items = new ObservableCollection<int[]>(new[] { new[] { 1, 2 }, new[] { 3 } });
            var flatIndex = GetFlatSectionIndex(items, index);
            var flatItems = GetFlatItems(new[] { newItem });
            var flatCount = GetFlatItems(items).Count() + flatItems.Count();

            var sut = new FlatObservableCollection<int>(items);

            items.Insert(index, newItem);

            Assert.That(flatCount, Is.EqualTo(sut.Count));
            Assert.That(flatItems, Is.EqualTo(sut.Take(flatIndex, newItem.Length)));
        }
    }

    [TestFixture]
    public class WhenSectionsRemoved : MvxObservableCollectionTestBase
    {
        [Test]
        [TestCase(0, 1)]
        [TestCase(1, 1)]
        [TestCase(2, 1)]
        [TestCase(0, 3)]
        public void RemoveEventShouldBeRaised(int index, int count)
        {
            Setup();

            var events = new List<NotifyCollectionChangedEventArgs>();
            var items = new MvxObservableCollection<int[]>(new[] { new[] { 1, 2 }, new[] { 3 }, new[] { 4, 5, 6 } });
            var flatIndex = GetFlatSectionIndex(items, index);
            var flatItems = GetFlatItems(items.Take(index, count));

            var sut = new FlatObservableCollection<int>(items);

            sut.CollectionChanged += (_, args) => events.Add(args);

            items.RemoveRange(index, count);

            Assert.That(1, Is.EqualTo(events.Count));
            Assert.That(NotifyCollectionChangedAction.Remove, Is.EqualTo(events[0].Action));
            Assert.That(flatIndex, Is.EqualTo(events[0].OldStartingIndex));
            Assert.That(flatItems, Is.EqualTo(events[0].OldItems));
        }

        [Test]
        [TestCase(0, 1)]
        [TestCase(1, 1)]
        [TestCase(2, 1)]
        [TestCase(0, 3)]
        public void CollectionShouldBeUpdated(int index, int count)
        {
            Setup();

            var items = new MvxObservableCollection<int[]>(new[] { new[] { 1, 2 }, new[] { 3 }, new[] { 4, 5, 6 } });
            var flatItems = GetFlatItems(items.Take(index, count)).ToArray();
            var flatCount = GetFlatItems(items).Count() - flatItems.Length;

            var sut = new FlatObservableCollection<int>(items);

            items.RemoveRange(index, count);

            Assert.That(flatCount, Is.EqualTo(sut.Count));
            CollectionAssert.IsNotSubsetOf(flatItems, sut);
        }
    }

    [TestFixture]
    public class WhenSectionsReplaced : MvxObservableCollectionTestBase
    {
        [Test]
        [TestCase(new[] { 4 })]
        [TestCase(new[] { 4 }, new[] { 4 })]
        [TestCase(new[] { 4 }, new[] { 5 }, new[] { 6 })]
        public void ResetEventShouldBeRaised(params int[][] newItems)
        {
            Setup();

            var events = new List<NotifyCollectionChangedEventArgs>();
            var items = new MvxObservableCollection<int[]>(new[] { new[] { 1, 2 }, new[] { 3 }, new[] { 4, 5, 6 } });

            var sut = new FlatObservableCollection<int>(items);

            sut.CollectionChanged += (_, args) => events.Add(args);

            items.ReplaceWith(newItems);

            Assert.That(1, Is.EqualTo(events.Count));
            Assert.That(NotifyCollectionChangedAction.Reset, Is.EqualTo(events[0].Action));
        }

        [Test]
        [TestCase(new[] { 4 })]
        [TestCase(new[] { 4 }, new[] { 4 })]
        [TestCase(new[] { 4 }, new[] { 5 }, new[] { 6 })]
        public void CollectionShouldBeUpdated(params int[][] newItems)
        {
            Setup();

            var items = new MvxObservableCollection<int[]>(new[] { new[] { 1, 2 }, new[] { 3 }, new[] { 4, 5, 6 } });
            var flatItems = GetFlatItems(newItems).ToArray();
            var flatCount = flatItems.Length;

            var sut = new FlatObservableCollection<int>(items);

            items.ReplaceWith(newItems);

            Assert.That(flatCount, Is.EqualTo(sut.Count));
            Assert.That(flatItems, Is.EqualTo(sut));
        }
    }

    [TestFixture]
    public class WhenSectionsMoved : MvxObservableCollectionTestBase
    {
        [Test]
        [TestCase(0, 1)]
        [TestCase(1, 1)]
        [TestCase(2, 1)]
        public void MoveEventShouldBeRaised(int oldIndex, int newIndex)
        {
            Setup();

            var events = new List<NotifyCollectionChangedEventArgs>();
            var items = new MvxObservableCollection<int[]>(new[] { new[] { 1, 2 }, new[] { 3 }, new[] { 4, 5, 6 } });
            var flatOldIndex = GetFlatSectionIndex(items, oldIndex);

            var sut = new FlatObservableCollection<int>(items);

            sut.CollectionChanged += (_, args) => events.Add(args);

            items.Move(oldIndex, newIndex);

            var flatNewIndex = GetFlatSectionIndex(items, newIndex);
            var flatNewItems = GetFlatItems(items.Take(newIndex, 1));

            Assert.That(1, Is.EqualTo(events.Count));
            Assert.That(NotifyCollectionChangedAction.Move, Is.EqualTo(events[0].Action));
            Assert.That(flatOldIndex, Is.EqualTo(events[0].OldStartingIndex));
            Assert.That(flatNewIndex, Is.EqualTo(events[0].NewStartingIndex));
            Assert.That(flatNewItems, Is.EqualTo(events[0].NewItems));
            Assert.That(flatNewItems, Is.EqualTo(events[0].OldItems));
        }

        [Test]
        [TestCase(0, 1)]
        [TestCase(1, 1)]
        [TestCase(2, 1)]
        public void CollectionShouldBeUpdated(int oldIndex, int newIndex)
        {
            Setup();

            var items = new MvxObservableCollection<int[]>(new[] { new[] { 1, 2 }, new[] { 3 }, new[] { 4, 5, 6 } });
            var flatOldItems = GetFlatItems(items).ToArray();
            var flatOldCount = flatOldItems.Length;

            var sut = new FlatObservableCollection<int>(items);

            items.Move(oldIndex, newIndex);

            var flatNewItems = GetFlatItems(items).ToArray();
            var flatNewCount = flatNewItems.Length;

            Assert.That(flatOldCount, Is.EqualTo(sut.Count));
            Assert.That(flatNewCount, Is.EqualTo(sut.Count));
            Assert.That(flatNewItems, Is.EqualTo(sut));
        }
    }

    [TestFixture]
    public class WhenItemsAdded : MvxObservableCollectionTestBase
    {
        [Test]
        [TestCase(0, new[] { 4 })]
        [TestCase(0, new[] { 4, 4 })]
        [TestCase(1, new[] { 4, 5, 6 })]
        public void AddEventShouldBeRaised(int section, params int[] newItems)
        {
            Setup();

            var events = new List<NotifyCollectionChangedEventArgs>();

            var items = new MvxObservableCollection<MvxObservableCollection<int>>
            {
                new()
                {
                    1, 2
                },
                new()
                {
                    3
                }
            };

            var flatIndex = GetFlatItemIndex(items, section, items[section].Count);

            var sut = new FlatObservableCollection<int>(items);

            sut.CollectionChanged += (_, args) => events.Add(args);

            items[section].AddRange(newItems);

            Assert.That(1, Is.EqualTo(events.Count));
            Assert.That(NotifyCollectionChangedAction.Add, Is.EqualTo(events[0].Action));
            Assert.That(flatIndex, Is.EqualTo(events[0].NewStartingIndex));
            Assert.That(newItems, Is.EqualTo(events[0].NewItems));
        }

        [Test]
        [TestCase(0, new[] { 4 })]
        [TestCase(0, new[] { 4, 4 })]
        [TestCase(1, new[] { 4, 5, 6 })]
        public void CollectionShouldBeUpdated(int section, params int[] newItems)
        {
            Setup();

            var items = new MvxObservableCollection<MvxObservableCollection<int>>
            {
                new()
                {
                    1, 2
                },
                new()
                {
                    3
                }
            };

            var flatIndex = GetFlatItemIndex(items, section, items[section].Count);
            var flatCount = GetFlatItems(items).Count() + newItems.Length;

            var sut = new FlatObservableCollection<int>(items);

            items[section].AddRange(newItems);

            Assert.That(flatCount, Is.EqualTo(sut.Count));
            Assert.That(newItems, Is.EqualTo(sut.Take(flatIndex, newItems.Length)));
        }
    }

    [TestFixture]
    public class WhenItemsRemoved : MvxObservableCollectionTestBase
    {
        [Test]
        [TestCase(0, 1, 1)]
        [TestCase(1, 2, 2)]
        [TestCase(0, 0, 2)]
        [TestCase(1, 0, 4)]
        public void RemoveEventShouldBeRaised(int section, int start, int count)
        {
            Setup();

            var events = new List<NotifyCollectionChangedEventArgs>();

            var items = new MvxObservableCollection<MvxObservableCollection<int>>
            {
                new()
                {
                    1, 2
                },
                new()
                {
                    3, 4, 5, 6
                }
            };

            var flatIndex = GetFlatItemIndex(items, section, start);
            var oldItems = items[section].Take(start, count).ToArray();

            var sut = new FlatObservableCollection<int>(items);

            sut.CollectionChanged += (_, args) => events.Add(args);

            items[section].RemoveRange(start, count);

            Assert.That(1, Is.EqualTo(events.Count));
            Assert.That(NotifyCollectionChangedAction.Remove, Is.EqualTo(events[0].Action));
            Assert.That(flatIndex, Is.EqualTo(events[0].OldStartingIndex));
            Assert.That(oldItems, Is.EqualTo(events[0].OldItems));
        }

        [Test]
        [TestCase(0, 1, 1)]
        [TestCase(1, 2, 2)]
        [TestCase(0, 0, 2)]
        [TestCase(1, 0, 4)]
        public void CollectionShouldBeUpdated(int section, int start, int count)
        {
            Setup();

            var items = new MvxObservableCollection<MvxObservableCollection<int>>
            {
                new()
                {
                    1, 2
                },
                new()
                {
                    3, 4, 5, 6
                }
            };

            var flatCount = GetFlatItems(items).Count() - count;
            var oldItems = items[section].Take(start, count).ToArray();

            var sut = new FlatObservableCollection<int>(items);

            items[section].RemoveRange(start, count);

            Assert.That(flatCount, Is.EqualTo(sut.Count));
            CollectionAssert.IsNotSubsetOf(oldItems, sut);
        }
    }

    [TestFixture]
    public class WhenItemsReplaced : MvxObservableCollectionTestBase
    {
        [Test]
        [TestCase(0)]
        [TestCase(1, 7, 8)]
        public void ResetEventShouldBeRaised(int section, params int[] newItems)
        {
            Setup();

            var events = new List<NotifyCollectionChangedEventArgs>();

            var items = new MvxObservableCollection<MvxObservableCollection<int>>
            {
                new()
                {
                    1, 2
                },
                new()
                {
                    3, 4, 5, 6
                }
            };

            var sut = new FlatObservableCollection<int>(items);

            sut.CollectionChanged += (_, args) => events.Add(args);

            items[section].ReplaceWith(newItems);

            Assert.That(1, Is.EqualTo(events.Count));
            Assert.That(NotifyCollectionChangedAction.Reset, Is.EqualTo(events[0].Action));
        }

        [Test]
        [TestCase(0)]
        [TestCase(1, 7, 8)]
        public void CollectionShouldBeUpdated(int section, params int[] newItems)
        {
            Setup();

            var items = new MvxObservableCollection<MvxObservableCollection<int>>
            {
                new()
                {
                    1, 2
                },
                new()
                {
                    3, 4, 5, 6
                }
            };

            var sut = new FlatObservableCollection<int>(items);

            items[section].ReplaceWith(newItems);

            var flatItems = GetFlatItems(items).ToArray();
            var flatCount = flatItems.Length;

            Assert.That(flatCount, Is.EqualTo(sut.Count));
            Assert.That(flatItems, Is.EqualTo(sut));
        }
    }

    [TestFixture]
    public class WhenItemsMoved : MvxObservableCollectionTestBase
    {
        [Test]
        [TestCase(1, 1, 2)]
        [TestCase(1, 2, 2)]
        [TestCase(1, 2, 1)]
        public void MoveEventShouldBeRaised(int section, int oldIndex, int newIndex)
        {
            Setup();

            var events = new List<NotifyCollectionChangedEventArgs>();

            var items = new MvxObservableCollection<MvxObservableCollection<int>>
            {
                new()
                {
                    1, 2
                },
                new()
                {
                    3, 4, 5, 6
                }
            };

            var flatOldIndex = GetFlatItemIndex(items, section, oldIndex);
            var changedItems = items[section].Take(oldIndex, 1).ToArray();

            var sut = new FlatObservableCollection<int>(items);

            sut.CollectionChanged += (_, args) => events.Add(args);

            items[section].Move(oldIndex, newIndex);

            var flatNewIndex = GetFlatItemIndex(items, section, newIndex);

            Assert.That(1, Is.EqualTo(events.Count));
            Assert.That(NotifyCollectionChangedAction.Move, Is.EqualTo(events[0].Action));
            Assert.That(flatOldIndex, Is.EqualTo(events[0].OldStartingIndex));
            Assert.That(flatNewIndex, Is.EqualTo(events[0].NewStartingIndex));
            Assert.That(changedItems, Is.EqualTo(events[0].NewItems));
            Assert.That(changedItems, Is.EqualTo(events[0].OldItems));
        }

        [Test]
        [TestCase(1, 1, 2)]
        [TestCase(1, 2, 2)]
        [TestCase(1, 2, 1)]
        public void CollectionShouldBeUpdated(int section, int oldIndex, int newIndex)
        {
            Setup();

            var items = new MvxObservableCollection<MvxObservableCollection<int>>
            {
                new()
                {
                    1, 2
                },
                new()
                {
                    3, 4, 5, 6
                }
            };

            var flatOldItems = GetFlatItems(items).ToArray();
            var flatOldCount = flatOldItems.Length;

            var sut = new FlatObservableCollection<int>(items);

            items[section].Move(oldIndex, newIndex);

            var flatNewItems = GetFlatItems(items).ToArray();
            var flatNewCount = flatNewItems.Length;

            Assert.That(flatOldCount, Is.EqualTo(sut.Count));
            Assert.That(flatNewCount, Is.EqualTo(sut.Count));
            Assert.That(flatNewItems, Is.EqualTo(sut));
        }
    }

    [TestFixture]
    public class WhenSeveralOperationsPerformedInSequence : MvxObservableCollectionTestBase
    {
        [Test]
        public void OutputCollectionShouldBeCorrect()
        {
            Setup();

            var items = new MvxObservableCollection<MvxObservableCollection<int>>
            {
                new() { 1, 2 }
            };

            var sut = new FlatObservableCollection<int>(items);

            items.Add(new MvxObservableCollection<int> { 3, 4, 5, 6 });
            items[0].ReplaceWith(new[] { 1, 2, 0 });
            items[1].Add(8);
            items[0].Move(2, 0);
            items[1][4] = 7;

            Assert.That(Enumerable.Range(0, 8), Is.EqualTo(sut));
        }
    }
}