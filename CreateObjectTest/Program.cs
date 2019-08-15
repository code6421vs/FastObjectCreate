using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CreateObjectTest
{
    public static class LinqBuilderWithoutCachingWithGeneric<T>
    {
        private static Dictionary<Type, Func<T>> _cache = new Dictionary<Type, Func<T>>();

        public static T Build()
        {
            if (!_cache.ContainsKey(typeof(T)))
            {
                var t = typeof(T);
                var ex = new Expression[] { Expression.New(t) };
                var block = Expression.Block(t, ex);
                var builder = Expression.Lambda<Func<T>>(block).Compile();
                _cache[typeof(T)] = builder;
            }
            return _cache[typeof(T)]();
        }
    }

    public static class ReflectionOnly<T>
    {
        private static Dictionary<Type, ConstructorInfo> _cache = new Dictionary<Type, ConstructorInfo>();
        public static T Build()
        {
            if(!_cache.ContainsKey(typeof(T)))
            {
                _cache[typeof(T)] = typeof(T).GetConstructor(new Type[] { });
            }
            return (T)_cache[typeof(T)].Invoke(null);
        }
    }

    public static class MyEmit<T>
    {
        private static Dictionary<Type, InvokeMethod> _cache = new Dictionary<Type, InvokeMethod>();

        delegate T InvokeMethod();
        public static T Build()
        {
            if (!_cache.ContainsKey(typeof(T)))
            {
                ConstructorInfo emptyConstructor = typeof(T).GetConstructor(Type.EmptyTypes);
                var dynamicMethod = new DynamicMethod("CreateInstance", typeof(T), Type.EmptyTypes, typeof(MyEmit<T>));
                ILGenerator ilGenerator = dynamicMethod.GetILGenerator();
                ilGenerator.Emit(OpCodes.Newobj, emptyConstructor);
                ilGenerator.Emit(OpCodes.Ret);
                _cache[typeof(T)] = (InvokeMethod)dynamicMethod.CreateDelegate(typeof(InvokeMethod));
            }
            return _cache[typeof(T)]();
        }
    }

    class MyClass
    {
        public string TestProp { get; set; }
    }

    class Program
    {
        static void Benchmark(Action func, string label)
        {
            var sw = new Stopwatch();
            sw.Start();
            func();
            sw.Stop();
            Console.WriteLine($"{label} elapsed {sw.ElapsedMilliseconds} ms");
        }

        static void TestLinq()
        {
            for(int i = 0; i < 10000000; i++)
            {
                var o = LinqBuilderWithoutCachingWithGeneric<MyClass>.Build();
            }
        }

        static void TestEmit()
        {
            for (int i = 0; i < 10000000; i++)
            {
                var o = MyEmit<MyClass>.Build();
            }
        }

        static void TestReflection()
        {
            for (int i = 0; i < 10000000; i++)
            {
                var o = ReflectionOnly<MyClass>.Build();
            }
        }

        static void Main(string[] args)
        {
            Benchmark(TestLinq, "Linq");
            Benchmark(TestEmit, "Emit");
            Benchmark(TestReflection, "Reflection");
            Benchmark(TestLinq, "Linq");
            Benchmark(TestEmit, "Emit");
            Benchmark(TestReflection, "Reflection");
            Benchmark(TestLinq, "Linq");
            Benchmark(TestEmit, "Emit");
            Benchmark(TestReflection, "Reflection");
            Console.ReadLine();
        }
    }
}
