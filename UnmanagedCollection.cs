using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
                                                            // todo: impl IList
public unsafe class UnmanagedCollection<T> : ICollection<T>, IReadOnlyList<T>, IDisposable where T : unmanaged
{
    // public getters
    public bool IsReadOnly => false;
    public int DataSizeInBytes => dataSizeInElements_ * elementSize_;
    public int UsedSizeInBytes => Count * elementSize_;

    // public getter with setter
    public T* Data { get; private set; }
    public int Count { get; private set; } = 0;

    // private fields
    private int dataSizeInElements_;

    // readonly
    private readonly float overflowMult_;
    private readonly int elementSize_;

    public UnmanagedCollection(int startingBufferSize = 8, float overflowMult = 1.5f)
    {
        if ((int)(startingBufferSize * overflowMult) < startingBufferSize + 1)
            throw new ArgumentOutOfRangeException("Overflow multiplier doesn't increase size");

        overflowMult_ = overflowMult;
        elementSize_ = sizeof(T);
        dataSizeInElements_ = startingBufferSize;
        Data = (T*)Marshal.AllocHGlobal(DataSizeInBytes);
    }
    
    ~UnmanagedCollection()
    {
        Marshal.FreeHGlobal((IntPtr)Data);
    }

    public void Dispose()
    {
        Marshal.FreeHGlobal((IntPtr)Data);
        GC.SuppressFinalize(this);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(T item)
    {
        if (Count + 1 > dataSizeInElements_)
            GrowMemoryBlock(GetNextBlockSize());

        Data[Count] = item;
        Count++;
    }

    public void AddRange(UnmanagedCollection<T> unmanagedCollection)
    {
        AssureSize(Count + unmanagedCollection.Count);

        for (int i = 0; i < unmanagedCollection.Count; i++)
            Data[Count + i] = unmanagedCollection.Data[i];

        Count += unmanagedCollection.Count;
    }

    public void AddRange(IList<T> collection)
    {
        AssureSize(Count + collection.Count);

        for (int i = 0; i < collection.Count; i++)
            Data[Count + i] = collection[i];

        Count += collection.Count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AssureSize(int size)
    {
        if (size > dataSizeInElements_)
        {
            var nextAccomodatingSize = GetNextBlockSize();
            while (nextAccomodatingSize < size)
                nextAccomodatingSize = GetNextBlockSize();
            GrowMemoryBlock(nextAccomodatingSize);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GetNextBlockSize()
    {
        return (int)(dataSizeInElements_ * overflowMult_);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void GrowMemoryBlock(int newElementCount)
    {
        var newDataSize = elementSize_ * newElementCount;
        var newData = (T*)Marshal.AllocHGlobal(newDataSize);
        Buffer.MemoryCopy(Data, newData, DataSizeInBytes, DataSizeInBytes);
        Marshal.FreeHGlobal((IntPtr)Data);
        dataSizeInElements_ = newElementCount;
        Data = newData;
    }

    public void Clear()
    {
        Count = 0;
    }

    public T this[int index]
    {
        set => Data[index] = value;
        get => Data[index];
    }

    public IEnumerator<T> GetEnumerator()
    {
        for (int i = 0; i < Count; i++)
            yield return this[i];
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
    
    public unsafe void FastForeach(Action<T> loopAction)
    {
        for (int i = 0; i < Count; i++)
            loopAction(Data[i]);
    }

    public bool Contains(T item)
    {
        for (int i = 0; i < Count; i++)
            if (Data[i].Equals(item))
                return true;
        return false;
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        if (arrayIndex + Count > array.Length)
            throw new IndexOutOfRangeException("Array to copy to doesn't have enough space");

        fixed (T* manArrDataPtr = &array[arrayIndex])
        {
            Buffer.MemoryCopy(Data, manArrDataPtr, UsedSizeInBytes, UsedSizeInBytes);
        }
    }

    public void CopyTo(IntPtr memAddr)
    {
        Buffer.MemoryCopy(Data, (void*)memAddr, UsedSizeInBytes, UsedSizeInBytes);
    }

    public bool Remove(T item)
    {
        throw new NotImplementedException("The memory block doesn't currently shrink");
    }

}