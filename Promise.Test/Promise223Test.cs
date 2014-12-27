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
    // 2.2.3: If `onRejected` is a function,
    public class Promise223Test : BasePromiseTest {
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
        // 2.2.3.1: it must be called after `promise` is rejected, with `promise`’s rejection reason as its 
        [TestMethod()]
        public void MustBeCalledAfterPromiseIsRejectedWithPromisesRejectionTest() {
            var sentinel = new Exception();
            var called = false;
            TestRejected(sentinel, (promise) => {
                promise.Then(null, (reason) => {
                    Assert.AreEqual(reason, sentinel);
                    called = true;
                });
            });

            SetTimeout(() => Assert.IsTrue(called), 50);
        }

        // 2.2.2.2: it must not be called before `promise` is fulfilled
        [TestMethod()]
        public void MustNotBeCalledBeforePromiseIsRejectedDelayedTest() {
            var d = Deferred();
            var isRejected = false;
            var called = false;

            d.Promise.Then(null, reason => {
                Assert.AreEqual(true, isRejected);
                called = true;
            });

            SetTimeout(() => {
                // In original test, isRejected is set after d.Resolve.
                // but promise.cs does not support asynchronouse call.
                // so, isRejected should be set before call d.Resolve.
                isRejected = true;
                d.Reject(null);
            }, 50);

            SetTimeout(() => Assert.IsTrue(called), 100);
        }

        [TestMethod()]
        public void MustNotBeCalledBeforePromiseIsNeverRejectedTest() {
            var d = Deferred();
            var onRejectedCalled = false;
            var called = false;

            d.Promise.Then(null, reason => {
                onRejectedCalled = true;
                called = true;
            });

            SetTimeout(() => {
                Assert.AreEqual(false, onRejectedCalled);
                called = true;
            }, 50);

            SetTimeout(() => Assert.IsTrue(called), 100);
        }

        // 2.2.3.3: it must not be called more than once.
        [TestMethod()]
        public void MustNotBeCalledMoreThanOnceAlreadyRejectedTest() {
            var timesCalled = 0;
            var called = false;

            Rejected(null).Then(null, (reason) => {
                Assert.AreEqual(1, ++timesCalled);

                called = true;
            });

            SetTimeout(() => Assert.IsTrue(called), 50);
        }

        [TestMethod()]
        public void MustNotBeCalledMoreThanOnceTryingToRejectAPendingPromiseMoreThenOnceImmediately() {
            var d = Deferred();
            var timesCalled = 0;
            
            d.Promise.Then(null, reason => {
                Assert.AreEqual(1, ++timesCalled);
            });

            d.Reject(null);
            d.Reject(null);

            SetTimeout(() => Assert.AreEqual(1, timesCalled), 50);
        }

        [TestMethod()]
        public void MustNotBeCalledMoreThanOnceTryingToRejectAPendingPromiseMoreThenOnceDelayed() {
            var d = Deferred();
            var timesCalled = 0;

            d.Promise.Then(null, reason => {
                Assert.AreEqual(1, ++timesCalled);
            });

            SetTimeout(() => {
                d.Reject(null);
                d.Reject(null);
            }, 50);

            SetTimeout(() => Assert.AreEqual(1, timesCalled), 100);
        }

        [TestMethod()]
        public void MustNotBeCalledMoreThanOnceTryingToRejectAPendingPromiseMoreThenOnceImmediatelyAndDelayed() {
            var d = Deferred();
            var timesCalled = 0;

            d.Promise.Then(null, reason => {
                Assert.AreEqual(1, ++timesCalled);
            });

            d.Reject(null);
            
            SetTimeout(() => {
                d.Reject(null);
            }, 50);

            SetTimeout(() => Assert.AreEqual(1, timesCalled), 100);
        }

        [TestMethod()]
        public void MustNotBeCalledMoreThanOnceTryingToRejectAPendingPromiseMoreThenOnceMultiple() {
            var d = Deferred();
            var timesCalled = new int[] { 0, 0, 0 };

            d.Promise.Then(null, reason => {
                Assert.AreEqual(++timesCalled[0], 1);
            });

            SetTimeout(() => {
                d.Promise.Then(null, reason => {
                    Assert.AreEqual(++timesCalled[1], 1);
                });
            }, 50);

            SetTimeout(() => {
                d.Promise.Then(null, reason => {
                    Assert.AreEqual(++timesCalled[2], 1);
                });
            }, 100);

            SetTimeout(() => d.Reject(null), 150);

            SetTimeout(() => 
                CollectionAssert.AreEqual(new int[] { 1, 1, 1 }, timesCalled), 
                200);
        }

        [TestMethod()]
        public void MustNotBeCalledMoreThanOnceWhenThenIsInterleavedWithRejection() {
            var d = Deferred();
            var timesCalled = new int[] { 0, 0 };

            d.Promise.Then(null, reason => {
                Assert.AreEqual(++timesCalled[0], 1);
            });

            d.Reject(null);

            d.Promise.Then(null, result => {
                Assert.AreEqual(++timesCalled[1], 1);
            });

            SetTimeout(() =>
                CollectionAssert.AreEqual(new int[] { 1, 1 }, timesCalled),
                100);
        }
    }
}

