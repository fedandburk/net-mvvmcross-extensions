using System;
using System.ComponentModel;
using System.Linq.Expressions;

namespace Fedandburk.MvvmCross.Extensions;

public static class NotifyPropertyChangedExtensions
{
    /// <summary>
    /// Subscribes to a set of properties of a given source.
    /// </summary>
    /// <param name="propertyChanged">The source to subscribe on.</param>
    /// <param name="eventHandler">The callback.</param>
    /// <param name="properties">The set of properties to listen on.</param>
    /// <returns>Disposable that can be disposed to cancel a subscription.</returns>
    /// <exception cref="ArgumentNullException">If command or notifyPropertyChanged or properties are <c>null</c>.</exception>
    public static IDisposable WeakSubscribe(
        this INotifyPropertyChanged propertyChanged,
        EventHandler<PropertyChangedEventArgs> eventHandler,
        params Expression<Func<object>>[] properties
    )
    {
        if (propertyChanged == null)
        {
            throw new ArgumentNullException(nameof(propertyChanged));
        }

        if (eventHandler == null)
        {
            throw new ArgumentNullException(nameof(eventHandler));
        }

        if (properties == null)
        {
            throw new ArgumentNullException(nameof(properties));
        }

        return new MvxNamedNotifyPropertiesChangedEventSubscription(propertyChanged, eventHandler, properties);
    }
}