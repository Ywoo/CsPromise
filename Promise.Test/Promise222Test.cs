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
    // 2.2.1: Both `onFulfilled` and `onRejected` are optional arguments
    public class Promise222Test : BasePromiseTest {
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
        // 2.2.2: If `onFulfilled` is a function,
        // 2.2.2.1: it must be called after `promise` is fulfilled, with `promise`’s fulfillment value as its 
        [TestMethod()]
        public void MustBeCalledAfterPromiseIsFulfilledWithPromisesFulfillmentTest() {
            var sentinel = new object();
            var called = false;
            TestFulfilled(sentinel, (promise) => {
                promise.Then((result) => {
                    Assert.AreEqual(result, sentinel);
                    called = true;
                });
            });

            SetTimeout(() => Assert.IsTrue(called), 50);
        }

        // 2.2.2.2: it must not be called before `promise` is fulfilled
        [TestMethod()]
        public void MustNotBeCalledBeforePromiseIsFulfilledDelayedTest() {
            var d = Deferred();
            var isFulfilled = false;
            var called = false;

            d.Promise.Then(result => {
                Assert.AreEqual(true, isFulfilled);
                called = true;
            });

            SetTimeout(() => {
                // In original test, isFulfilled set after d.Resolve.
                // but promise.cs does not support asynchronouse call.
                // so, isFullfilled should be set before call d.Resolve.
                isFulfilled = true;
                d.Resolve(dummy_);
            }, 50);

            SetTimeout(() => Assert.IsTrue(called), 100);
        }

        [TestMethod()]
        public void MustNotBeCalledBeforePromiseIsNeverFulfilledTest() {
            var d = Deferred();
            var onFulfilledCalled = false;
            var called = false;

            d.Promise.Then(result => {
                onFulfilledCalled = true;
                called = true;
            });

            SetTimeout(() => {
                Assert.AreEqual(false, onFulfilledCalled);
                called = true;
            }, 150);

            SetTimeout(() => Assert.IsTrue(called), 200);
        }

        // 2.2.2.3: it must not be called more than once.
        [TestMethod()]
        public void MustNotBeCalledMoreThanOnceAlreadyFulfilledTest() {
            var timesCalled = 0;
            var called = false;

            Resolved(dummy_).Then((result) => {
                Assert.AreEqual(1, ++timesCalled);

                called = true;
            });

            SetTimeout(() => Assert.IsTrue(called), 50);
        }

        [TestMethod()]
        public void MustNotBeCalledMoreThanOnceTryingToFulfillAPendingPromiseMoreThenOnceImmediately() {
            var d = Deferred();
            var timesCalled = 0;
            
            d.Promise.Then(result => {
                Assert.AreEqual(1, ++timesCalled);
            });

            d.Resolve(dummy_);
            d.Resolve(dummy_);

            SetTimeout(() => Assert.AreEqual(1, timesCalled), 50);
        }

        [TestMethod()]
        public void MustNotBeCalledMoreThanOnceTryingToFulfillAPendingPromiseMoreThenOnceDelayed() {
            var d = Deferred();
            var timesCalled = 0;

            d.Promise.Then(result => {
                Assert.AreEqual(1, ++timesCalled);
            });

            SetTimeout(() => {
                d.Resolve(dummy_);
                d.Resolve(dummy_);
            }, 50);

            SetTimeout(() => Assert.AreEqual(1, timesCalled), 100);
        }

        [TestMethod()]
        public void MustNotBeCalledMoreThanOnceTryingToFulfillAPendingPromiseMoreThenOnceImmediatelyAndDelayed() {
            var d = Deferred();
            var timesCalled = 0;

            d.Promise.Then(result => {
                Assert.AreEqual(1, ++timesCalled);
            });

            d.Resolve(dummy_);
            
            SetTimeout(() => {
                d.Resolve(dummy_);
            }, 50);

            SetTimeout(() => Assert.AreEqual(1, timesCalled), 100);
        }

        [TestMethod()]
        public void MustNotBeCalledMoreThanOnceTryingToFulfillAPendingPromiseMoreThenOnceMultiple() {
            var d = Deferred();
            var timesCalled = new int[] { 0, 0, 0 };

            d.Promise.Then(result => {
                Assert.AreEqual(++timesCalled[0], 1);
            });

            SetTimeout(() => {
                d.Promise.Then(result => {
                    Assert.AreEqual(++timesCalled[1], 1);
                });
            }, 50);

            SetTimeout(() => {
                d.Promise.Then(result => {
                    Assert.AreEqual(++timesCalled[2], 1);
                });
            }, 100);

            SetTimeout(() => d.Resolve(dummy_), 150);

            SetTimeout(() => 
                CollectionAssert.AreEqual(new int[] { 1, 1, 1 }, timesCalled), 
                200);
        }

        [TestMethod()]
        public void MustNotBeCalledMoreThanOnceWhenThenIsInterleavedWithFulfillment() {
            var d = Deferred();
            var timesCalled = new int[] { 0, 0 };

            d.Promise.Then(result => {
                Assert.AreEqual(++timesCalled[0], 1);
            });

            d.Resolve(dummy_);

            d.Promise.Then(result => {
                Assert.AreEqual(++timesCalled[1], 1);
            });

            SetTimeout(() =>
                CollectionAssert.AreEqual(new int[] { 1, 1 }, timesCalled),
                100);
        }
    }
}

