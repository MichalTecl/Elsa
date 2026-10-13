using System;
using System.Threading;

namespace CodeGeneration
{
    /// <summary>
    /// Generates identifiers for generated code. Unlike Guid.NewGuid() the produced names are
    /// deterministic (they depend only on the order of calls), so identical setup produces
    /// identical code on every start and the compiled assembly can be cached by its code hash.
    /// </summary>
    public static class UniqueNames
    {
        [ThreadStatic]
        private static Scope _currentScope;

        private static long _globalCounter;

        /// <summary>
        /// Returns a name fragment unique within the current scope (or within the process when no scope is open).
        /// The result is a valid identifier part (letters and digits only).
        /// </summary>
        public static string Next()
        {
            var scope = _currentScope;
            if (scope != null)
            {
                return $"g{++scope.Counter}";
            }

            return $"n{Interlocked.Increment(ref _globalCounter)}";
        }

        /// <summary>
        /// Opens a scope on the current thread where names are numbered from the beginning.
        /// Use it around generation of one compilation unit, so the generated code doesn't depend
        /// on what was generated before in the process.
        /// </summary>
        public static IDisposable BeginScope()
        {
            return new Scope(_currentScope);
        }

        private sealed class Scope : IDisposable
        {
            private readonly Scope _previous;
            private bool _disposed;

            public Scope(Scope previous)
            {
                _previous = previous;
                _currentScope = this;
            }

            public long Counter { get; set; }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                _currentScope = _previous;
            }
        }
    }
}
