using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace CsPromise {
    // There is an order when c# compiler bind the method.
    // The c# compiler lookup member method and then 
    // extension method.
    //
    // for giving high priority on specialized generic method,
    // I created an extension method for a general method. 
    // and create member method for a specialized generic method.
    
    public static class PromiseExtensions {

        public static Promise<T> Catch<T>(
            this Promise thisPromise,
            Func<Exception, T> postProcessRejected) {
            return PromiseExtensions.Then<T>(thisPromise,
                null, postProcessRejected);
        }

        public static Promise<TOtherResult> Then<T, TOtherResult>(
                this Promise<T> thisPromise,
                Func<T, TOtherResult> postProcessFulfilled,
                Func<Exception, TOtherResult> postProcessRejected = null) {
            var promise = new Promise<TOtherResult>();

            thisPromise.PostProcessedDone(promise,
                Promise.GetNullOrConvertFunction(postProcessFulfilled),
                Promise.GetNullOrConvertFunction(postProcessRejected)
            );


            return promise;
        }

        public static Promise<TOtherResult> Then<TOtherResult>(
            this Promise thisPromise,
            Func<object, TOtherResult> postProcessFulfilled,
            Func<Exception, TOtherResult> postProcessRejected = null) {

            var promise = new Promise<TOtherResult>();

            thisPromise.PostProcessedDone(promise,
                Promise.GetNullOrConvertFunction(postProcessFulfilled),
                Promise.GetNullOrConvertFunction(postProcessRejected));

            return promise;
        }

        /*
          Promise extension for EAP. 
         */
        public static Promise<IAsyncResult> Begin(
            this Promise<IAsyncResult> promise,
            Func<AsyncCallback, IAsyncResult> beginFunc) {
            var asyncCallback = new AsyncCallback(result =>
                promise.Resolve(result));

            var asyncResult = beginFunc(asyncCallback);

            return promise;
        }

        public static Promise<T> CallAsync<T>(
            Func<AsyncCallback, IAsyncResult> beginFunc,
            Func<IAsyncResult, T> endFunc) {

            var promise = new Promise<IAsyncResult>();

            return PromiseExtensions.Begin(promise, beginFunc).Then(endFunc);
        }
    }
}
