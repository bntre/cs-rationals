using System;
using System.Collections.Generic;

namespace Rationals
{
    public interface IHandler<T> {
        bool Handle(T input); // false for reject
    }

    public class HandlerPipe<T> : IHandler<T> {
        private IHandler<T>[] _handlers;
        public HandlerPipe(params IHandler<T>[] handlers) {
            _handlers = handlers;
        }
        public bool Handle(T item) {
            for (int i = 0; i < _handlers.Length; ++i) {
                bool accepted = _handlers[i].Handle(item);
                if (!accepted) return false;
            }
            return true;
        }
    }

    public interface IIterator<T> {
        void Iterate(IHandler<T> handler);
    }

    public class Collector<T> : IHandler<T>, IIterator<T> {
        private List<T> _items = new List<T>();
        // First collect them all
        public bool Handle(T item) {
            _items.Add(item);
            return true;
        }
        // Then sort/iterate
        public void Iterate(IHandler<T> handler) {
            int len = _items.Count;
            for (int i = 0; i < len; ++i) {
                handler.Handle(_items[i]);
            }
        }
        public void Iterate(Comparison<T> comparison, IHandler<T> handler) {
            _items.Sort(comparison);
            Iterate(handler);
        }
    }
}
