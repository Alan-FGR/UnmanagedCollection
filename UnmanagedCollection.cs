#if UNMANAGED_COLLECTION_IMPL_ILIST
#define UCIL
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;


public unsafe class UnmanagedCollection<T> :
#if UCIL
    IList<T>,
#else
    IEnumerable<T>,
#endif
    IDisposable where T : unmanaged
{
    // public getters
    public nuint DataSizeInBytes => allocatedSizeInElements_ * ElementSize;
    public nuint UsedSizeInBytes => ElementCount * ElementSize;
    public IntPtr DataIntPtr => (IntPtr)Data;
    private nuint ElementSize => (nuint)Unsafe.SizeOf<T>();

#if UCIL
    // IList<T> getters
    public bool IsReadOnly => false;
#endif
    public int Count => (int)ElementCount;

    // data members
    public T* Data { get; private set; }
    public nuint ElementCount { get; private set; }

#if DEBUG
    readonly
#endif
    private nuint allocatedSizeInElements_;

#if DEBUG
    readonly
#endif
    private float overflowMult_;

#if DEBUG
    readonly
#endif
    private nuint alignment_;

    public UnmanagedCollection(nuint startingBufferSize = 8, float overflowMult = 1.5f, nuint alignment = 128)
    {
        if ((startingBufferSize * overflowMult) <= startingBufferSize)
            throw new ArithmeticException("Overflow multiplier doesn't increase size. Try increasing it.");

        overflowMult_ = overflowMult;
        allocatedSizeInElements_ = startingBufferSize;
        alignment_ = alignment;
        Data = Allocate(DataSizeInBytes);
    }

    private T* Allocate(nuint sizeInBytes)
    {
        return (T*)NativeMemory.AlignedAlloc(sizeInBytes, alignment_);
    }

    public void Dispose()
    {
        NativeMemory.AlignedFree(Data);
        GC.SuppressFinalize(this);
    }

    ~UnmanagedCollection()
    {
        NativeMemory.AlignedFree(Data);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(T item)
    {
        if (ElementCount + 1 > allocatedSizeInElements_)
            GrowMemoryBlock(GetNextBlockSize());

        Data[ElementCount] = item;
        ElementCount++;
    }

    public void AddRange(UnmanagedCollection<T> otherUnmanagedCollection)
    {
        AssureSize(ElementCount + otherUnmanagedCollection.ElementCount);

        for (nuint i = 0; i < otherUnmanagedCollection.ElementCount; i++)
            Data[ElementCount + i] = otherUnmanagedCollection.Data[i];

        ElementCount += otherUnmanagedCollection.ElementCount;
    }

    public void AddRange(IList<T> collection)
    {
        AssureSize(ElementCount + (nuint)collection.Count);

        for (nuint i = 0; i < (nuint)collection.Count; i++)
            Data[ElementCount + i] = collection[(int)i];

        ElementCount += (nuint)collection.Count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AssureSize(nuint sizeInElements)
    {
        if (sizeInElements > allocatedSizeInElements_)
        {
            var nextAccomodatingSize = GetNextBlockSize();
            while (nextAccomodatingSize < sizeInElements)
                nextAccomodatingSize = GetNextBlockSize();
            GrowMemoryBlock(nextAccomodatingSize);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private nuint GetNextBlockSize()
    {
        return (nuint)(allocatedSizeInElements_ * overflowMult_);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void GrowMemoryBlock(nuint newElementCount)
    {
        var newDataSize = ElementSize * newElementCount;
        var newData = Allocate(newDataSize);
        Buffer.MemoryCopy(Data, newData, DataSizeInBytes, DataSizeInBytes);
        NativeMemory.AlignedFree(Data);
        ElementCount = newElementCount;
        Data = newData;
    }

    public void Clear()
    {
        ElementCount = 0;
    }

    public nuint IndexOf(T item)
    {
        for (nuint i = 0; i < ElementCount; i++)
            if (Data[i].Equals(item))
                return i;
        throw new ArgumentOutOfRangeException("Element not found in collection");
    }

    public void Insert(nuint index, T item)
    {
        AssureSize(ElementCount + 1);
        var trailingSize = (ElementCount - index) * ElementSize;
        Buffer.MemoryCopy(&Data[index], &Data[index + 1], trailingSize, trailingSize);
        Data[index] = item;
        ElementCount++;
    }

#if UCIL
    public void Insert(int index, T item)
    {
        AssureSize(ElementCount + 1);
        var trailingSize = (ElementCount - (nuint)index) * ElementSize;
        Buffer.MemoryCopy(&Data[index], &Data[index + 1], trailingSize, trailingSize);
        Data[index] = item;
        ElementCount++;
    }
#endif

    /// <summary>This is slow. Use RemoveAtFast() if you don't need stable order</summary>
    public void RemoveAt(nuint index)
    {
        var trailingSize = (ElementCount - index) * ElementSize;
        Buffer.MemoryCopy(&Data[index + 1], &Data[index], trailingSize, trailingSize);
        ElementCount--;
    }

    /// <summary>Removes element at index without preserving order (very fast)</summary>
    public void RemoveAtFast(nuint index)
    {
        Buffer.MemoryCopy(&Data[ElementCount - 1], &Data[index], ElementSize, ElementSize);
        ElementCount--;
    }

    // IEnumerable<T>
    public IEnumerator<T> GetEnumerator()
    {
        for (nuint i = 0; i < ElementCount; i++)
            yield return this[i];
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public ref T this[nuint index] => ref Data[index];

    public unsafe void FastForeach(Action<T> loopAction)
    {
        for (nuint i = 0; i < ElementCount; i++)
            loopAction(Data[i]);
    }

    public bool Contains(T item)
    {
        for (nuint i = 0; i < ElementCount; i++)
            if (Data[i].Equals(item))
                return true;
        return false;
    }

    public void CopyTo(T[] array, nuint startPositionInTargetArray = 0)
    {
        if (startPositionInTargetArray + ElementCount > (nuint)array.Length)
            throw new IndexOutOfRangeException("Array to copy into doesn't have enough space");

        fixed (T* manArrDataPtr = &array[startPositionInTargetArray])
        {
            Buffer.MemoryCopy(Data, manArrDataPtr, UsedSizeInBytes, UsedSizeInBytes);
        }
    }

    public void CopyTo(IntPtr memAddr)
    {
        Buffer.MemoryCopy(Data, (void*)memAddr, UsedSizeInBytes, UsedSizeInBytes);
    }

    public void Remove(T item)
    {
        var index = IndexOf(item);
        RemoveAt(index);
    }

    public void TrimExcess()
    {
        //todo shrink buffer but not beyond a value that doesn't increase when multiplied by overflowMult_
        throw new NotImplementedException();
    }
}