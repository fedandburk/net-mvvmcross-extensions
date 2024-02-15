using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Fedandburk.Common.Extensions;
using MvvmCross.ViewModels;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace Fedandburk.MvvmCross.Extensions.Tests;

[TestFixture]
public class WrappedObservableCollectionTests
{
    private static string Factory(int item)
    {
        return item.ToString();
    }

    private static IEnumerable<string> Transform(IEnumerable<int> items)
    {
        return items.Select(Factory);
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
                Assert.Throws<ArgumentNullException>(() =>
                    new WrappedObservableCollection<object, object>(null, null)
                );
            }
        }

        [TestFixture]
        public class AndTheFactoryIsNull
        {
            [Test]
            public void ExceptionShouldBeThrown()
            {
                // ReSharper disable once ObjectCreationAsStatement
                Assert.Throws<ArgumentNullException>(() =>
                    new WrappedObservableCollection<object, object>(Enumerable.Empty<object>(), null)
                );
            }
        }

        [Test]
        public void ShouldContainInitialItems()
        {
            var items = new[] { 1, 2, 3 };

            var sut = new WrappedObservableCollection<int, string>(items, item => item.ToString());

            Assert.That(3, Is.EqualTo(sut.Count));
            Assert.That(sut.Contains(1.ToString()), Is.True);
            Assert.That(sut.Contains(2.ToString()), Is.True);
            Assert.That(sut.Contains(3.ToString()), Is.True);
        }

        [Test]
        public void InitialItemsShouldBeInProvidedOrder()
        {
            var items = new[] { 1, 2, 3 };

            var sut = new WrappedObservableCollection<int, string>(items, item => item.ToString());

            Assert.That(0, Is.EqualTo(sut.IndexOf(1.ToString())));
            Assert.That(1, Is.EqualTo(sut.IndexOf(2.ToString())));
            Assert.That(2, Is.EqualTo(sut.IndexOf(3.ToString())));
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
                var items = new MvxObservableCollection<int> { 1, 2, 3 };

                var sut = new WrappedObservableCollection<int, string>(items, item => item.ToString());

                sut.CollectionChanged += (_, args) => events.Add(args);

                sut.Dispose();

                items.Add(4);
                items.Remove(4);

                Assert.That(0, Is.EqualTo(events.Count));
            }
        }
    }

    [TestFixture]
    public class WhenItemsAdded : MvxObservableCollectionTestBase
    {
        [Test]
        [TestCase(4)]
        [TestCase(4, 4)]
        [TestCase(4, 5, 6)]
        public void AddEventShouldBeRaised(params int[] newItems)
        {
            Setup();

            var events = new List<NotifyCollectionChangedEventArgs>();
            var items = new MvxObservableCollection<int> { 1, 2, 3 };
            var index = items.Count;

            var sut = new WrappedObservableCollection<int, string>(items, Factory);

            sut.CollectionChanged += (_, args) => events.Add(args);

            items.AddRange(newItems);

            Assert.That(1, Is.EqualTo(events.Count));
            Assert.That(NotifyCollectionChangedAction.Add, Is.EqualTo(events[0].Action));
            Assert.That(index, Is.EqualTo(events[0].NewStartingIndex));
            Assert.That(Transform(newItems), Is.EqualTo(events[0].NewItems));
        }

        [Test]
        [TestCase(4)]
        [TestCase(4, 4)]
        [TestCase(4, 5, 6)]
        public void CollectionShouldBeUpdated(params int[] newItems)
        {
            Setup();

            var items = new MvxObservableCollection<int> { 1, 2, 3 };
            var index = items.Count;

            var sut = new WrappedObservableCollection<int, string>(items, Factory);

            items.AddRange(newItems);

            Assert.That(items.Count, Is.EqualTo(sut.Count));
            Assert.That(Transform(newItems), Is.EqualTo(sut.Take(index, newItems.Length)));
        }
    }

    [TestFixture]
    public class WhenItemsRemoved : MvxObservableCollectionTestBase
    {
        [Test]
        [TestCase(1, 1)]
        [TestCase(2, 2)]
        [TestCase(0, 2)]
        [TestCase(0, 4)]
        public void RemoveEventShouldBeRaised(int start, int count)
        {
            Setup();

            var events = new List<NotifyCollectionChangedEventArgs>();
            var items = new MvxObservableCollection<int> { 1, 2, 3, 4, 5, 6 };
            var oldItems = Transform(items.ToArray().Take(start, count));

            var sut = new WrappedObservableCollection<int, string>(items, Factory);

            sut.CollectionChanged += (_, args) => events.Add(args);

            items.RemoveRange(start, count);

            Assert.That(1, Is.EqualTo(events.Count));
            Assert.That(NotifyCollectionChangedAction.Remove, Is.EqualTo(events[0].Action));
            Assert.That(start, Is.EqualTo(events[0].OldStartingIndex));
            Assert.That(oldItems, Is.EqualTo(events[0].OldItems));
        }

        [Test]
        [TestCase(1, 1)]
        [TestCase(2, 2)]
        [TestCase(0, 2)]
        [TestCase(0, 4)]
        public void CollectionShouldBeUpdated(int start, int count)
        {
            Setup();

            var items = new MvxObservableCollection<int> { 1, 2, 3, 4, 5, 6 };
            var oldItems = Transform(items.ToArray().Take(start, count));

            var sut = new WrappedObservableCollection<int, string>(items, Factory);

            items.RemoveRange(start, count);

            Assert.That(items.Count, Is.EqualTo(sut.Count));
            CollectionAssert.IsNotSubsetOf(oldItems, sut);
        }
    }

    [TestFixture]
    public class WhenItemsReplaced : MvxObservableCollectionTestBase
    {
        [Test]
        [TestCase(new int[0])]
        [TestCase(7, 8)]
        public void ResetEventShouldBeRaised(params int[] newItems)
        {
            Setup();

            var events = new List<NotifyCollectionChangedEventArgs>();
            var items = new MvxObservableCollection<int> { 1, 2, 3, 4, 5, 6 };

            var sut = new WrappedObservableCollection<int, string>(items, Factory);

            sut.CollectionChanged += (_, args) => events.Add(args);

            items.ReplaceWith(newItems);

            Assert.That(1, Is.EqualTo(events.Count));
            Assert.That(NotifyCollectionChangedAction.Reset, Is.EqualTo(events[0].Action));
        }

        [Test]
        [TestCase(new int[0])]
        [TestCase(7, 8)]
        public void CollectionShouldBeUpdated(params int[] newItems)
        {
            Setup();

            var items = new MvxObservableCollection<int> { 1, 2, 3, 4, 5, 6 };

            var sut = new WrappedObservableCollection<int, string>(items, Factory);

            items.ReplaceWith(newItems);

            Assert.That(Transform(newItems), Is.EqualTo(sut));
        }
    }

    [TestFixture]
    public class WhenItemsMoved : MvxObservableCollectionTestBase
    {
        [Test]
        [TestCase(1, 2)]
        [TestCase(2, 2)]
        [TestCase(2, 1)]
        public void MoveEventShouldBeRaised(int oldIndex, int newIndex)
        {
            Setup();

            var events = new List<NotifyCollectionChangedEventArgs>();
            var items = new MvxObservableCollection<int> { 1, 2, 3, 4, 5, 6 };
            var changedItems = new[] { Transform(items).ElementAt(oldIndex) };

            var sut = new WrappedObservableCollection<int, string>(items, Factory);

            sut.CollectionChanged += (_, args) => events.Add(args);

            items.Move(oldIndex, newIndex);

            Assert.That(1, Is.EqualTo(events.Count));
            Assert.That(NotifyCollectionChangedAction.Move, Is.EqualTo(events[0].Action));
            Assert.That(oldIndex, Is.EqualTo(events[0].OldStartingIndex));
            Assert.That(newIndex, Is.EqualTo(events[0].NewStartingIndex));
            Assert.That(changedItems, Is.EqualTo(events[0].NewItems));
            Assert.That(changedItems, Is.EqualTo(events[0].OldItems));
        }

        [Test]
        [TestCase(1, 2)]
        [TestCase(2, 2)]
        [TestCase(2, 1)]
        public void CollectionShouldBeUpdated(int oldIndex, int newIndex)
        {
            Setup();

            var items = new MvxObservableCollection<int> { 1, 2, 3, 4, 5, 6 };

            var sut = new WrappedObservableCollection<int, string>(items, Factory);

            items.Move(oldIndex, newIndex);

            Assert.That(Transform(items), Is.EqualTo(sut));
        }
    }
}