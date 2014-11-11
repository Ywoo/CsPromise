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
    public class PromiseTest {


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

            Assert.AreEqual(false, promise.Wait());

            Assert.AreEqual("Echo", firstResult);
            Assert.AreEqual("Echo Test", secondResult);
            Assert.AreEqual("test for catch", message);
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
            var promise = new Promise<Object>();

            CheckSettled(promise, () => {
                promise.Resolve(new Object());
                promise.Reject(new Exception());
            }, true);
        }

        [TestMethod()]
        public void DelayedFullfillThenRejectTest() {
            var promise = new Promise<Object>();

            CheckSettled(promise, () => {
                Task.Factory.StartNew(() => {
                    promise.Resolve(new Object());
                    promise.Reject(new Exception());
                });
            }, true);
        }

        [TestMethod()]
        public void FullfillThenDelayedRejectTest() {
            var promise = new Promise<Object>();

            CheckSettled(promise, () => {
                promise.Resolve(new Object());    

                Task.Factory.StartNew(() => {
                    promise.Reject(new Exception());
                });
            }, true);
        }

        // 2.1.3.1: When rejected, a promise: must not transition to any other state.
        [TestMethod()]
        public void RejectThenImmediatelyFulfillTest() {
            var promise = new Promise<Object>();

            CheckSettled(promise, () => {
                promise.Reject(new Exception());
                promise.Resolve(new Object());
            }, false);
        }

        [TestMethod()]
        public void DelayedRejectThenFulfillTest() {
            var promise = new Promise<Object>();

            CheckSettled(promise, () => {
                Task.Factory.StartNew(() => {
                    promise.Reject(new Exception());
                    promise.Resolve(new Object());
                });
            }, false);
        }

        [TestMethod()]
        public void RejectThenDelayedFulfillTest() {
            var promise = new Promise<Object>();

            CheckSettled(promise, () => {
                promise.Reject(new Exception());

                Task.Factory.StartNew(() => {
                    promise.Resolve(new Object());
                });
            }, false);
        }

        Promise<Object> GetRejectedPromise<T>(Exception e) {
            var promise = new Promise<Object>();
            promise.Reject(e);

            return promise;
        }

        Promise<Object> GetResolvedPromise<T>(T result) {
            var promise = new Promise<Object>();
            promise.Resolve(result);

            return promise;
        }

        // 2.2.1: Both `onFulfilled` and `onRejected` are optional arguments.
        // 2.2.1.1: If `onFulfilled` is not a function, it must be ignored.
        [TestMethod()]
        public void OnFullfilledIgnoreTest() {
            var promise = GetRejectedPromise<Object>(new Exception());
            var rejectedCalled = false;

            promise.Then(null, (e) => {
                rejectedCalled = true;
            });

            CheckSettled(promise, null, false);
            Assert.IsTrue(rejectedCalled);
        }

        [TestMethod()]
        public void OnFullfilledIgnoreChainedOffTest() {
            var promise = GetRejectedPromise<Object>(new Exception());
            var rejectedCalled = false;

            promise.Then(
                (result) => {}
            ).Then(null, (e) => {
                rejectedCalled = true;
            });

            CheckSettled(promise, null, false);
            Assert.IsTrue(rejectedCalled);
        }

        // 2.2.1.2: If `onRejected` is not a function, it must be ignored.
        [TestMethod()]
        public void OnRejectIgnoreTest() {
            var promise = GetResolvedPromise<Object>(new Exception());
            var fulfilledCalled = false;

            promise.Then((result) => {
                fulfilledCalled = true;
            }, null);

            CheckSettled(promise, null, true);
            Assert.IsTrue(fulfilledCalled);
        }

        [TestMethod()]
        public void OnRejectIgnoreChainedOffTest() {
            var promise = GetResolvedPromise<Object>(new Exception());
            var fulfilledCalled = false;

            promise.Then(
                null, (e) => {}
            ).Then(
                (result) => {
                    fulfilledCalled = true;
                }, 
                null);

            CheckSettled(promise, null, true);
            Assert.IsTrue(fulfilledCalled);
        }

        // "2.2.2: If `onFulfilled` is a function,"
        // 2.2.2.1: it must be called after `promise` is fulfilled, with `promise`’s fulfillment value as its first argument.
        [TestMethod()]
        public void OnFulfillTest() {
            var promise = new Promise<Object>();

            var objResult = new Object();

            promise.Then((result) => {
                Assert.AreEqual(objResult, result);
            });

            promise.Resolve(objResult);

            CheckSettled(promise, null, true);
        }

        // 2.2.2.2: it must not be called before `promise` is fulfilled
        [TestMethod()]
        public void CallAfterPromiseIsFulfilledTest() {
            var promise = new Promise<Object>();

            var objResult = new Object();
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
            var promise = new Promise<Object>();

            var objResult = new Object();
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

            var promise = GetResolvedPromise<Object>(new Object()).Then(
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
            var resultObj = new Object();

            var promise = new Promise<Object>();

            promise.Then(
                (result) => {
                    Assert.AreEqual(1, ++timesCalled);
                    Assert.AreEqual(resultObj, result);
                }
            );

            promise.Resolve(resultObj);
            promise.Resolve(new Object());

            CheckSettled(promise, null, true);
        }

        // trying to fulfill a pending promise more than once, delayed
        [TestMethod()]
        public void NotMoreCallOnFulfillMoreDelayedResolveTest() {
            var timesCalled = 0;
            var resultObj = new Object();

            var promise = new Promise<Object>();

            promise.Then(
                (result) => {
                    Assert.AreEqual(1, ++timesCalled);
                    Assert.AreEqual(resultObj, result);
                }
            );

            var task = Task.Factory.StartNew(() => {
                promise.Resolve(resultObj);
                promise.Resolve(new Object());
            });

            CheckSettled(promise, null, true);

            task.Wait();
        }

        // trying to fulfill a pending promise more than once, immediately then delayed
        [TestMethod()]
        public void NotMoreCallOnFulfillMoreOnceDelayedResolveTest() {
            var timesCalled = 0;
            var resultObj = new Object();

            var promise = new Promise<Object>();

            promise.Then(
                (result) => {
                    Assert.AreEqual(1, ++timesCalled);
                    Assert.AreEqual(resultObj, result);
                }
            );

            promise.Resolve(resultObj);

            var task = Task.Factory.StartNew(() => {
                promise.Resolve(new Object());
            });

            CheckSettled(promise, null, true);

            task.Wait();
        }

        // when multiple `then` calls are made, spaced apart in time
        [TestMethod()]
        public void MultipleThenCallSparcedApartInTime() {
            var timesCalled = new int[] { 0, 0, 0 };
            var resultObj = new Object();
            var tasks = new List<Task>();

            var promise = new Promise<Object>();

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
            var resultObj = new Object();

            var promise = new Promise<Object>();

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
            var promise = new Promise<Object>();

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
            var promise = new Promise<Object>();

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
            var promise = new Promise<Object>();

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

            var promise = GetRejectedPromise<Object>(new Exception())
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

            var promise = new Promise<Object>();

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

            var promise = new Promise<Object>();

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

            var promise = new Promise<Object>();

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

            var promise = new Promise<Object>();

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

            var promise = new Promise<Object>();

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

        // "2.2.4: `onFulfilled` or `onRejected` must not be called until the execution context stack contains only
        // platform code.
        //
        // we did not support this.
    }
}
