using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public unsafe class UnmanagedCollection<T> : ICollection<T> where T : unmanaged
{
    // public getters
    public bool IsReadOnly => false;
    public int DataSizeInBytes => dataSizeInElements_ * elementSize_;
    public IntPtr Data => (IntPtr)data_;

    // public getter with setter
    public int Count { get; private set; } = 0;

    // private fields
    private int dataSizeInElements_;
    private T* data_;

    // readonly
    private readonly float overflowMult_;
    private readonly int elementSize_;

    public UnmanagedCollection(int startBufferSize = 16, float overflowMult = 1.25f)
    {
        if ((int)(startBufferSize * overflowMult) < startBufferSize + 1)
            throw new ArgumentOutOfRangeException("Overflow multiplier doesn't increase size");

        overflowMult_ = overflowMult;
        elementSize_ = Marshal.SizeOf<T>();
        dataSizeInElements_ = startBufferSize;
        data_ = (T*)Marshal.AllocHGlobal(DataSizeInBytes);
    }

    ~UnmanagedCollection()
    {
        Marshal.FreeHGlobal((IntPtr)data_);
    }

    public void Add(T item)
    {
        if (Count + 1 > dataSizeInElements_)
            GrowMemoryBlock(GetNextBlockSize());

        data_[Count] = item;
        Count++;
    }

    public void AddRange(UnmanagedCollection<T> unmanagedCollection)
    {
        AssureSize(Count + unmanagedCollection.Count);

        for (int i = 0; i < unmanagedCollection.Count; i++)
            data_[Count+i] = unmanagedCollection.data_[i];

        Count += unmanagedCollection.Count;
    }

    public void AddRange(IList<T> collection)
    {
        AssureSize(Count + collection.Count);

        for (int i = 0; i < collection.Count; i++)
            data_[Count+i] = collection[i];

        Count += collection.Count;
    }

    private void AssureSize(int size)
    {
        if (size > dataSizeInElements_)
            GrowMemoryBlock(size);
    }

    private int GetNextBlockSize()
    {
        return (int)(dataSizeInElements_ * overflowMult_);
    }

    private void GrowMemoryBlock(int newElementCount)
    {
        var newDataSize = elementSize_ * newElementCount;
        var newData = (T*)Marshal.AllocHGlobal(newDataSize);
        Buffer.MemoryCopy(data_, newData, DataSizeInBytes, DataSizeInBytes);
        Marshal.FreeHGlobal((IntPtr)data_);
        dataSizeInElements_ = newElementCount;
        data_ = newData;
    }

    public void Clear()
    {
        Count = 0;
    }

    public T GetUnsafe(int index)
    {
        return data_[index];
    }

    public IEnumerator<T> GetEnumerator()
    {
        for (int i = 0; i < Count; i++)
            yield return GetUnsafe(i);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public bool Contains(T item)
    {
        for (int i = 0; i < Count; i++)
            if (data_[i].Equals(item))
                return true;
        return false;
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        if (arrayIndex + Count > array.Length)
            throw new IndexOutOfRangeException("Array to copy to doesn't have enough space");

        for (int i = 0; i < Count; i++)
            array[arrayIndex + i] = data_[i];
    }

    public bool Remove(T item)
    {
        throw new NotImplementedException("The memory block doesn't currently shrink");
    }

}