﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
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
    public class PromiseTest : BasePromiseTest{


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

        public static void TestFulfilled<T>(T result, Action<Promise<T>> internalProcess) {
            var promise = new Promise<T>();

            internalProcess(promise);

            promise.Resolve(result);

            promise.Wait();
        }

        public static void TestRejected<T>(Exception e, Action<Promise<T>> internalProcess) {
            var promise = new Promise<T>();

            internalProcess(promise);

            promise.Reject(e);

            promise.Wait();
        }


        [TestMethod()]
        public void ThenAsyncResolveTest() {
            Action<String> resolveAction = null;
            String firstResult = null;
            String secondResult = null;
            String message = null;

            var promise = new Promise<String>(
                (resolve, reject) => {
                    resolveAction = resolve;
                }
            ).Then(
                (result) => {
                    firstResult = result;
                    Assert.IsNull(secondResult);
                    return new StringBuilder(result).Append(" Test");
                     
                }
            ).Then(
                (result) => {
                    secondResult = result.ToString();

                    Assert.AreEqual("Echo Test", result.ToString());
                    Assert.IsNotNull(firstResult);
                    Assert.IsNull(message);
                    throw new InvalidOperationException("test for catch");
                }
            ).Catch(
                (e) => {
                    Assert.IsNotNull(firstResult);
                    Assert.IsNotNull(secondResult);

                    Assert.AreEqual("test for catch", e.Message);
                    message = e.Message;
                }
            );

            resolveAction("Echo");

            SetTimeout(() => {
                Assert.AreEqual(false, promise.Wait());

                Assert.AreEqual("Echo", firstResult);
                Assert.AreEqual("Echo Test", secondResult);
                Assert.AreEqual("test for catch", message);
            }, 50);   
        }

        [TestMethod()]
        public void MultiplePromiseResolveTest() {
            new Promise(
                (resolve, reject) => {
                    Task.Factory.StartNew(() => {
                        resolve("First");
                    });
                }
            ).Then(
                (result) => {
                    return new Promise<String>(
                        (resolve, reject) => {
                            Task.Factory.StartNew(() => {
                                resolve(result + "Second");
                            });
                        }
                    );
                }
            ).Then(
                (result) => {
                    Assert.AreEqual("FirstSecond", result);
                }
            ).Wait();
        }

        [TestMethod()]
        public void ThenSyncResolveTest() {
            String firstResult = null;
            String secondResult = null;
            String message = null;

            var promise = new Promise<String>(
                (resolve, reject) => {
                    resolve("Echo");
                }
            ).Then(
                (result) => {
                    firstResult = result;
                    Assert.IsNull(secondResult);
                    return new StringBuilder(result).Append(" Test");

                }
            ).Then(
                (result) => {
                    secondResult = result.ToString();

                    Assert.AreEqual("Echo Test", result.ToString());
                    Assert.IsNotNull(firstResult);
                    Assert.IsNull(message);
                    throw new InvalidOperationException("test for catch");
                }
            ).Catch<String>(
                (e) => {
                    Assert.IsNotNull(firstResult);
                    Assert.IsNotNull(secondResult);

                    Assert.AreEqual("test for catch", e.Message);
                    message = e.Message;

                    return null;
                }
            );

            Assert.AreEqual(true, promise.Wait());

            Assert.AreEqual("Echo", firstResult);
            Assert.AreEqual("Echo Test", secondResult);
            Assert.AreEqual("test for catch", message);
        }
        
        private void CheckSettled(Promise promise, Action resolveAction, bool fulfilled) {
            var onFulfilledCalled = false;

            var waitedPromise = promise.Then(
                (result) => {
                    onFulfilledCalled = true;
                },
                (exception) => {
                    Assert.AreEqual(onFulfilledCalled, false);
                }
            );

            if (resolveAction != null) {
                resolveAction();
            }

            Assert.AreEqual(waitedPromise.Wait(), fulfilled);

            Assert.AreEqual(onFulfilledCalled, fulfilled);
        }

        // 2.1.2.1: When fulfilled, a promise: must not transition to any other state.
        [TestMethod()]
        public void FullfillThenImmediatelyRejectTest() {
            var promise = new Promise<object>();

            CheckSettled(promise, () => {
                promise.Resolve(new object());
                promise.Reject(new Exception());
            }, true);
        }

        [TestMethod()]
        public void DelayedFullfillThenRejectTest() {
            var promise = new Promise<object>();

            CheckSettled(promise, () => {
                Task.Factory.StartNew(() => {
                    promise.Resolve(new object());
                    promise.Reject(new Exception());
                });
            }, true);
        }

        [TestMethod()]
        public void FullfillThenDelayedRejectTest() {
            var promise = new Promise<object>();

            CheckSettled(promise, () => {
                promise.Resolve(new object());    

                Task.Factory.StartNew(() => {
                    promise.Reject(new Exception());
                });
            }, true);
        }

        // 2.1.3.1: When rejected, a promise: must not transition to any other state.
        [TestMethod()]
        public void RejectThenImmediatelyFulfillTest() {
            var promise = new Promise<object>();

            CheckSettled(promise, () => {
                promise.Reject(new Exception());
                promise.Resolve(new object());
            }, false);
        }

        [TestMethod()]
        public void DelayedRejectThenFulfillTest() {
            var promise = new Promise<object>();

            CheckSettled(promise, () => {
                Task.Factory.StartNew(() => {
                    promise.Reject(new Exception());
                    promise.Resolve(new object());
                });
            }, false);
        }

        [TestMethod()]
        public void RejectThenDelayedFulfillTest() {
            var promise = new Promise<object>();

            CheckSettled(promise, () => {
                promise.Reject(new Exception());

                Task.Factory.StartNew(() => {
                    promise.Resolve(new object());
                });
            }, false);
        }

        Promise<object> GetRejectedPromise<T>(Exception e) {
            var promise = new Promise<object>();
            promise.Reject(e);

            return promise;
        }

        Promise<object> GetResolvedPromise<T>(T result) {
            var promise = new Promise<object>();
            promise.Resolve(result);

            return promise;
        }

        // 2.2.1: Both `onFulfilled` and `onRejected` are optional arguments.
        // 2.2.1.1: If `onFulfilled` is not a function, it must be ignored.
        [TestMethod()]
        public void OnFullfilledIgnoreTest() {
            var promise = GetRejectedPromise<object>(new Exception());
            var rejectedCalled = false;

            promise.Then(null, (e) => {
                rejectedCalled = true;
            });

            CheckSettled(promise, null, false);
            SetTimeout(() => Assert.IsTrue(rejectedCalled), 50);
        }

        [TestMethod()]
        public void OnFullfilledIgnoreChainedOffTest() {
            var promise = GetRejectedPromise<object>(new Exception());
            var rejectedCalled = false;

            promise.Then(
                (result) => {}
            ).Then(null, (e) => {
                rejectedCalled = true;
            });

            CheckSettled(promise, null, false);
            SetTimeout(() => Assert.IsTrue(rejectedCalled), 50);
        }

        // 2.2.1.2: If `onRejected` is not a function, it must be ignored.
        [TestMethod()]
        public void OnRejectIgnoreTest() {
            var promise = GetResolvedPromise<object>(new Exception());
            var fulfilledCalled = false;

            promise.Then((result) => {
                fulfilledCalled = true;
            }, null).Wait();

            CheckSettled(promise, null, true);
            Assert.IsTrue(fulfilledCalled);
        }

        [TestMethod()]
        public void OnRejectIgnoreChainedOffTest() {
            var promise = GetResolvedPromise<object>(new Exception());
            var fulfilledCalled = false;

            promise.Then(
                null, (e) => {
                }
            ).Then(
                (result) => {
                    fulfilledCalled = true;
                },
                null).Wait();

            CheckSettled(promise, null, true);
            Assert.IsTrue(fulfilledCalled);
        }

        // "2.2.2: If `onFulfilled` is a function,"
        // 2.2.2.1: it must be called after `promise` is fulfilled, with `promise`’s fulfillment value as its first argument.
        [TestMethod()]
        public void OnFulfillTest() {
            var promise = new Promise<object>();

            var objResult = new object();

            promise.Then((result) => {
                Assert.AreEqual(objResult, result);
            });

            promise.Resolve(objResult);

            CheckSettled(promise, null, true);
        }

        // 2.2.2.2: it must not be called before `promise` is fulfilled
        [TestMethod()]
        public void CallAfterPromiseIsFulfilledTest() {
            var promise = new Promise<object>();

            var objResult = new object();
            var isFulfilled = false;

            promise.Then((result) => {
                Assert.AreEqual(isFulfilled, true);
            });

            var task = Task.Factory.StartNew(() => {
                Thread.Sleep(50);
                isFulfilled = true;
                promise.Resolve(objResult);
            });

            CheckSettled(promise, null, true);
            task.Wait();
        }

        [TestMethod()]
        public void NotCallBeforePromiseIsFulfilledTest() {
            var promise = new Promise<object>();

            var objResult = new object();
            var onFulfilled = false;

            promise.Then((result) => {
                onFulfilled = true;
            });

            var task = Task.Factory.StartNew(() => {
                Thread.Sleep(100);
                Assert.AreEqual(false, onFulfilled);
            });

            task.Wait();
        }

        // 2.2.2.3: it must not be called more than once.
        // already fullfill
        [TestMethod()]
        public void NotMoreCallOnFulfillAlreadyFulfilledTest() {
            var timesCalled = 0;

            var promise = GetResolvedPromise<object>(new object()).Then(
                (result) => {
                    Assert.AreEqual(1, ++timesCalled);
                }
            );

            CheckSettled(promise, null, true);
        }

        // trying to fulfill a pending promise more than once, immediately
        [TestMethod()]
        public void NotMoreCallOnFulfillMoreResolveTest() {
            var timesCalled = 0;
            var resultObj = new object();

            var promise = new Promise<object>();

            promise.Then(
                (result) => {
                    Assert.AreEqual(1, ++timesCalled);
                    Assert.AreEqual(resultObj, result);
                }
            );

            promise.Resolve(resultObj);
            promise.Resolve(new object());

            CheckSettled(promise, null, true);
        }

        // trying to fulfill a pending promise more than once, delayed
        [TestMethod()]
        public void NotMoreCallOnFulfillMoreDelayedResolveTest() {
            var timesCalled = 0;
            var resultObj = new object();

            var promise = new Promise<object>();

            promise.Then(
                (result) => {
                    Assert.AreEqual(1, ++timesCalled);
                    Assert.AreEqual(resultObj, result);
                }
            );

            var task = Task.Factory.StartNew(() => {
                promise.Resolve(resultObj);
                promise.Resolve(new object());
            });

            CheckSettled(promise, null, true);

            task.Wait();
        }

        // trying to fulfill a pending promise more than once, immediately then delayed
        [TestMethod()]
        public void NotMoreCallOnFulfillMoreOnceDelayedResolveTest() {
            var timesCalled = 0;
            var resultObj = new object();

            var promise = new Promise<object>();

            promise.Then(
                (result) => {
                    Assert.AreEqual(1, ++timesCalled);
                    Assert.AreEqual(resultObj, result);
                }
            );

            promise.Resolve(resultObj);

            var task = Task.Factory.StartNew(() => {
                promise.Resolve(new object());
            });

            CheckSettled(promise, null, true);

            task.Wait();
        }

        // when multiple `then` calls are made, spaced apart in time
        [TestMethod()]
        public void MultipleThenCallSparcedApartInTime() {
            var timesCalled = new int[] { 0, 0, 0 };
            var resultObj = new object();
            var tasks = new List<Task>();

            var promise = new Promise<object>();

            promise.Then(
                (result) => {
                    Assert.AreEqual(1, ++timesCalled[0]);
                    Assert.AreEqual(resultObj, result);
                }
            );

            tasks.Add(Task.Factory.StartNew(() => {
                Thread.Sleep(50);
                promise.Then(
                    (result) => {
                        Assert.AreEqual(1, ++timesCalled[1]);
                        Assert.AreEqual(resultObj, result);
                    }
                );
            }));

            tasks.Add(Task.Factory.StartNew(() => {
                Thread.Sleep(100);
                promise.Then(
                    (result) => {
                        Assert.AreEqual(1, ++timesCalled[2]);
                        Assert.AreEqual(resultObj, result);
                    }
                );
            }));

            tasks.Add(Task.Factory.StartNew(() => {
                Thread.Sleep(150);
                promise.Resolve(resultObj);
            }));

            tasks.ForEach(
                (task) => {
                    task.Wait();
                }
            );
        }

        // when `then` is interleaved with fulfillment
        [TestMethod()]
        public void MultipleThenCallInterleaved() {
            var timesCalled = new int[] { 0, 0, 0 };
            var resultObj = new object();

            var promise = new Promise<object>();

            promise.Then(
                (result) => {
                    Assert.AreEqual(1, ++timesCalled[0]);
                    Assert.AreEqual(resultObj, result);
                }
            );

            promise.Resolve(resultObj);

            promise.Then(
                (result) => {
                    Assert.AreEqual(1, ++timesCalled[0]);
                    Assert.AreEqual(resultObj, result);
                }
            );
        }

        // "2.2.3: If `onRejected` is a function,"
        // "2.2.3.1: it must be called after `promise` is rejected, with `promise`’s rejection reason as its first argument.
        [TestMethod()]
        public void OnRejectedTest() {
            var promise = new Promise<object>();

            var exception = new Exception();

            promise.Catch((result) => {
                Assert.AreEqual(exception, result);
            });

            promise.Reject(exception);

            CheckSettled(promise, null, false);
        }

        // 2.2.3.2: it must not be called before `promise` is rejected
        [TestMethod()]
        public void CallAfterPromiseIsRejectedTest() {
            var promise = new Promise<object>();

            var exception = new Exception();
            var isRejected = false;

            promise.Catch((result) => {
                Assert.AreEqual(exception, result);
                Assert.AreEqual(isRejected, true);
            });

            var task = Task.Factory.StartNew(() => {
                Thread.Sleep(50);
                isRejected = true;
                promise.Reject(exception);
            });

            CheckSettled(promise, null, false);
            task.Wait();
        }

        [TestMethod()]
        public void NotCallBeforePromiseIsRejectedTest() {
            var promise = new Promise<object>();

            var exception = new Exception();
            var onRejected = false;

            promise.Catch((result) => {
                onRejected = true;
            });

            var task = Task.Factory.StartNew(() => {
                Thread.Sleep(100);
                Assert.AreEqual(false, onRejected);
            });

            task.Wait();
        }

        // 2.2.3.3: it must not be called more than once.
        // already-rejected
        [TestMethod()]
        public void NotMoreCallOnRejectedAlreadyRejectedTest() {
            var timesCalled = 0;

            var promise = GetRejectedPromise<object>(new Exception())
                .Then(null,
                    (result) => {
                        Assert.AreEqual(1, ++timesCalled);
                    }
                );

            CheckSettled(promise, null, false);
        }

        // trying to reject a pending promise more than once, immediately
        [TestMethod()]
        public void NotMoreCallOnRejectedMoreRejectedTest() {
            var timesCalled = 0;
            var resultObj = new Exception();

            var promise = new Promise<object>();

            promise.Catch(
                (result) => {
                    Assert.AreEqual(1, ++timesCalled);
                    Assert.AreEqual(resultObj, result);
                }
            );

            promise.Reject(resultObj);
            promise.Reject(new Exception());

            CheckSettled(promise, null, false);
        }

        // trying to reject a pending promise more than once, delayed
        [TestMethod()]
        public void NotMoreCallOnRejectedMoreDelayedRejectTest() {
            var timesCalled = 0;
            var resultObj = new Exception();

            var promise = new Promise<object>();

            promise.Catch(
                (result) => {
                    Assert.AreEqual(1, ++timesCalled);
                    Assert.AreEqual(resultObj, result);
                }
            );

            var task = Task.Factory.StartNew(() => {
                promise.Reject(resultObj);
                promise.Reject(new Exception());
            });

            CheckSettled(promise, null, false);

            task.Wait();
        }

        // trying to reject a pending promise more than once, immediately then delayed
        [TestMethod()]
        public void NotMoreCallOnRejectedMoreOnceDelayedRejectTest() {
            var timesCalled = 0;
            var resultObj = new Exception();

            var promise = new Promise<object>();

            promise.Catch(
                (result) => {
                    Assert.AreEqual(1, ++timesCalled);
                    Assert.AreEqual(resultObj, result);
                }
            );

            promise.Reject(resultObj);

            var task = Task.Factory.StartNew(() => {
                promise.Reject(new Exception());
            });

            CheckSettled(promise, null, false);

            task.Wait();
        }

        // "when multiple `then` calls are made, spaced apart in time
        [TestMethod()]
        public void MultipleThenCallSparcedApartInTimeForRejectTest() {
            var timesCalled = new int[] { 0, 0, 0 };
            var resultObj = new Exception();
            var tasks = new List<Task>();

            var promise = new Promise<object>();

            promise.Then(null, 
                (result) => {
                    Assert.AreEqual(1, ++timesCalled[0]);
                    Assert.AreEqual(resultObj, result);
                }
            );

            tasks.Add(Task.Factory.StartNew(() => {
                Thread.Sleep(50);
                promise.Then(null, 
                    (result) => {
                        Assert.AreEqual(1, ++timesCalled[1]);
                        Assert.AreEqual(resultObj, result);
                    }
                );
            }));

            tasks.Add(Task.Factory.StartNew(() => {
                Thread.Sleep(100);
                promise.Then(null, 
                    (result) => {
                        Assert.AreEqual(1, ++timesCalled[2]);
                        Assert.AreEqual(resultObj, result);
                    }
                );
            }));

            tasks.Add(Task.Factory.StartNew(() => {
                Thread.Sleep(150);
                promise.Reject(resultObj);
            }));

            tasks.ForEach(
                (task) => {
                    task.Wait();
                }
            );
        }

        // when `then` is interleaved with fulfillment
        [TestMethod()]
        public void MultipleThenCallInterleavedWithRejectionTest() {
            var timesCalled = new int[] { 0, 0, 0 };
            var resultObj = new Exception();

            var promise = new Promise<object>();

            promise.Then(null, 
                (result) => {
                    Assert.AreEqual(1, ++timesCalled[0]);
                    Assert.AreEqual(resultObj, result);
                }
            );

            promise.Reject(resultObj);

            promise.Then(null, 
                (result) => {
                    Assert.AreEqual(1, ++timesCalled[0]);
                    Assert.AreEqual(resultObj, result);
                }
            );
        }

        [TestMethod()]
        public void ResolveActionTest() {
            var thenWasCalled = false;
            var resultObj = new object();

            var promise = GetResolvedPromise<object>(resultObj);

            var promiseReturn1 = promise.Then((result) => {
                return (object) new Action<Action<object>>((resolved) => {
                    resolved(1);
                });
            });

            Assert.IsTrue(promiseReturn1.Wait());

            promiseReturn1.Then(result => {
                Assert.AreEqual(1, result);
                thenWasCalled = true;
            });

            SetTimeout(() => {
                Assert.IsTrue(thenWasCalled);
            }, 50);
        }

        [TestMethod()]
        public void ResolveActionTest2() {
            var thenWasCalled = false;
            var resultObj = new object();

            var promise = GetResolvedPromise<object>(resultObj);

            var promiseReturn1 = promise.Then((result) => {
                return (object)new Action<Action<object>, Action<Exception>>(
                    (resolved, rejected) => {
                        resolved(1);
                    }
                );
            });

            Assert.IsTrue(promiseReturn1.Wait());

            promiseReturn1.Then(result => {
                Assert.AreEqual(1, result);
                thenWasCalled = true;
            });

            SetTimeout(() => {
                Assert.IsTrue(thenWasCalled);
            }, 50);
        }

        class ThenObject {
            public void Then(Action<Object> resolved) {
                resolved(1);
            }
        }

        [TestMethod()]
        public void ResolveObjectTest() {
            var thenWasCalled = false;
            var resultObj = new object();

            var promiseReturn1 = GetResolvedPromise<object>(new ThenObject());

            Assert.IsTrue(promiseReturn1.Wait());

            promiseReturn1.Then(result => {
                Assert.AreEqual(1, result);
                thenWasCalled = true;
            });

            SetTimeout(() => {
                Assert.IsTrue(thenWasCalled);
            }, 50);
        }

        class ThenObject2 {
            public void Then(Action<Object> resolved, Action<Exception> rejected) {
                resolved(1);
            }
        }

        [TestMethod()]
        public void ResolveObjectTest2() {
            var thenWasCalled = false;
            var resultObj = new object();

            var promiseReturn1 = GetResolvedPromise<object>(new ThenObject2());

            Assert.IsTrue(promiseReturn1.Wait());

            promiseReturn1.Then(result => {
                Assert.AreEqual(1, result);
                thenWasCalled = true;
            });

            SetTimeout(() => {
                Assert.IsTrue(thenWasCalled);
            }, 50);
        }

        class InvalidThenObject {
            public void Then(Action<Object> resolved, Action<Object> rejected) {
                resolved(1);
            }
        }

        [TestMethod()]
        public void ResolveInvalidThenObjectTest() {
            var thenWasCalled = false;
            var resultObj = new InvalidThenObject();

            var promise = GetResolvedPromise<object>(1);

            var promiseReturn1 = promise.Then((result) => {
                return resultObj;
            });

            Assert.IsTrue(promiseReturn1.Wait());

            promiseReturn1.Then(result => {
                Assert.AreEqual(resultObj, result);
                thenWasCalled = true;
            });

            SetTimeout(() => {
                Assert.IsTrue(thenWasCalled);
            }, 50);
        }

        [TestMethod()]
        public void ThenGenericTest() {
            var thenWasCalled = false;
            var resultObj = new object();

            var promise = GetResolvedPromise<object>(2);

            var promiseReturn1 = promise.Then((result) => {
                return new Promise<int>((resolve, rejected) => {
                    resolve(1);
                });
            });

            Assert.IsTrue(promiseReturn1.Wait());

            promiseReturn1.Then(result => {
                Assert.AreEqual(1, result);
                thenWasCalled = true;
            });

            SetTimeout(() => {
                Assert.IsTrue(thenWasCalled);
            }, 50);
        }

        [TestMethod()]
        public void ThenActionNullTest() {
            var thenWasCalled = false;
            var resultObj = new object();

            var promise = GetResolvedPromise<object>(2);

            var promiseReturn1 = promise.Then(null);

            Assert.IsTrue(promiseReturn1.Wait());

            promiseReturn1.Then(result => {
                Assert.AreEqual(2, result);
                thenWasCalled = true;
            });

            SetTimeout(() => {
                Assert.IsTrue(thenWasCalled);
            }, 50);
        }

        [TestMethod()]
        public void ThenFuncNullResolveTest() {
            var thenWasCalled = false;
            var resultObj = new object();

            var promise = GetResolvedPromise<object>(2);

            var promiseReturn1 = promise.Then(null, exception => {
                return 0;
            });

            Assert.IsTrue(promiseReturn1.Wait());

            promiseReturn1.Then(result => {
                Assert.AreEqual(2, result);
                thenWasCalled = true;
            });

            SetTimeout(() => {
                Assert.IsTrue(thenWasCalled);
            }, 50);
        }

        [TestMethod()]
        public void ThenFuncNullRejectedTest() {
            var called = false;
            var resultObj = new Exception();

            var promise = GetRejectedPromise<object>(resultObj);

            var promiseReturn1 = promise.Then((result) => 1, null);

            Assert.IsFalse(promiseReturn1.Wait());

            promiseReturn1.Then(null, exception => {
                Assert.AreEqual(resultObj, exception);
                called = true;
            });

            SetTimeout(() => {
                Assert.IsTrue(called);
            }, 50);
        }

        [TestMethod()]
        public void DoneGenericResolvedTest() {
            var called = false;
            var resultObj = new object();

            var promise = GetResolvedPromise<object>(2);

            promise.Done(result => {
                Assert.AreEqual(1, result);
                called = true;
            }, exception => {
            });

            SetTimeout(() => {
                Assert.IsTrue(called);
            }, 100);
        }

        [TestMethod()]
        public void DoneGenericRejectedTest() {
            var called = false;
            var rejectedException = new Exception();

            var promise = GetRejectedPromise<object>(rejectedException );

            promise.Done(result => {
                
            }, exception => {
                Assert.AreEqual(rejectedException, exception);
                called = true;
            });

            SetTimeout(() => {
                Assert.IsTrue(called);
            }, 50);
        }

    }
}
