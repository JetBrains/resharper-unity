#if RESHARPER

using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

// ReSharper disable once CheckNamespace
namespace JetBrains.ReSharper.Resources.Shell
{
    // ReSharper 2018.1.2 introduced a breaking API change - caller info overloads for WriteLockCookie. Recompiling with
    // the 2018.1.2 SDK would break anyone running this on 2018.1 RTM, so we have this lovely class. We implement
    // WriteLockCookie here so that we can reroute all references in this project (around 140) to the correct implementation
    public static class WriteLockCookie
    {
        private static readonly Func<bool, IDisposable> ourOriginalCreate = null;
        private static readonly Func<bool, string, string, IDisposable> ourNewCreate = null;

        static WriteLockCookie()
        {
            var type = typeof(ReadLockCookie).Assembly.GetType("JetBrains.ReSharper.Resources.Shell.WriteLockCookie");
            var methodInfo = type.GetMethod("Create", new Type[] {typeof(bool)});
            if (methodInfo != null)
                ourOriginalCreate = BuildWriteLookCookieCreateMethodOriginal(methodInfo);
            else
            {
                methodInfo = type.GetMethod("Create", new[] {typeof(bool), typeof(string), typeof(string)});
                ourNewCreate = BuildWriteLockCookieCreateMethodNew(methodInfo);
            }
        }

        private static Func<bool, IDisposable> BuildWriteLookCookieCreateMethodOriginal(MethodInfo methodInfo)
        {
            var takeLock = Expression.Parameter(typeof(bool), "takeLock");
            var methodCall = Expression.Call(methodInfo, takeLock);
            var body = Expression.Convert(methodCall, typeof(IDisposable));
            var lambda = Expression.Lambda<Func<bool, IDisposable>>(body, takeLock);
            return lambda.Compile();
        }

        private static Func<bool, string, string, IDisposable> BuildWriteLockCookieCreateMethodNew(MethodInfo methodInfo)
        {
            var takeLock = Expression.Parameter(typeof(bool), "takeLock");
            var callerFilePath = Expression.Parameter(typeof(string), "callerFilePath");
            var callerMemberName = Expression.Parameter(typeof(string), "callerMemberName");
            var methodCall = Expression.Call(methodInfo, takeLock, callerFilePath, callerMemberName);
            var body = Expression.Convert(methodCall, typeof(IDisposable));
            var lambda =
                Expression.Lambda<Func<bool, string, string, IDisposable>>(body, takeLock, callerFilePath,
                    callerMemberName);
            return lambda.Compile();
        }

        public static IDisposable Create(bool takeLock = true, [CallerFilePath] string callerFilePath = null,
            [CallerMemberName] string callerMemberName = null)
        {
            return ourOriginalCreate != null
                ? ourOriginalCreate(takeLock)
                : ourNewCreate(takeLock, callerFilePath, callerMemberName);
        }
    }
}

#endif