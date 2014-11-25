#define PROMISE_DEBUG
#define ASYNC_THEN_CALL

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ghost.Util {
    // TODO 
    // 1. logging feature
    // 2. creating promise object if Then return IAsyncResult
    // 3. comparing the performance of TAP and Promise.

    /*
      Promise pattern for c#
     */

    public class Promise {
        public enum States {
            Pending = 0,
            Fulfilled = 1,
            Rejected = -1,
        };

        private delegate void OnFullFill(Object result);
        private delegate void OnReject(Exception e);

        private OnFullFill onFulfill_ = null;
        private OnReject onReject_ = null;

        private Object result_ = null;

        protected Object lock_ = new Object();

#if PROMISE_DEBUG
        private Promise sourcePromise_;
#endif

        protected States State {
            get;
            set;
        }

        public Promise() {
            State = States.Pending;
        }

        public Promise(Action<Action<Object>, Action<Exception>> initializer) {
            State = States.Pending;

            initializer(this.Resolve, this.Reject);

        }

        public Promise(Action<Action<Object>> initializer) {
            State = States.Pending;

            initializer(this.Resolve);
        }

        public void Done(Action<Object> onFulfilled, 
                Action<Exception> onRejected) {
            Delegate delegateToCall = null;

            lock (lock_) {
                if (onFulfilled != null) {
                    onFulfill_ += new OnFullFill(onFulfilled);
                }

                if (onRejected != null) {
                    onReject_ += new OnReject(onRejected);
                }

                delegateToCall = PopDelegateToCall();
            }

            // User delegate can take a long time,
            // we will call user delegate outside lock.
            CallDelegate(delegateToCall);
        }

        // this function should be called after locking the lock_ object.
        private Delegate PopDelegateToCall() {
            Delegate callDelegate = null;

            if (State != States.Pending) {
                if (State == States.Fulfilled) {
                    if (onFulfill_ != null) {
                        callDelegate = onFulfill_;
                    }

                }
                else if (State == States.Rejected) {
                    if (onReject_ != null) {
                        callDelegate = onReject_; 
                    }
                }

                ResetHandler();
            }

            return callDelegate;
        }

        private void CallDelegate(Delegate callDelegate) {
            if (callDelegate != null) {
#if ASYNC_THEN_CALL
                Task.Factory.StartNew(() => {
                    callDelegate.DynamicInvoke(result_);
                });
#else
                callDelegate.DynamicInvoke(result_);
#endif
            }
        }

        private void ResetHandler() {
            onFulfill_ = null;
            onReject_ = null;
        }

        // not a standard. but it might be useful for c#.
        public bool Wait() {
            if (State == States.Pending) {
                var resetEvent = new ManualResetEventSlim();

                this.Done(
                    (result) => {
                        resetEvent.Set();
                    },
                    (exception) => {
                        resetEvent.Set();
                    }
                );

                resetEvent.Wait();
            }

            return State == States.Fulfilled;
        }

        public Promise Catch(Action<Exception> postProcessRejected) {
            return Then(null, postProcessRejected);
        }

        public Promise<T> Catch<T>(
            Func<Exception, Promise<T>> postProcessRejected) {
            return Then((Func<Object, Promise<T>>)null, postProcessRejected);
        }

        public Promise Then(
            Action<Object> postProcessFulfilled,
            Action<Exception> postProcessRejected = null) {

            var postFulfilled = ConvertAsObjectFunc(postProcessFulfilled);
            var postRejected = ConvertAsObjectFunc(postProcessRejected);

            var promise = new Promise();

            PostProcessedDone(promise, postFulfilled, false, postRejected, false);

            return promise;
        }

        public Promise<TOtherResult> Then<TOtherResult>(
            Func<Object, Promise<TOtherResult>> postProcessFulfilled,
            Func<Exception, Promise<TOtherResult>> postProcessRejected = null) {

            var postFulfilled = ConvertAsObjectFunc(postProcessFulfilled);
            var postRejected = ConvertAsObjectFunc(postProcessRejected);

            var promise = new Promise<TOtherResult>();
            PostProcessedDone(promise, postFulfilled, true, postRejected, true);

            return promise;
        }

        // Action<T> => Action<Object>
        internal static Action<Object> ConvertAsObjectAction<T>(Action<T> action) {
            return action == null ? null : new Action<Object>(
                (value) => {
                    action((T)value);
                }
            );
        }

        // Action<T> => Func<T, Object>
        internal static Func<T, Object> ConvertAsObjectFunc<T>(Action<T> action) {
            return ConvertAsObjectFunc<T, T>(action);
        }

        // Action<T> => Func<TNew, Object>
        internal static Func<TNew, Object> ConvertAsObjectFunc<TOrg, TNew>(
            Action<TOrg> action) {
            return action == null ? null : new Func<TNew, Object>(
                (result) => {
                    action((TOrg)(object)result);
                    return null;
                }
            );
        }

        // Func<T1, T2> => Func<Object, Object>
        internal static Func<Object, Object> ConvertAsObjectFunc<T, TResult>(
            Func<T, TResult> func) {
            return func == null ? null : new Func<Object, Object>(
                (result) => {
                    return func((T)result);
                }
            );
        }

        
        // depend on fulfillReturn and rejectReturn flag, we
        // implement then behavior.
        // this.Done -> promise.PostProcessedResolve, promise.PostProcessedReject
        internal void PostProcessedDone(Promise promise,
            Func<Object, Object> postProcessFullfilled, 
            bool postFulfilledReturnObject,
            Func<Exception, Object> postProcessRejected, 
            bool postRejectedReturnObject) {

#if PROMISE_DEBUG
            promise.LinkSource(this);
#endif

            this.Done(
                result => promise.PostProcessedResolve(result, 
                    postProcessFullfilled,
                    postFulfilledReturnObject),
                exception => promise.PostProcessedReject(exception,
                    postProcessRejected,
                    postRejectedReturnObject)
            );
        }

        private void PostProcessedResolve(Object result,
            Func<Object, Object> postProcess,
            bool returnObject) {

            if (postProcess != null) {
                try {
                    var resultProcess = postProcess(result);

                    this.Resolve(returnObject ? resultProcess : result);
                }
                catch (Exception e) {
                    this.Reject(e);
                }
            }
            else {
                this.Resolve(result);
            }
        }

        private void PostProcessedReject(Exception exception,
            Func<Exception, Object> postProcess,
            bool returnObject) {

            if (postProcess != null) {
                try {
                    var invokeResult = postProcess(exception);

                    if (returnObject) {
                        this.Resolve(invokeResult);
                    }
                    else {
                        try {
                            this.Reject(exception);
                        }
                        catch {
                        }
                    }
                }
                catch (Exception e) {
                    this.Reject(e);
                }
            }
            else {
                this.Reject(exception);
            }
        }

        // https://github.com/promises-aplus/promises-spec#the-promise-resolution-procedure

        public void Resolve(Object obj) {
            // 1.    
            if (obj != null && obj.Equals(this)) {
                throw new InvalidOperationException("Type error. "
                + "The resolved result is same as the promise.");
            }

            // 2.
            // A.Resolve(B)
            // <=>
            // B.Done((result) -> A.Resolve(result))
            if (obj is Promise) {
#if PROMISE_DEBUG
                this.LinkSource((Promise)obj);
#endif

                ((Promise)obj).Done(
                    (result) => this.Resolve(result),
                    (exception) => this.Reject(exception)
                );

                return;
            }

            // 3. for object and function
            try {
                if (obj != null) {
                    var promise = GetPromiseFromThenAction(obj);

                    if (promise != null) {
                        Resolve(promise);

                        return;
                    }
                }

                Fulfill(obj);
            }
            catch (Exception e) {
                Reject(e);
            }
        }

        public void Reject(Exception e) {
            SetState(States.Rejected, e);
        }

        private void Fulfill(Object result) {
            SetState(States.Fulfilled, result);
        }

        private Promise GetPromiseFromThenAction(Object target) {
            if (target is Action<Action<Object>, Action<Exception>>) {
                return new Promise(
                    (Action<Action<Object>, Action<Exception>>)target
                );
            }
            else if (target is Action<Action<Object>>) {
                return new Promise((Action<Action<Object>>)target);
            }
            
            return CreateNewPromiseFromThenMethod(target);
        }

        private Promise CreateNewPromiseFromThenMethod(Object target) {
            var thenMethod = target.GetType().GetMethod("Then");

            if (thenMethod == null) {
                return null;
            }

            var parameterTypes = thenMethod.GetParameters();

            if (parameterTypes.Length == 1) {
                if (parameterTypes[0].ParameterType
                        == typeof(Action<Action<Object>>)) {
                    return new Promise(resolve
                        => thenMethod
                            .Invoke(target, new Object[] { resolve }));
                }

                if (parameterTypes[0].ParameterType
                        == typeof(Action<Action<Object>, Action<Exception>>)) {
                    return new Promise((resolve, reject)
                        => thenMethod
                            .Invoke(target, new Object[] { resolve, reject }));
                }
            }

            return null;
        }

        private void SetState(States newState, Object result) {
            Delegate delegateToCall = null;

            lock (lock_) {
                if (State == States.Pending) {
                    State = newState;

                    result_ = result;

                    delegateToCall = PopDelegateToCall();

#if PROMISE_DEBUG
                    if (sourcePromise_ == null) {
                        updateStateFrame_ =  new StackFrame(1);
                    }
#endif
                }
            }

            CallDelegate(delegateToCall);
        }

#if PROMISE_DEBUG
        private void LinkSource(Promise promise) {
            if (promise != null)
                promise.LinkSource(sourcePromise_);

            sourcePromise_ = promise;
        }

        public Promise GetSourcePromise() {
            return sourcePromise_;
        }
#endif
    }

    public class Promise<T> : Promise {
        public Promise() {
        }

        public Promise(Action<Action<T>, Action<Exception>> initializer) {
            initializer(t => this.Resolve(t), this.Reject);
        }

        // this function is implemented for type checking.
        #region then, done api for type checking.
        public Promise Then(
            Action<T> postProcessFulfilled,
            Action<Exception> postProcessRejected = null) {

            var postFulfill = ConvertAsObjectFunc<T, Object>(postProcessFulfilled);
            var postRejected = ConvertAsObjectFunc(postProcessRejected);

            var promise = new Promise();

            PostProcessedDone(promise, postFulfill, false, postRejected, false);

            return promise;
        }

        public Promise<TOtherResult> Then<TOtherResult>(
            Func<T, Promise<TOtherResult>> postProcessFulfilled,
            Func<Exception, Promise<TOtherResult>> postProcessRejected = null) {

            var postFulfill = ConvertAsObjectFunc(postProcessFulfilled);
            var postRejected = ConvertAsObjectFunc(postProcessRejected);

            var promise = new Promise<TOtherResult>();

            PostProcessedDone(promise, postFulfill, true, postRejected, true);

            return promise;
        }

        
        public void Done(Action<T> onFulfilled, Action<Exception> onRejected) {
            Done(ConvertAsObjectAction(onFulfilled), onRejected);
        }

        #endregion
    }

    // There is an order when c# compiler bind the method.
    // The c# compiler lookup member method and then 
    // extension method.
    // for giving high priority on specialized generic method.
    // I created an extension method for a general method. 
    // and create member method for a specialized generic method.
    public static class PromiseUtil {
        public static Promise<T> Catch<T>(
            this Promise thisPromise,
            Func<Exception, T> postProcessRejected) {
            return PromiseUtil.Then<T>(thisPromise,
                null, postProcessRejected);
        }

        public static Promise<TOtherResult> Then<T, TOtherResult>(
                this Promise<T> thisPromise,
                Func<T, TOtherResult> postProcessFulfilled,
                Func<Exception, TOtherResult> postProcessRejected = null) {

            var postFulfill = Promise.ConvertAsObjectFunc(postProcessFulfilled);
            var postRejected = Promise.ConvertAsObjectFunc(postProcessRejected);

            var promise = new Promise<TOtherResult>();

            thisPromise.PostProcessedDone(promise, postFulfill, true, 
                postRejected, true);

            return promise;
        }

        public static Promise<TOtherResult> Then<TOtherResult>(
            this Promise thisPromise,
            Func<Object, TOtherResult> postProcessFulfilled,
            Func<Exception, TOtherResult> postProcessRejected = null) {

            var postFulfilled = Promise.ConvertAsObjectFunc(postProcessFulfilled);
            var postRejected = Promise.ConvertAsObjectFunc(postProcessRejected);

            var promise = new Promise<TOtherResult>();

            thisPromise.PostProcessedDone(promise, postFulfilled, true, 
                postRejected, true);

            return promise;
        }

    }
}
