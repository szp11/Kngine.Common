using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Kngine.Threading
{
    public static class AsyncValue
    {
        public static AsyncValue<TResult> Start<TResult>(Func<TResult> f)
        {
            AsyncValue<TResult> value = new AsyncValue<TResult>();

            new Action(() =>
            {
                value.returnValue = f();
                value.isCompleted = true;
                value.asyncWaitHandle.Set();
            }).BeginInvoke(null, null);

            return value;
        }

        public static AsyncValue<TResult> Start<TResult, A>(Func<A, TResult> f, A a)
        {
            AsyncValue<TResult> value = new AsyncValue<TResult>();

            new Action(() =>
            {
                value.returnValue = f(a);
                value.asyncWaitHandle.Set();
                value.isCompleted = true;
            }).BeginInvoke(null, null);

            return value;
        }

        public static AsyncValue<TResult> Start<TResult, A, B>(Func<A, B, TResult> f, A a, B b)
        {
            AsyncValue<TResult> value = new AsyncValue<TResult>();

            new Action(() =>
            {
                value.returnValue = f(a, b);
                value.asyncWaitHandle.Set();
                value.isCompleted = true;
            }).BeginInvoke(null, null);

            return value;
        }

        public static AsyncValue<TResult> Start<TResult, A, B, C>(Func<A, B, C, TResult> f, A a, B b, C c)
        {
            AsyncValue<TResult> value = new AsyncValue<TResult>();

            new Action(() =>
            {
                value.returnValue = f(a, b, c);
                value.asyncWaitHandle.Set();
                value.isCompleted = true;
            }).BeginInvoke(null, null);

            return value;
        }

        public static AsyncValue<TResult> Start<TResult, A, B, C, D>(Func<A, B, C, D, TResult> f, A a, B b, C c, D d)
        {
            AsyncValue<TResult> value = new AsyncValue<TResult>();

            new Action(() =>
            {
                value.returnValue = f(a, b, c, d);
                value.asyncWaitHandle.Set();
                value.isCompleted = true;
            }).BeginInvoke(null, null);

            return value;
        }

        public static AsyncValue<TResult> Start<TResult, A, B, C, D, E>(Func<A, B, C, D, E, TResult> f, A a, B b, C c, D d, E e)
        {
            AsyncValue<TResult> value = new AsyncValue<TResult>();

            new Action(() =>
            {
                value.returnValue = f(a, b, c, d, e);
                value.asyncWaitHandle.Set();
                value.isCompleted = true;
            }).BeginInvoke(null, null);

            return value;
        }
    }

    public sealed class AsyncValue<A> : IAsyncResult
    {
        internal bool isCompleted;
        internal A returnValue;
        internal ManualResetEventSlim asyncWaitHandle;

        /*///////////////////////////////////////////////////////////////////////////////////////////////////////*/

        // ctors
        public AsyncValue()
        {
            asyncWaitHandle = new ManualResetEventSlim(false);
        }

        /*///////////////////////////////////////////////////////////////////////////////////////////////////////*/

        // properties
        public object AsyncState
        {
            get { return null; }
        }

        public WaitHandle AsyncWaitHandle
        {
            get { return asyncWaitHandle.WaitHandle; }
        }

        public bool CompletedSynchronously
        {
            get { return false; }
        }

        public bool IsCompleted
        {
            get { return isCompleted; }
        }

        public A Join()
        {
            while (!isCompleted)
                asyncWaitHandle.WaitHandle.WaitOne();
            return returnValue;
        }

    }
}
