using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Windows;
using System.Windows.Media;

namespace BaseWPFLibrary.Others
{
    public static class EmasaWPFLibraryStaticMethods
    {
        public static IEnumerable<IEnumerable<T>> GetAllPossibleCombos<T>(IEnumerable<IEnumerable<T>> inValues)
        {
            IEnumerable<IEnumerable<T>> combos = new T[][] { new T[0] };

            foreach (var inner in inValues)
                combos = from c in combos
                    from i in inner
                    select c.Append(i);

            return combos;
        }

        public static IEnumerable<TSource> Append<TSource>(this IEnumerable<TSource> source, TSource item)
        {
            foreach (TSource element in source)
                yield return element;

            yield return item;
        }

        /// <summary>
        /// A helper to give an integer percent of a progress - to be used in a ProgressBar
        /// </summary>
        /// <param name="Current">The current value.</param>
        /// <param name="Maximum">The maximum value.</param>
        /// <returns></returns>
        public static int ProgressPercent(int Current, int Maximum)
        {
            return (int)Math.Round(((double)Current) * 100 / ((double)(Maximum)));
        }
        public static int ProgressPercent(long Current, long Maximum)
        {
            return (int)Math.Round(((double)Current) * 100 / ((double)(Maximum)));
        }

        // For use with no-parameter constructors. Also contains constants and utility methods
        public static class FastActivator
        {
            // THIS VERSION NOT THREAD SAFE YET
            static Dictionary<Type, Func<object>> constructorCache = new Dictionary<Type, Func<object>>();

            private const string DynamicMethodPrefix = "DM$_FastActivator_";

            public static object CreateInstance(Type objType)
            {
                return GetConstructor(objType)();
            }

            public static Func<object> GetConstructor(Type objType)
            {
                if (!constructorCache.TryGetValue(objType, out var constructor))
                {
                    constructor = (Func<object>)BuildConstructorDelegate(objType, typeof(Func<object>), new Type[] { });
                    constructorCache.Add(objType, constructor);
                }
                return constructor;
            }

            public static object BuildConstructorDelegate(Type objType, Type delegateType, Type[] argTypes)
            {
                var dynMethod = new DynamicMethod(DynamicMethodPrefix + objType.Name + "$" + argTypes.Length.ToString(), objType, argTypes, objType);
                ILGenerator ilGen = dynMethod.GetILGenerator();
                for (int argIdx = 0; argIdx < argTypes.Length; argIdx++)
                {
                    ilGen.Emit(OpCodes.Ldarg, argIdx);
                }
                ilGen.Emit(OpCodes.Newobj, objType.GetConstructor(argTypes));
                ilGen.Emit(OpCodes.Ret);
                return dynMethod.CreateDelegate(delegateType);
            }
        }

        // For use with one-parameter constructors, argument type = T1
        public static class FastActivator<T1>
        {
            // THIS VERSION NOT THREAD SAFE YET
            static Dictionary<Type, Func<T1, object>> constructorCache = new Dictionary<Type, Func<T1, object>>();
            public static object CreateInstance(Type objType, T1 arg1)
            {
                return GetConstructor(objType, new Type[] { typeof(T1) })(arg1);
            }
            public static Func<T1, object> GetConstructor(Type objType, Type[] argTypes)
            {
                if (!constructorCache.TryGetValue(objType, out var constructor))
                {
                    constructor = (Func<T1, object>)FastActivator.BuildConstructorDelegate(objType, typeof(Func<T1, object>), argTypes);
                    constructorCache.Add(objType, constructor);
                }
                return constructor;
            }
        }

        // For use with two-parameter constructors, argument types = T1, T2
        public static class FastActivator<T1, T2>
        {
            // THIS VERSION NOT THREAD SAFE YET
            static Dictionary<Type, Func<T1, T2, object>> constructorCache = new Dictionary<Type, Func<T1, T2, object>>();
            public static object CreateInstance(Type objType, T1 arg1, T2 arg2)
            {
                return GetConstructor(objType, new Type[] { typeof(T1), typeof(T2) })(arg1, arg2);
            }

            public static Func<T1, T2, object> GetConstructor(Type objType, Type[] argTypes)
            {
                if (!constructorCache.TryGetValue(objType, out var constructor))
                {
                    constructor = (Func<T1, T2, object>)FastActivator.BuildConstructorDelegate(objType, typeof(Func<T1, T2, object>), argTypes);
                    constructorCache.Add(objType, constructor);
                }
                return constructor;
            }
        }

        // For use with three-parameter constructors, argument types = T1, T2, T3
        // NB: could possibly merge these FastActivator<T1,...> classes and avoid generic type parameters
        // but would need to take care that cache entries were keyed to distinguish constructors having 
        // the same number of parameters but of different types. Keep separate for now.
        public static class FastActivator<T1, T2, T3>
        {
            // THIS VERSION NOT THREAD SAFE YET
            static Dictionary<Type, Func<T1, T2, T3, object>> constructorCache = new Dictionary<Type, Func<T1, T2, T3, object>>();
            public static object CreateInstance(Type objType, T1 arg1, T2 arg2, T3 arg3)
            {
                return GetConstructor(objType, new Type[] { typeof(T1), typeof(T2), typeof(T3) })(arg1, arg2, arg3);
            }

            public static Func<T1, T2, T3, object> GetConstructor(Type objType, Type[] argTypes)
            {
                if (!constructorCache.TryGetValue(objType, out var constructor))
                {
                    constructor = (Func<T1, T2, T3, object>)FastActivator.BuildConstructorDelegate(objType, typeof(Func<T1, T2, T3, object>), argTypes);
                    constructorCache.Add(objType, constructor);
                }
                return constructor;
            }
        }

        public static IEnumerable<T> FindVisualDescendents<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }
        public static IEnumerable<T> FindVisualChildren<T>(this DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) throw new ArgumentNullException(nameof(parent));

            var queue = new Queue<DependencyObject>(new[] { parent });

            while (queue.Any())
            {
                DependencyObject reference = queue.Dequeue();
                int count = VisualTreeHelper.GetChildrenCount(reference);

                for (int i = 0; i < count; i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(reference, i);
                    if (child is T children)
                        yield return children;

                    queue.Enqueue(child);
                }
            }
        }
        public static IEnumerable<T> FindLogicalChildren<T>(this DependencyObject parent) where T : DependencyObject
        {
            if (parent == null)
                throw new ArgumentNullException(nameof(parent));

            var queue = new Queue<DependencyObject>(new[] { parent });

            while (queue.Any())
            {
                var reference = queue.Dequeue();
                var children = LogicalTreeHelper.GetChildren(reference);
                var objects = children.OfType<DependencyObject>();

                foreach (var o in objects)
                {
                    if (o is T child)
                        yield return child;

                    queue.Enqueue(o);
                }
            }
        }

        public static bool IsFileLocked(FileInfo file)
        {
            try
            {
                using (FileStream stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    stream.Close();
                }
            }
            catch (IOException)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }

            //file is not locked
            return false;
        }
    }
}
