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
    public class Promise227Test : BasePromiseTest {


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


        // 2.2.7: `then` must return a promise: `promise2 = promise1.then(onFulfilled, onRejected)
        // trivial

        // 2.2.7.2: If either `onFulfilled` or `onRejected` throws an exception `e`, `promise2` must be rejected
        // with `e` as the reason.
        [TestMethod()]
        public void ThrowExceptionInHandlerMakeRejectedTest() {
            var expectedException = new Exception();
            Exception sentException = null;

            PromiseTest.TestFulfilled<Object>(new Object(), promise => {
                promise.Then(
                    result => {
                        throw expectedException;
                    }
                ).Then(
                    null,
                    (exception) => {
                        sentException = exception;
                    }
                );
            });

            SetTimeout(() =>
                Assert.AreEqual(expectedException, sentException), 50);

            sentException = null;

            PromiseTest.TestRejected<Object>(new Exception(), promise => {
                promise.Then(null,
                    exception => {
                        throw expectedException;
                    }
                ).Then(
                    null,
                    (exception) => {
                        sentException = exception;
                    }
                );
            });

            SetTimeout(() =>
                Assert.AreEqual(expectedException, sentException), 50);
        }

        // 2.2.7.3: If `onFulfilled` is not a function and `promise1` is fulfilled, `promise2` must be fulfilled
        // with the same value.
        [TestMethod()]
        public void SecondOnfulfillCallTest() {
            var expectedResult = new Object();
            object sentResult = null;

            PromiseTest.TestFulfilled<Object>(expectedResult, promise => {
                promise.Then(
                    null        
                ).Then(
                    (result) => {
                        sentResult = result;
                    }
                );
            });

            SetTimeout(() =>
                Assert.AreEqual(expectedResult, sentResult), 50);
        }

        // 2.2.7.4: If `onRejected` is not a function and `promise1` is rejected, `promise2` must be rejected 
        // with the same reason.
        [TestMethod()]
        public void SecondOnRejectCallTest() {
            var expectedException = new Exception();
            Exception sentException = null;

            PromiseTest.TestRejected<Object>(expectedException, promise => {
                promise.Then(
                    null,
                    null
                ).Then(
                    null,
                    (exception) => {
                        sentException = exception;
                    }
                );
            });

            SetTimeout(() => Assert.AreEqual(expectedException, sentException),
                50);
        }
    }
}
