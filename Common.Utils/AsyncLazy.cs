using System;
using System.Threading.Tasks;

namespace Common.Utils
{
    public class AsyncLazy : Lazy<Task>
    {
        public AsyncLazy(Func<Task> taskFactory) : base(() => Task.Run(taskFactory))
        {
        }
    }

    public class AsyncLazy<T> : Lazy<Task<T>>
    {
        public AsyncLazy(Func<T> valueFactory) : base(() => Task.Factory.StartNew(valueFactory))
        {
        }

        public AsyncLazy(Func<Task<T>> taskFactory) : base(() => Task.Run(taskFactory))
        {
        }
    }
}