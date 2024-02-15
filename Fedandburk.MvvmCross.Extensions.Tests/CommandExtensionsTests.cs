using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;
using MvvmCross.Commands;
using MvvmCross.Tests;
using NSubstitute;
using NUnit.Framework;

namespace Fedandburk.MvvmCross.Extensions.Tests;

[TestFixture]
public class CommandExtensionsTests
{
    [TestFixture]
    public class WhenSafeExecuteAsyncCalled
    {
        [TestFixture]
        public class AndTheCommandIsNull
        {
            [Test]
            public void ExceptionsShouldNotBeThrown()
            {
                const int parameter = 1;
                Assert.DoesNotThrowAsync(() => default(ICommand).SafeExecuteAsync(parameter));
            }

            [Test]
            public async Task ExecuteShouldNotBeCalled()
            {
                const int parameter = 1;
                var command = Substitute.For<ICommand>();

                await command.SafeExecuteAsync(parameter).ConfigureAwait(false);

                command.DidNotReceive().Execute(Arg.Any<object>());
            }
        }

        [TestFixture]
        public class AndTheCommandIsNotExecutable
        {
            [Test]
            public async Task ShouldReturnFalse()
            {
                const int parameter = 1;
                var command = Substitute.For<ICommand>();

                var result = await command.SafeExecuteAsync(parameter).ConfigureAwait(false);

                Assert.That(result, Is.False);
            }

            [Test]
            public async Task ExecuteShouldNotBeCalled()
            {
                const int parameter = 1;
                var command = Substitute.For<ICommand>();

                await command.SafeExecuteAsync(parameter).ConfigureAwait(false);

                command.DidNotReceive().Execute(Arg.Any<object>());
            }
        }

        [TestFixture]
        public class AndTheCommandIsExecutable
        {
            [TestFixture]
            public class AndTheCommandIsAsyncCommand
            {
                [Test]
                public async Task ExecuteAsyncShouldBeCalled()
                {
                    const int parameter = 1;
                    var command = Substitute.For<IMvxAsyncCommand>();
                    command.CanExecute(Arg.Any<object>()).Returns(true);

                    await command.SafeExecuteAsync(parameter).ConfigureAwait(false);

                    await command.Received().ExecuteAsync(Arg.Is(parameter)).ConfigureAwait(false);
                }

                [Test]
                public async Task ShouldReturnTrue()
                {
                    const int parameter = 1;
                    var command = Substitute.For<IMvxAsyncCommand>();
                    command.CanExecute(Arg.Any<object>()).Returns(true);

                    var result = await command.SafeExecuteAsync(parameter).ConfigureAwait(false);

                    Assert.That(result, Is.True);
                }
            }

            [TestFixture]
            public class AndTheCommandIsNotAsyncCommand
            {
                [Test]
                public async Task ExecuteShouldBeCalled()
                {
                    const int parameter = 1;
                    var command = Substitute.For<ICommand>();
                    command.CanExecute(Arg.Any<object>()).Returns(true);

                    await command.SafeExecuteAsync(parameter).ConfigureAwait(false);

                    command.Received().Execute(Arg.Is(parameter));
                }

                [Test]
                public async Task ShouldReturnTrue()
                {
                    const int parameter = 1;
                    var command = Substitute.For<ICommand>();
                    command.CanExecute(Arg.Any<object>()).Returns(true);

                    var result = await command.SafeExecuteAsync(parameter).ConfigureAwait(false);

                    Assert.That(result, Is.True);
                }
            }
        }
    }

    [TestFixture]
    public class WhenCancelExecutionCalled
    {
        [TestFixture]
        public class AndTheCommandIsNull
        {
            [Test]
            public void ExceptionsShouldNotBeThrown()
            {
                Assert.DoesNotThrow(() => default(ICommand).CancelExecution());
            }

            [Test]
            public void ShouldReturnFalse()
            {
                Assert.That(default(ICommand).CancelExecution(), Is.False);
            }
        }

        [TestFixture]
        public class AndTheCommandIsNotMvxAsyncCommand
        {
            [Test]
            public void ShouldReturnFalse()
            {
                Assert.That(default(ICommand).CancelExecution(), Is.False);
            }
        }

        [TestFixture]
        public class AndTheCommandIsNotRunning
        {
            [Test]
            public void ShouldReturnFalse()
            {
                var command = new MvxAsyncCommand(() => Task.Delay(TimeSpan.FromMilliseconds(5)));

                Assert.That(command.CancelExecution(), Is.False);
            }
        }

        [TestFixture]
        public class AndTheCommandIsRunning : MvxIoCSupportingTest
        {
            [Test]
            public void ShouldReturnTrue()
            {
                var command = new MvxAsyncCommand(() => Task.Delay(TimeSpan.FromSeconds(1)));

                command.Execute();

                Assert.That(command.CancelExecution(), Is.True);
            }

