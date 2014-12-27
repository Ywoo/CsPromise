using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Threading;
// using System.Threading.Tasks;
using System.Text;
using System.Timers;

using CsPromise;

namespace CsPromise.Test
{
    public class BasePromiseTest {
        public object dummy_ = new object();
        public bool done_ = false;

        public SemaphoreSlim semaphore_ = null;
        public int activeTimer_ = 0;

        [TestInitialize()]
        public void TestInitialize()
        {
            // semaphore_ = new SemaphoreSlim(0);
        }
        [TestCleanup()]
        public void TestCleanup()
        {
            // semaphore_.Wait();
            WaitTimersAreFinished();
        }

        public class DeferredItem {
            public Promise Promise;
            public Action<object> Resolve;
            public Action<Exception> Reject;
        }

        public void TestFulfilled(object value, Action<Promise> test) {
            new FulfilledTester(value, test).Test();
        }

        public void TestRejected(Exception reason, Action<Promise> test) {
            new RejectedTester(reason, test).Test();
        }

        public Promise Resolved(object value) {
            var promise = new Promise();

            promise.Resolve(value);
            return promise;
        }

        public DeferredItem Deferred() {
            var promise = new Promise();
            return new DeferredItem() {
                Promise = promise,
                Resolve = obj => promise.Resolve(obj),
                Reject = exception => promise.Reject(exception)
            };
        }

        public Promise Rejected(Exception reason) {
            var promise = new Promise();
            if (reason == null) {
                reason = new Exception("promise rejected exception");
            }
            promise.Reject(reason);

            return promise;
        }

        public void Done() {
            // semaphore_.Release();
        }

        public void SetTimeout(Action callback, int timeout) {
            // for testing set time, mstest does not support 
            // done() function for finishing test. So, we will
            // wait for finishing timeout callback.

            if (timeout < 10) {
                timeout = 10;
            }

            var timer = new System.Timers.Timer(timeout);

            timer.AutoReset = false;
            
            RegisterTimer(timer);

            timer.Elapsed += (sender, e) => callback();

            timer.Start();
        }

        public void RegisterTimer(System.Timers.Timer timer) {
            if(Interlocked.Increment(ref activeTimer_) == 1) {
                semaphore_ = new SemaphoreSlim(0);
            }

            timer.Elapsed += (sender, e) => {
                if(Interlocked.Decrement(ref activeTimer_) == 0) {
                    semaphore_.Release();
                }
            };
        }

        public void WaitTimersAreFinished() {
            if(semaphore_ != null) {
                semaphore_.Wait();
            }
        }
    }

    public class FulfilledTester : BasePromiseTest {
        private object value_;
        private Action<Promise> test_;
        public FulfilledTester(object value,
                Action<Promise> test) {
            value_ = value;
            test_ = test;
        }

        public void Test() {
            AlreadyFulfilled();
            ImmediatelyFulfilled();
            EventuallyFulfilled();
        }

        private void AlreadyFulfilled() {
            test_(Resolved(value_));
        }

        private void ImmediatelyFulfilled() {
            var d = Deferred();

            test_(d.Promise);
            d.Resolve(value_);
        }

        private void EventuallyFulfilled() {
            var d = Deferred();

            test_(d.Promise);
            SetTimeout(() => d.Resolve(value_), 50);
        }
    }

    public class RejectedTester : BasePromiseTest {
        private Exception reason_;
        private Action<Promise> test_;

        public RejectedTester(Exception reason,
                Action<Promise> test) {
            reason_ = reason;
            test_ = test;
        }

        public void Test() {
            AlreadyRejected();
            ImmediatelyRejected();
            EventuallyRejected();
        }

        private void AlreadyRejected() {
            test_(Rejected(reason_));
        }

        private void ImmediatelyRejected() {
            var d = Deferred();

            test_(d.Promise);
            d.Reject(reason_);
        }

        private void EventuallyRejected() {
            var d = Deferred();

            test_(d.Promise);
            SetTimeout(() => d.Reject(reason_), 50);
        }
    }

}
