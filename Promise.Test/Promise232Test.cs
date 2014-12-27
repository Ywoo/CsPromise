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
    public class Promise232Test {


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

        static public void TestPromiseResolution<T>(T resolveObj, Func<object> factory, Action<Promise<T>> test) {
            PromiseTest.TestFulfilled<T>(resolveObj, 
                internalPromise => {
                    internalPromise.Then(result => factory());

                    test(internalPromise);
                }
            );

            PromiseTest.TestRejected<T>(new Exception(),
                internalPromise => {
                    internalPromise.Catch(
                        (exception) => {
                            return factory();
                        }
                    );

                    test(internalPromise);
                }
            );
        }

        // 2.3.2: If `x` is a promise, adopt its state
        // 2.3.2.1: If `x` is pending, `promise` must remain pending until `x` is fulfilled or rejected.
        [TestMethod()]
        public void PromiseRemainPendingUntilXIsResolvedTest() {
            TestPromiseResolution<object>(new object(),
                () => new Promise<object>(),
                (promise) => {
                    promise.Then(
                        (result) => {
                            Assert.Fail("fulfilled called!");
                        },
                        (exception) => {
                            Assert.Fail("rejected called!");
                        }
                    );

                    Task.Factory.StartNew(() => {
                        Thread.Sleep(100);
                    });
                }
            );
        }

        // 2.3.2.2: If/when `x` is fulfilled, fulfill `promise` with the same value.
        [TestMethod()]
        public void FulfillPromiseWithSameValueIfXIsFulfilledTest() {
            var result = new object();

            // `x` is already-fulfilled
            TestPromiseResolution<object>(new object(),
                () => {
                    var promise = new Promise<object>();

                    promise.Resolve(result);
                    
                    return promise;
                },
                (promise) => {
                    promise.Then((thenResult) => {
                        Assert.AreEqual(result, thenResult);
                    });
                }
            );

            // `x` is eventually-fulfilled

            TestPromiseResolution<object>(new object(),
                () => {
                    var promise = new Promise<object>();

                    Task.Factory.StartNew(() => {
                        Thread.Sleep(100);
                        promise.Resolve(result);
                    });

                    return promise;
                },
                (promise) => {
                    promise.Then((thenResult) => {
                        Assert.AreEqual(result, thenResult);
                    });
                }
            );

        }

        // 2.3.2.3: If/when `x` is rejected, reject `promise` with the same reason.
        [TestMethod()]
        public void RejectPromiseWithSameValueIfXIsRejectedTest() {
            var throwException = new Exception();

            // `x` is already-rejected
            TestPromiseResolution<object>(new object(),
                () => {
                    var promise = new Promise<object>();

                    promise.Reject(throwException);

                    return promise;
                },
                (promise) => {
                    promise.Then(
                        null,
                        (thenResult) => {
                            Assert.AreEqual(throwException, thenResult);
                        }
                    );
                }
            );

            // `x` is eventually-rejected

            TestPromiseResolution<object>(new object(),
                () => {
                    var promise = new Promise<object>();

                    Task.Factory.StartNew(() => {
                        Thread.Sleep(100);
                        promise.Reject(throwException);
                    });

                    return promise;
                },
                (promise) => {
                    promise.Then(
                        null,
                        (thenResult) => {
                            Assert.AreEqual(throwException, thenResult);
                        }
                    );
                }
            );
        }
    }
}
