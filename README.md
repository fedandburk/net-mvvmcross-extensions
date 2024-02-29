# Extensions for MvvmCross
![GitHub](https://img.shields.io/github/license/fedandburk/net-mvvmcross-extensions.svg)
![Nuget](https://img.shields.io/nuget/v/Fedandburk.MvvmCross.Extensions.svg)
[![CI](https://github.com/fedandburk/net-mvvmcross-extensions/actions/workflows/ci.yml/badge.svg)](https://github.com/fedandburk/net-mvvmcross-extensions/actions/workflows/ci.yml)
[![CD](https://github.com/fedandburk/net-mvvmcross-extensions/actions/workflows/cd.yml/badge.svg)](https://github.com/fedandburk/net-mvvmcross-extensions/actions/workflows/cd.yml)
[![CodeFactor](https://www.codefactor.io/repository/github/fedandburk/net-mvvmcross-extensions/badge)](https://www.codefactor.io/repository/github/fedandburk/net-mvvmcross-extensions)

Extensions is a .NET library with common extensions and helpers for [MvvmCross](https://github.com/MvvmCross/MvvmCross).

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

### INotifyPropertyChanged Extensions
To subscribe for changes of `INotifyPropertyChanged` properties:

```cs
Subscription = this.WeakSubscribe(EventHandler, () => PropertyOne, () => PropertyTwo);
```
