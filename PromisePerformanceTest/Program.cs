using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using CsPromise;

namespace PromisePerformanceTest {
    class Program {
        private static Promise<IPHostEntry> GetPromiseForGetHostEntry(String host) {
            return PromiseExtensions.CallAsync<IPHostEntry>(
                (callback) => Dns.BeginGetHostEntry(host, callback, null),
                (result) => Dns.EndGetHostEntry(result)
            );
        }

        static void Main(string[] args) {
            String host = "www.google.com";
            var resolvedCount = 0;
            var count = 50000;

            Enumerable.Repeat(host, count)
                .Select(item => {
                    return GetPromiseForGetHostEntry(item)
                        .Then(result =>
                            Interlocked.Increment(ref resolvedCount)
                        );
                })
                .ToList()
                .ForEach(item => item.Wait());
        }
    }
}
