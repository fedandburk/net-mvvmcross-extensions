# Extensions for MvvmCross
![GitHub](https://img.shields.io/github/license/fedandburk/net-mvvmcross-extensions.svg)
![Nuget](https://img.shields.io/nuget/v/Fedandburk.MvvmCross.Extensions.svg)
[![CI](https://github.com/fedandburk/net-mvvmcross-extensions/actions/workflows/ci.yml/badge.svg)](https://github.com/fedandburk/net-mvvmcross-extensions/actions/workflows/ci.yml)
[![CD](https://github.com/fedandburk/net-mvvmcross-extensions/actions/workflows/cd.yml/badge.svg)](https://github.com/fedandburk/net-mvvmcross-extensions/actions/workflows/cd.yml)
[![CodeFactor](https://www.codefactor.io/repository/github/fedandburk/net-mvvmcross-extensions/badge)](https://www.codefactor.io/repository/github/fedandburk/net-mvvmcross-extensions)

Extensions is a .Net Standard library with common extensions and helpers for [MvvmCross](https://github.com/MvvmCross/MvvmCross).

## Installation

Use [NuGet](https://www.nuget.org) package manager to install this library.

```bash
Install-Package Fedandburk.MvvmCross.Extensions
```

## Usage
```cs
using Fedandburk.MvvmCross.Extensions;
```

### ICommand Extensions
To check the execution ability and run the `IMvxAsyncCommand` asynchronously:

```cs
await Command.SafeExecuteAsync(parameter);
```

To cancel the `IMvxAsyncCommand` execution with `IsRunning` check:

```cs
Command.CancelExecution();
```

To raise `CanExecuteChanged` when one of the target properties changed:

```cs
Subscription = Command.RelayOn(
    NotifyPropertyChanged,
    () => NotifyPropertyChanged.Property
);
```

### IEnumerable Extensions
To transform a two-dimensional collection or an `ObservableCollection` into a single-dimensional collection and keep the `CollectionChanged` events:

```cs
Items = new ObservableCollection<ObservableCollection<int>>
{
    new ObservableCollection<int> { 1, 2 },
    new ObservableCollection<int> { 3 }
};

IEnumerable<int> flatItems = Items.ObservableFlatten();
```

To transform each element of a collection into a new form and keep the `CollectionChanged` events:

```cs
Items = new ObservableCollection<int> {1, 2, 3, 4, 5, 6};

IEnumerable<string> wrappedItems = Items.ObservableSelect(item => item.ToString());
```

### INotifyPropertyChanged Extensions
To subscribe for changes of `INotifyPropertyChanged` properties:

```cs
Subscription = this.WeakSubscribe(EventHandler, () => PropertyOne, () => PropertyTwo);
```

## Contributing
Pull requests are welcome. For major changes, please open an issue first to discuss what you would like to change.

Please make sure to update the tests as appropriate.

## License
[MIT](https://choosealicense.com/licenses/mit/)