            [Test]
            public void ShouldCancelCommandExecution()
            {
                Setup();

                var task = default(Task);
                var command = new MvxAsyncCommand(
                    cancellationToken => task = Task.Delay(TimeSpan.FromSeconds(1), cancellationToken)
                );

                command.Execute();
                command.CancelExecution();

                Assert.That(task.IsCanceled, Is.True);
            }
        }
    }

    [TestFixture]
    public class WhenRelayOnCalled
    {
        [TestFixture]
        public class AndTheCommandIsNull
        {
            [Test]
            public void ExceptionShouldBeThrown()
            {
                Assert.Throws<ArgumentNullException>(() => default(ICommand).RelayOn(null));
            }
        }

        [TestFixture]
        public class AndTheNotifyPropertyChangedIsNull
        {
            [Test]
            public void ExceptionShouldBeThrown()
            {
                var command = Substitute.For<ICommand>();

                Assert.Throws<ArgumentNullException>(() => command.RelayOn(null));
            }
        }

        [TestFixture]
        public class AndTheCommandIsNotMvxCommand
        {
            [Test]
            public void ShouldReturnNull()
            {
                var notifyPropertyChanged = Substitute.For<INotifyPropertyChanged>();
                var command = Substitute.For<ICommand>();

                Assert.That(command.RelayOn(notifyPropertyChanged), Is.Null);
            }
        }

        [TestFixture]
        public class AndTheCommandIsMvxCommand
        {
            [Test]
            public void ShouldNotReturnNull()
            {
                var notifyPropertyChanged = Substitute.For<INotifyPropertyChanged>();
                var command = Substitute.For<IMvxCommand>();

                Assert.That(command.RelayOn(notifyPropertyChanged), Is.Not.Null);
            }

            [TestFixture]
            public class AndSuitablePropertyChangedFired
            {
                [TestFixture]
                public class AndSubscriptionIsAlive
                {
                    [Test]
                    public void ShouldRaiseCanExecuteChanged()
                    {
                        var notifyPropertyChanged = Substitute.For<ITestNotifyPropertyChanged>();
                        var command = Substitute.For<IMvxCommand>();

                        var _ = command.RelayOn(notifyPropertyChanged, () => notifyPropertyChanged.FirstProperty);

                        notifyPropertyChanged.PropertyChanged += Raise.Event<PropertyChangedEventHandler>(
                            new PropertyChangedEventArgs(
                                nameof(ITestNotifyPropertyChanged.FirstProperty)
                            ));

                        command.Received().RaiseCanExecuteChanged();
                    }
                }

                [TestFixture]
                public class AndSubscriptionIsDisposed
                {
                    [Test]
                    public void ShouldNotRaiseCanExecuteChanged()
                    {
                        var notifyPropertyChanged = Substitute.For<ITestNotifyPropertyChanged>();
                        var command = Substitute.For<IMvxCommand>();

                        var subscription = command.RelayOn(
                            notifyPropertyChanged,
                            () => notifyPropertyChanged.FirstProperty
                        );

                        subscription.Dispose();

                        notifyPropertyChanged.PropertyChanged += Raise.Event<PropertyChangedEventHandler>(
                            new PropertyChangedEventArgs(
                                nameof(ITestNotifyPropertyChanged.FirstProperty)
                            ));

                        command.DidNotReceive().RaiseCanExecuteChanged();
                    }
                }
            }

            [TestFixture]
            public class AndAllPropertiesChangedFired
            {
                [TestFixture]
                public class AndSubscriptionIsAlive
                {
                    [Test]
                    public void ShouldRaiseCanExecuteChanged()
                    {
                        var notifyPropertyChanged = Substitute.For<INotifyPropertyChanged>();
                        var command = Substitute.For<IMvxCommand>();

                        var _ = command.RelayOn(notifyPropertyChanged);

                        notifyPropertyChanged.PropertyChanged += Raise.Event<PropertyChangedEventHandler>(
                            new PropertyChangedEventArgs(string.Empty)
                        );

                        command.Received().RaiseCanExecuteChanged();
                    }
                }
            }

            [TestFixture]
            public class AndNotSuitablePropertyChangedFired
            {
                [Test]
                public void ShouldNotRaiseCanExecuteChanged()
                {
                    var notifyPropertyChanged = Substitute.For<ITestNotifyPropertyChanged>();
                    var command = Substitute.For<IMvxCommand>();

                    var _ = command.RelayOn(notifyPropertyChanged, () => notifyPropertyChanged.FirstProperty);

                    notifyPropertyChanged.PropertyChanged += Raise.Event<PropertyChangedEventHandler>(
                        new PropertyChangedEventArgs(
                            nameof(ITestNotifyPropertyChanged.SecondProperty)
                        ));

                    command.DidNotReceive().RaiseCanExecuteChanged();
                }
            }
        }
    }
}