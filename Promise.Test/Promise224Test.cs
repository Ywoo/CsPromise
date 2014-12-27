using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Text;

using CsPromise;

namespace CsPromise.Test
{
    /// <summary>
    ///이 클래스는 PromiseTest에 대한 테스트 클래스로서
    ///PromiseTest 단위 테스트를 모두 포함합니다.
    ///</summary>
    [TestClass()]
    // 2.2.4: `onFulfilled` or `onRejected` must not be called until the execution context stack contains only 
    public class Promise224Test : BasePromiseTest {
        private TestContext testContextInstance;

        /// <summary>
        ///현재 테스트 실행에 대한 정보 및 기능을
        ///제공하는 테스트 컨텍스트를 가져오거나 설정합니다.
        ///</summary>
        public TestContext TestContext {
            get {
                return testContextInstance;
            }
            set {
                testContextInstance = value;
            }
        }

        #region 추가 테스트 특성
        // 
        //테스트를 작성할 때 다음 추가 특성을 사용할 수 있습니다.
        //
        //ClassInitialize를 사용하여 클래스의 첫 번째 테스트를 실행하기 전에 코드를 실행합니다.
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        //ClassCleanup을 사용하여 클래스의 테스트를 모두 실행한 후에 코드를 실행합니다.
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //TestInitialize를 사용하여 각 테스트를 실행하기 전에 코드를 실행합니다.
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
        //
        //TestCleanup을 사용하여 각 테스트를 실행한 후에 코드를 실행합니다.
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion
        // `then` returns before the promise becomes fulfilled or rejected
        [TestMethod()]
        public void ThenReturnBeforeThePromiseBecomesFulfilledOrRejectedTest() {
            TestFulfilled(dummy_, promise => {
                var thenHasReturned = false;
                var called = false;

                promise.Then((result) => {
                    Assert.AreEqual(true, thenHasReturned);
                    called = true;
                });

                thenHasReturned = true;

                SetTimeout(() => Assert.IsTrue(called), 100);
            });

            TestRejected(null, promise => {
                var thenHasReturned = false;
                var called = false;

                promise.Then(null, reason => {
                    Assert.AreEqual(true, thenHasReturned);

                    called = true;
                });

                thenHasReturned = true;

                SetTimeout(() => Assert.IsTrue(called), 100);
            });
        }

        [TestMethod()]
        public void CleanStackExecutionOrderingTestsWhenOnFulfilledBeforeFulfilled() {
            var d = Deferred();
            var onFulfilledCalled = false;
            
            d.Promise.Then(result => {
                onFulfilledCalled = true;
            });

            d.Resolve(dummy_);

            Assert.AreEqual(false, onFulfilledCalled);

            SetTimeout(() => Assert.IsTrue(onFulfilledCalled), 100);
        }

        [TestMethod()]
        public void CleanStackExecutionOrderingTestsWhenOnFulfilledAfterFulfilled() {
            var d = Deferred();
            var onFulfilledCalled = false;

            d.Resolve(dummy_);

            d.Promise.Then(result => {
                onFulfilledCalled = true;
            });

            Assert.AreEqual(false, onFulfilledCalled);

            SetTimeout(() => Assert.IsTrue(onFulfilledCalled), 100);
        }

        [TestMethod()]
        public void CleanStackExecutionOrderingTestsWhenOnFulfilledIsAdded() {
            var promise = Resolved(dummy_);
            var firstOnFulfilledFinished = false;

            var called = false;
            promise.Then(result => {
                promise.Then(subResult => {
                    Assert.AreEqual(true, firstOnFulfilledFinished);
                    called = true;
                });
                firstOnFulfilledFinished = true;
            });

            SetTimeout(() => Assert.IsTrue(called), 100);
        }

        [TestMethod()]
        public void CleanStackExecutionOrderingTestsWhenOnFulfilledIsAddedInsideAnOnRejected() {
            var promise = Rejected(null);
            var promise2 = Resolved(null);

            var firstOnRejectedFinished = false;
            var called = false;
            
            promise.Then(null, reason => {
                promise2.Then(result => {
                    Assert.AreEqual(true, firstOnRejectedFinished);
                    called = true;
                });
                firstOnRejectedFinished = true;
            });

            SetTimeout(() => Assert.IsTrue(called), 50);
        }

        [TestMethod()]
        public void CleanStackExecutionOrderingTestsWhenThePromiseIsFulfilledAsynchronously() {
            var d = Deferred();
            var firstStackFinished = false;
            var called = false;

            SetTimeout(() => {
                d.Resolve(dummy_);
                firstStackFinished = true;
            }, 0);

            d.Promise.Then(result => {
                Assert.AreEqual(true, firstStackFinished);
                called = true;
            });

            SetTimeout(() => Assert.IsTrue(called), 100);
        }

        [TestMethod()]
        public void CleanStackExecutionOrderingTestsWhenOnFulfilledBeforeRejected() {
            var d = Deferred();
            var onRejectedCalled = false;

            d.Promise.Then(null, reason => {
                onRejectedCalled = true;
            });

            d.Reject(null);

            Assert.AreEqual(false, onRejectedCalled);

            SetTimeout(() => Assert.IsTrue(onRejectedCalled), 100);
        }

        [TestMethod()]
        public void CleanStackExecutionOrderingTestsWhenOnFulfilledAfterRejected() {
            var d = Deferred();
            var onRejectedCalled = false;

            d.Reject(null);

            d.Promise.Then(null, reason => {
                onRejectedCalled = true;
            });

            Assert.AreEqual(false, onRejectedCalled);

            SetTimeout(() => Assert.IsTrue(onRejectedCalled), 100);
        }

        [TestMethod()]
        public void CleanStackExecutionOrderingTestsWhenOnRejectedIsAdded() {
            var promise = Resolved(dummy_);
            var promise2 = Rejected(null);

            var firstOnFulfilledFinished = false;

            var called = false;
            promise.Then(result => {
                promise2.Then(null, reason => {
                    Assert.AreEqual(true, firstOnFulfilledFinished);
                    called = true;
                });

                firstOnFulfilledFinished = true;
            });

            SetTimeout(() => Assert.IsTrue(called), 100);
        }

        [TestMethod()]
        public void CleanStackExecutionOrderingTestsWhenOnRejectedIsAddedInsideAnOnRejected() {
            var promise = Rejected(null);
            
            var firstOnRejectedFinished = false;
            var called = false;

            promise.Then(null, reason => {
                promise.Then(null, subReason => {
                    Assert.AreEqual(true, firstOnRejectedFinished);
                    called = true;
                });
                firstOnRejectedFinished = true;
            });

            SetTimeout(() => Assert.IsTrue(called), 50);
        }

        [TestMethod()]
        public void CleanStackExecutionOrderingTestsWhenThePromiseIsRejectedAsynchronously() {
            var d = Deferred();
            var firstStackFinished = false;
            var called = false;

            SetTimeout(() => {
                d.Reject(null);
                firstStackFinished = true;
            }, 0);

            d.Promise.Then(null, reason => {
                Assert.AreEqual(true, firstStackFinished);
                called = true;
            });

            SetTimeout(() => Assert.IsTrue(called), 100);
        }

    }
}

