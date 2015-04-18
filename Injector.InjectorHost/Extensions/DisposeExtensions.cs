using System;
using System.Reactive.Disposables;

namespace Injector.InjectorHost.Extensions
{
    public static class DisposeExtensions
    {
        public static void DisposeWith(this IDisposable disposable, CompositeDisposable composite)
        {
            composite.Add(disposable);
        }
    }
}