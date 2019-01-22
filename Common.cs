using System;
using System.Collections.Generic;

namespace Rationals
{
    public interface IHandler<T> {
        T Handle(T input);
    }

    public class HandlerPipe<T> : IHandler<T>
        where T : class {
        private IHandler<T>[] _handlers;
        public HandlerPipe(params IHandler<T>[] handlers) {
            _handlers = handlers;
        }
        public T Handle(T r) {
            for (int i = 0; i < _handlers.Length; ++i) {
                r = _handlers[i].Handle(r);
                if (r == null) break; //!!! compare references
            }
            return r;
        }
    }

}
