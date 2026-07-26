// TODO: delete
//using System;

///// <summary>
///// A list with a fixed maximum capacity that never reallocates or resizes.
///// </summary>
//public class FixedList<T> where T : class {
//    readonly T[] items;

//    public int Count { get; private set; }

//    public int Capacity => items.Length;

//    public FixedList(int capacity) {
//        items = new T[capacity];
//    }

//    public bool Add(T item) {
//        if (Count == Capacity)
//            return false;
//        items[Count++] = item;
//        return true;
//    }

//    public bool Remove(T item) {
//        for (int i = 0; i < Count; i++) {
//            if (!ReferenceEquals(items[i], item))
//                continue;
//            items[i] = items[--Count];
//            items[Count] = null;
//            return true;
//        }
//        return false;
//    }

//    public bool Contains(T item) {
//        for (int i = 0; i < Count; i++) {
//            if (ReferenceEquals(items[i], item))
//                return true;
//        }
//        return false;
//    }

//    /// <summary>
//    /// Removes all items.
//    /// </summary>
//    public void Clear() {
//        for (int i = 0; i < Count; i++)
//            items[i] = null;

//        Count = 0;
//    }

//    public bool IsFull => Count == Capacity;

//    public T this[int index] {
//        get {
//            if ((uint)index >= Count)
//                throw new ArgumentOutOfRangeException(nameof(index));
//            return items[index];
//        }
//    }
//}
