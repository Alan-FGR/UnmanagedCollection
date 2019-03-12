using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

public static class UnmanagedCollectionTests
{
    static Stopwatch sw_ = new Stopwatch();
    static Dictionary<string, List<long>> results_ = new Dictionary<string, List<long>>();
    static void Measure<T>(string text, bool warmup, Func<T> func)
    {
        if (warmup)
        {
            Console.WriteLine($"Warming up... ");
            func.Invoke();
            return;
        }

        Console.Write($"Measuring... ");

        sw_.Restart();
        T r = func.Invoke();
        long elapsedMilliseconds = sw_.ElapsedMilliseconds;

        if (!results_.ContainsKey(text))
            results_[text] = new List<long>();
        results_[text].Add(elapsedMilliseconds);

        Console.WriteLine($"it took {elapsedMilliseconds} ms to {text}. Result: {r}");
    }

    private struct TestStruct
    {
        public int i;
        public readonly float f;

        public TestStruct(int i, float f)
        {
            this.i = i;
            this.f = f;
        }

        public override string ToString()
        {
            return $"{i}&{f}";
        }
    }

    public static void Main()
    {
        Console.WriteLine("Creating");
        var uc = new UnmanagedCollection<TestStruct>();
        Console.WriteLine($"uc elements: {String.Join(",", uc)}");

        Console.WriteLine("Adding single element");
        uc.Add(new TestStruct(1, 2));
        Console.WriteLine($"uc elements: {String.Join(",", uc)}");

        Console.WriteLine("Adding range from managed array");
        uc.AddRange(new[] { new TestStruct(3, 4), new TestStruct(5, 6) });
        Console.WriteLine($"uc elements: {String.Join(",", uc)}");

        Console.WriteLine("Adding range from another unmanaged list");
        var otherUc = new UnmanagedCollection<TestStruct>();
        otherUc.AddRange(new[] { new TestStruct(7, 8), new TestStruct(9, 10) });
        uc.AddRange(otherUc);
        Console.WriteLine($"uc elements: {String.Join(",", uc)}");

        Console.WriteLine("Removing At 1");
        uc.RemoveAt(1);
        Console.WriteLine($"uc elements: {String.Join(",", uc)}");

        Console.WriteLine("Removing At Fast 1");
        uc.RemoveAtFast(1);
        Console.WriteLine($"uc elements: {String.Join(",", uc)}");

        Console.WriteLine($"Index Of 9&10: {uc.IndexOf(new TestStruct(9, 10))}");

        Console.WriteLine("Removing 9&10");
        uc.Remove(new TestStruct(9, 10));
        Console.WriteLine($"uc elements: {String.Join(",", uc)}");

        Console.WriteLine("Inserting 9&10 At 1");
        uc.Insert(1, new TestStruct(9, 10));
        Console.WriteLine($"uc elements: {String.Join(",", uc)}");

        Console.ReadKey();

        Console.WriteLine("Copying to managed array");
        var mArr = new TestStruct[5];
        uc.CopyTo(mArr, 0);
        Console.WriteLine($"           uc elements: {String.Join(",", uc)}");
        Console.WriteLine($"managed array elements: {String.Join(",", uc)}");

        Console.WriteLine("Clearing");
        uc.Clear();
        Console.WriteLine($"uc elements: {String.Join(",", uc)}");

        Console.WriteLine("Adding single element after clearing");
        uc.Add(new TestStruct(1337, 42));
        Console.WriteLine($"uc elements: {String.Join(",", uc)}");

        const int alot = 2 << 5;
        Console.WriteLine($"Adding a {alot} elements");
        for (int i = 0; i < alot; i++)
        {
            uc.Add(new TestStruct(i, i));
        }
        Console.WriteLine($"uc elements: {String.Join(",", uc)}");

        Console.WriteLine("Clearing");
        uc.Clear();
        Console.WriteLine($"uc elements: {String.Join(",", uc)}");

        //testing new copy optimization with primitives
        Console.WriteLine("Copying primitive types to managed array");
        TestCopy<byte>();
        TestCopy<sbyte>();
        TestCopy<short>();
        TestCopy<ushort>();
        TestCopy<int>();
        TestCopy<uint>();
        TestCopy<long>();
        TestCopy<ulong>();
        TestCopy<float>();
        TestCopy<double>();
        TestCopy<decimal>();
        TestCopy<char>();
        TestCopy<bool>();


        //performance tests
        foreach (var warmup in new []{true,false})
        for (int li = 0; li < 4; li++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();

            int qty = 10000000;

            var ucPerf = new UnmanagedCollection<TestStruct>(4, 2);
            var lsPerf = new List<TestStruct>();

            Measure($"Add {qty} to UnmanagedCollection", warmup, () =>
            {
                for (int i = 0; i < qty; i++)
                    ucPerf.Add(new TestStruct());
                return ucPerf.Count;
            });

            Measure($"Add {qty} to List", warmup, () =>
            {
                for (int i = 0; i < qty; i++)
                    lsPerf.Add(new TestStruct());
                return lsPerf.Count;
            });

            Measure($"Foreach UnmanagedCollection", warmup, () =>
            {
                foreach (TestStruct testStruct in ucPerf)
                {
                    var e = testStruct;
                    e.i++;
                }
                return ucPerf.Count;
            });

            Measure($"Fast Foreach UnmanagedCollection", warmup, () =>
            {
                unsafe
                {
                    ucPerf.FastForeach(ts =>
                    {
                        ts.i++;
                    });
                    return ucPerf.Count;
                }
            });

            Measure($"Foreach List", warmup, () =>
            {
                foreach (TestStruct testStruct in lsPerf)
                {
                    var e = testStruct;
                    e.i++;
                }
                return lsPerf.Count;
            });

            Measure($"For UnmanagedCollection", warmup, () =>
            {
                for (var i = 0; i < ucPerf.Count; i++)
                {
                    TestStruct e = ucPerf[i];
                    e.i++;
                }
                return ucPerf.Count;
            });

            Measure($"For Direct Access UnmanagedCollection", warmup, () =>
            {
                unsafe
                {
                    for (var i = 0; i < ucPerf.Count; i++)
                    {
                        TestStruct* e = &ucPerf.Data[i];
                        e->i++;
                    }
                    return ucPerf.Count;
                }
            });

            Measure($"For List", warmup, () =>
            {
                for (var i = 0; i < lsPerf.Count; i++)
                {
                    TestStruct e = lsPerf[i];
                    e.i++;
                }
                return lsPerf.Count;
            });

        }


        foreach (var result in results_)
        {
            Console.WriteLine($"Benchmarked avg of {result.Value.Count} samples totalling {result.Value.Average():F3} ms to {result.Key}");
        }


        Console.ReadKey();
    }

    static unsafe void TestCopy<T>() where T : unmanaged
    {
        var pArr = new T[5];
        var uCol = new UnmanagedCollection<T>();

        uCol.Add(ConvertNumber<T>(0));
        uCol.Add(ConvertNumber<T>(1));
        uCol.Add(ConvertNumber<T>(2));
        uCol.Add(ConvertNumber<T>(3));
        uCol.Add(ConvertNumber<T>(4));

        uCol.CopyTo(pArr, 0);

        string ucs = String.Join(",", uCol); //shitty equality test todo fix
        string mas = string.Join(",", pArr);
        Console.WriteLine($"{pArr[0].GetType()} passed: {ucs == mas}, {ucs}, {mas}");
    }

    static T ConvertNumber<T>(object t)
    {
        return (T)Convert.ChangeType(t, typeof(T));
    }

}
