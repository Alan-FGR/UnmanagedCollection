using System;

public static class UnmanagedCollectionTests
{
    private struct TestStruct
    {
        public readonly int i;
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

    public static void Run()
    {
        Console.WriteLine("Creating");
        var uc = new UnmanagedCollection<TestStruct>();
        Console.WriteLine($"uc elements: {String.Join(",", uc)}");

        Console.WriteLine("Adding single element");
        uc.Add(new TestStruct(1,2));
        Console.WriteLine($"uc elements: {String.Join(",", uc)}");

        Console.WriteLine("Adding range from managed array");
        uc.AddRange(new [] { new TestStruct(3,4), new TestStruct(5,6) });
        Console.WriteLine($"uc elements: {String.Join(",", uc)}");

        Console.WriteLine("Adding range from another unmanaged list");
        var otherUc = new UnmanagedCollection<TestStruct>();
        otherUc.AddRange(new [] { new TestStruct(7,8), new TestStruct(9,10) });
        uc.AddRange(otherUc);
        Console.WriteLine($"uc elements: {String.Join(",", uc)}");
        
        Console.WriteLine("Copying to managed array");
        var mArr = new TestStruct[5];
        uc.CopyTo(mArr,0);
        Console.WriteLine($"           uc elements: {String.Join(",", uc)}");
        Console.WriteLine($"managed array elements: {String.Join(",", uc)}");

        Console.WriteLine("Clearing");
        uc.Clear();
        Console.WriteLine($"uc elements: {String.Join(",", uc)}");
        
        Console.WriteLine("Adding single element after clearing");
        uc.Add(new TestStruct(1337,42));
        Console.WriteLine($"uc elements: {String.Join(",", uc)}");

        const int alot = 2 << 5;
        Console.WriteLine($"Adding a {alot} elements");
        for (int i = 0; i < alot; i++)
        {
            uc.Add(new TestStruct(i,i));
        }
        Console.WriteLine($"uc elements: {String.Join(",", uc)}");

        Console.WriteLine("Clearing");
        uc.Clear();
        Console.WriteLine($"uc elements: {String.Join(",", uc)}");

        Console.ReadKey();
    }
}
