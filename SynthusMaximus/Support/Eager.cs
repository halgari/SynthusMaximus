using System;
using System.Threading;
using System.Threading.Tasks;

namespace SynthusMaximus.Support
{
    public class Eager<T>
    {
        private TaskCompletionSource<T> _source;
        private Thread _t;

        public Eager(Func<T> f)
        {
            _source = new TaskCompletionSource<T>();
            _t = new Thread(() =>
            {
                try
                {
                    var result = f();
                    _source.SetResult(result);
                }
                catch (Exception e)
                {
                    _source.SetException(e);
                }
            });
            _t.Start();
        }
        public T Value => _source.Task.Result;

        public static Eager<T> Create(Func<T> f)
        {
            return new(f);
        }
    }
    
}