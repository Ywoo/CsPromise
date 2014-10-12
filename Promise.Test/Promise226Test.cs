using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Text;

using Ghost.Util;

namespace Ghost.Test
{
    /// <summary>
    ///이 클래스는 PromiseTest에 대한 테스트 클래스로서
    ///PromiseTest 단위 테스트를 모두 포함합니다.
    ///</summary>
    [TestClass()]
    public class Promise226Test {


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


        internal class Call {
            bool called_ = false;

            internal Object Result = null;

            Exception exception_ = new Exception();

            internal bool IsCalled {
                get {
                    return called_;
                }
            }

            internal Object SpyRejectReturnHandler(Exception result) {
                called_ = true;

                Result = result;
                
                return default(Object);
            }

            internal Object SpyRejectThrowsHandler(Exception result) {
                called_ = true;

                Result = result;

                throw exception_;
            }

            internal Object SpyFulfillReturnHander(Object result) {
                called_ = true;

                Result = result;

                return result;
            }

            internal Object SpyFulfillThrowsHander(Object result) {
                called_ = true;

                Result = result;

                throw exception_;
            }
        }

        // 2.2.6: `then` may be called multiple times on the same promise.
        // 2.2.6.1: If/when `promise` is fulfilled, all respective `onFulfilled` callbacks 
        // must execute in the order of their originating calls to `then`.
        [TestMethod()]
        public void MultipleFulfillHanderTest() {
            var promise = new Promise<Object>();

            var handler1 = new Call();
            var handler2 = new Call();
            var handler3 = new Call();
            var rejectHandler = new Call();

            promise.Then<Object>((Func<Object, Object>)handler1.SpyFulfillReturnHander, rejectHandler.SpyRejectReturnHandler);
            promise.Then<Object>((Func<Object, Object>)handler2.SpyFulfillReturnHander, rejectHandler.SpyRejectReturnHandler);
            promise.Then<Object>((Func<Object, Object>)handler3.SpyFulfillReturnHander, rejectHandler.SpyRejectReturnHandler);

            var resultObj = new object();

            promise.Then(
                (result) => {
                    Assert.AreEqual(resultObj, result);

                    Assert.AreEqual(resultObj, handler1.Result);
                    Assert.AreEqual(resultObj, handler2.Result);
                    Assert.AreEqual(resultObj, handler3.Result);

                    Assert.AreEqual(false, rejectHandler.IsCalled);
                }
            );

            promise.Resolve(resultObj);

            promise.Wait();
        }

        // multiple fulfillment handlers, one of which throws
        [TestMethod()]
        public void MultipleFulfillHanderOneThrowsTest() {
            var promise = new Promise<Object>();

            var handler1 = new Call();
            var handler2 = new Call();
            var handler3 = new Call();
            var rejectHandler = new Call();

            promise.Then<Object>((Func<Object, Object>)handler1.SpyFulfillReturnHander, rejectHandler.SpyRejectReturnHandler);
            promise.Then<Object>((Func<Object, Object>)handler2.SpyFulfillThrowsHander, rejectHandler.SpyRejectReturnHandler);
            promise.Then<Object>((Func<Object, Object>)handler3.SpyFulfillReturnHander, rejectHandler.SpyRejectReturnHandler);

            var resultObj = new object();

            promise.Then(
                (result) => {
                    Assert.AreEqual(resultObj, result);

                    Assert.AreEqual(resultObj, handler1.Result);
                    Assert.AreEqual(resultObj, handler2.Result);
                    Assert.AreEqual(resultObj, handler3.Result);

                    Assert.AreEqual(false, rejectHandler.IsCalled);
                }
            );

            promise.Resolve(resultObj);

            promise.Wait();
        }

        // results in multiple branching chains with their own fulfillment values
        [TestMethod()]
        public void MultipleChainedFulfillHanderTest() {
            var promise = new Promise<Object>();

            var resultObj = new object();

            var sentinel1 = new Object();
            var sentinel2 = new Object();
            var sentinel3 = new Object();

            promise.Then((result) => {
                return sentinel1;
            }).Then((result) => {
                Assert.AreEqual(sentinel1, result);
            });

            promise.Then((result) => {
                return sentinel2;
            }).Then((result) => {
                Assert.AreEqual(sentinel2, result);
            });

            promise.Then((result) => {
                return sentinel3;
            }).Then((result) => {
                Assert.AreEqual(sentinel3, result);
            });                

            promise.Resolve(resultObj);

            promise.Wait();
        }

        // "`onFulfilled` handlers are called in the original order
        [TestMethod()]
        public void MultipleFulfillHandlerCalledOriginalOrderTest() {
            var promise = new Promise<Object>();
            var callOrder = 0;

            promise.Then(
                (result) => {
                    Assert.AreEqual(0, callOrder);
                    callOrder = 1;
                }
            ).Then(
                (result) => {
                    Assert.AreEqual(1, callOrder);
                    callOrder = 2;
                }
            ).Then(
                (result) => {
                    Assert.AreEqual(2, callOrder);
                    callOrder = 3;
                }
            );

            promise.Resolve(new Object());
            promise.Wait();

            Assert.AreEqual(3, callOrder);
        }

