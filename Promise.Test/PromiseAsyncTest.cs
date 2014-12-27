using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.Net;
using System.Linq;
using System.Net.Sockets;
using System.Diagnostics;

using CsPromise;

namespace CsPromise.Test
{
    /// <summary>
    ///이 클래스는 PromiseTest에 대한 테스트 클래스로서
    ///PromiseTest 단위 테스트를 모두 포함합니다.
    ///</summary>
    [TestClass()]
    public class PromiseAsyncTest : BasePromiseTest {


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

        private Promise<IPHostEntry> GetPromiseForGetHostEntry(String host) {
            return PromiseExtensions.CallAsync<IPHostEntry>(
                (callback) => Dns.BeginGetHostEntry(host, callback,
                    null),
                (result) => Dns.EndGetHostEntry(result));
        }

        [TestMethod()]
        public void CallAsyncTest() {
            String host = "www.google.com";
            var called = false;

            GetPromiseForGetHostEntry(host)
                .Then(result => {
                    Assert.AreEqual(host, result.HostName);

                    Debug.WriteLine(String.Format("resolved ip message {0}",
                        result.AddressList[0].ToString()));

                    called = true;
                })
                .Catch(e => {
                    Assert.Fail();
                });

            SetTimeout(() => Assert.IsTrue(called), 1000);
        }

        [TestMethod()]
        public void CallAsyncExceptionTest() {
            String host = "test!host?name";
            var called = false;

            GetPromiseForGetHostEntry(host)
                .Then(result => {
                    Assert.Fail();
                })
                .Catch(e => {
                    Debug.WriteLine(String.Format("exception message {0}",
                        e.Message));
                    called = true;
                });

            SetTimeout(() => Assert.IsTrue(called), 1000);
        }

        [TestMethod()]
        public void CallAsyncStressTest() {
            String host = "www.google.com";
            var resolvedCount = 0;
            var count = 50000;

            Enumerable.Repeat(host, count)
                .Select(item => {
                    return GetPromiseForGetHostEntry(item)
                        .Then(result =>
                            Interlocked.Increment(ref resolvedCount)
                        );
                }).ToList().ForEach(item => item.Wait());

            SetTimeout(() => Assert.AreEqual(count, resolvedCount), 50);
        }

        private Task<IPHostEntry> GetHostEntryTask(String host) {
            var tcs = new TaskCompletionSource<IPHostEntry>();
                
            Dns.BeginGetHostEntry(host, ar => {
                try {
                    tcs.SetResult(Dns.EndGetHostEntry(ar));
                }
                catch (Exception e) {
                    tcs.SetException(e);
                }
            }, null);

            return tcs.Task;
        }

        [TestMethod()]
        public void CallTest() {
            String host = "www.google.com";
            var resolvedCount = 0;
            var count = 50000;

            Enumerable.Repeat(host, count)
                .Select(item => GetHostEntryTask(host))
                .ToList()
                .ForEach(item => {
                    Interlocked.Increment(ref resolvedCount);
                    item.Wait();
                });
        }
    }
}