        // even when one handler is added inside another handler
        [TestMethod()]
        public void MultipleFulfillHandlerCalledOriginalOrderEvenHandlerAddedTest() {
            var promise = new Promise<Object>();
            var callOrder = 0;

            promise.Then(
                (result) => {
                    Assert.AreEqual(0, callOrder);
                    callOrder = 1;
                }
            ).Then(
                (result) => {
                    Assert.AreEqual(1, callOrder);
                    callOrder = 2;

                    // 표준과는 다르다. 이는 Then이 바로 onFulfill을 호출하기 때문이다.
                    promise.Then((subResult) => {
                        Assert.AreEqual(2, callOrder);
                        callOrder = 3;
                    });
                }
            );

            promise.Resolve(new Object());
            promise.Wait();

            Assert.AreEqual(3, callOrder);
        }

        // 2.2.6.2: If/when `promise` is rejected, all respective `onRejected` callbacks must execute in the 
        // order of their originating calls to `then`.
        [TestMethod()]
        public void MultipleRejectHanderTest() {
            var promise = new Promise<Object>();

            var fulfillHander = new Call();
            var rejectHandler1 = new Call();
            var rejectHandler2 = new Call();
            var rejectHandler3 = new Call();

            promise.Then<Object>((Func<Object, Object>)fulfillHander.SpyFulfillReturnHander, rejectHandler1.SpyRejectReturnHandler);
            promise.Then<Object>((Func<Object, Object>)fulfillHander.SpyFulfillReturnHander, rejectHandler2.SpyRejectReturnHandler);
            promise.Then<Object>((Func<Object, Object>)fulfillHander.SpyFulfillReturnHander, rejectHandler3.SpyRejectReturnHandler);

            var resultObj = new Exception();

            promise.Then(
                (result) => {
                    Assert.AreEqual(resultObj, result);

                    Assert.AreEqual(resultObj, rejectHandler1.Result);
                    Assert.AreEqual(resultObj, rejectHandler2.Result);
                    Assert.AreEqual(resultObj, rejectHandler3.Result);

                    Assert.AreEqual(false, fulfillHander.IsCalled);
                }
            );

            promise.Reject(resultObj);

            promise.Wait();
        }

        // multiple rejection handlers, one of which throws
        [TestMethod()]
        public void MultipleRejectHanderOneThrowsTest() {
            var promise = new Promise<Object>();

            var fulfillHander = new Call();
            var rejectHandler1 = new Call();
            var rejectHandler2 = new Call();
            var rejectHandler3 = new Call();

            promise.Then<Object>((Func<Object, Object>)fulfillHander.SpyFulfillReturnHander, rejectHandler1.SpyRejectReturnHandler);
            promise.Then<Object>((Func<Object, Object>)fulfillHander.SpyFulfillThrowsHander, rejectHandler2.SpyRejectThrowsHandler);
            promise.Then<Object>((Func<Object, Object>)fulfillHander.SpyFulfillReturnHander, rejectHandler3.SpyRejectReturnHandler);

            var resultObj = new Exception();

            promise.Then(
                (result) => {
                    Assert.AreEqual(resultObj, result);

                    Assert.AreEqual(resultObj, rejectHandler1.Result);
                    Assert.AreEqual(resultObj, rejectHandler2.Result);
                    Assert.AreEqual(resultObj, rejectHandler3.Result);

                    Assert.AreEqual(false, fulfillHander.IsCalled);
                }
            );

            promise.Reject(resultObj);

            promise.Wait();
        }

        // results in multiple branching chains with their own fulfillment values
        [TestMethod()]
        public void MultipleChainedRejectHanderTest() {
            var promise = new Promise<Object>();

            var resultObj = new Exception();

            var sentinel1 = new Object();
            var sentinel2 = new Object();
            var sentinel3 = new Object();

            promise.Catch((result) => {
                return sentinel1;
            }).Then((result) => {
                Assert.AreEqual(sentinel1, result);
            });

            promise.Catch((result) => {
                return sentinel2;
            }).Then((result) => {
                Assert.AreEqual(sentinel2, result);
            });

            promise.Catch((result) => {
                return sentinel3;
            }).Then((result) => {
                Assert.AreEqual(sentinel3, result);
            });

            promise.Reject(resultObj);

            promise.Wait();
        }
        
        // onRejected` handlers are called in the original order
        [TestMethod()]
        public void MultipleRejectHandlerCalledOriginalOrderTest() {
            var promise = new Promise<Object>();
            var callOrder = 0;

            promise.Then(null, 
                (result) => {
                    Assert.AreEqual(0, callOrder);
                    callOrder = 1;
                }
            ).Then(null,
                (result) => {
                    Assert.AreEqual(1, callOrder);
                    callOrder = 2;
                }
            ).Then(null, 
                (result) => {
                    Assert.AreEqual(2, callOrder);
                    callOrder = 3;
                }
            );

            promise.Reject(new Exception());
            promise.Wait();

            Assert.AreEqual(3, callOrder);
        }

        // even when one handler is added inside another handler
        [TestMethod()]
        public void MultipleRejectHandlerCalledOriginalOrderEvenHandlerAddedTest() {
            var promise = new Promise<Object>();
            var callOrder = 0;

            promise.Then(null, 
                (result) => {
                    Assert.AreEqual(0, callOrder);
                    callOrder = 1;
                }
            ).Then(null, 
                (result) => {
                    Assert.AreEqual(1, callOrder);
                    callOrder = 2;

                    // 표준과는 다르다. 이는 Then이 바로 onFulfill을 호출하기 때문이다.
                    promise.Then(null, (subResult) => {
                        Assert.AreEqual(2, callOrder);
                        callOrder = 3;
                    });
                }
            );

            promise.Reject(new Exception());
            promise.Wait();

            Assert.AreEqual(3, callOrder);
        }
    }
}
