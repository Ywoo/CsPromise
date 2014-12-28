#define ASYNC_THEN_CALL

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

//
// Promise pattern for c#
//
namespace CsPromise {
    // TODO 
    // 1. logging feature for debugging.
    // 2. comparing the performance of TAP and Promise.


    public class Promise {
        public enum States {
            Pending = 0,
            Fulfilled = 1,
            Rejected = -1,
        };

        private delegate void OnCallback(object param1);

        private OnCallback onFulfill_ = null;
        private OnCallback onReject_ = null;

        private object result_ = null;

        protected object lock_ = new object();

        protected States State {
            get;
            set;
        }

        public Promise() {
            State = States.Pending;
        }

        public Promise(Action<Action<object>, Action<Exception>> initializer) {
            State = States.Pending;

            initializer(this.Resolve, this.Reject);

        }

        public Promise(Action<Action<object>> initializer) {
            State = States.Pending;

            initializer(this.Resolve);
        }

        public void Done(Action<object> onFulfilled, 
                Action<object> onRejected) {
            DoneImpl(onFulfilled, onRejected);
        }

        internal static Func<object, object> GetNullOrConvertFunction<TArg, TResult>(
            Func<TArg, TResult> func) {
            if (func == null)
                return null;

            return (result) => func((TArg)result);
        }

        internal static Action<object> GetNullOrConvertFunction<TArg>(
            Action<TArg> func) {
            if (func == null)
                return null;

            return (result) => func((TArg)result);
        }

        internal void DoneImpl(Action<object> onFulfilled,
            Action<object> onRejected) {

            OnCallback handlerToCall = null;

            lock (lock_) {
                if (onFulfilled != null) {
                    onFulfill_ += new OnCallback(onFulfilled);
                }

                if (onRejected != null) {
                    onReject_ += new OnCallback(onRejected);
                }

                handlerToCall = PopDelegateToCall();
            }

            // User delegate can take a long time,
            // we will call user delegate outside lock.
            CallDelegate(handlerToCall);

        }

        // this function should be called after locking the lock_ object.
        private OnCallback PopDelegateToCall() {
            OnCallback delegateToCall = null;

            if (State != States.Pending) {
                if (State == States.Fulfilled) {
                    if (onFulfill_ != null) {
                        delegateToCall = onFulfill_;
                    }
                }
                else if (State == States.Rejected) {
                    if (onReject_ != null) {
                        delegateToCall = onReject_;
                    }
                }

                onFulfill_ = null;
                onReject_ = null;
            }

            return delegateToCall;
        }

        private void CallDelegate(OnCallback delegateToCall) {
            if (delegateToCall != null) {
#if ASYNC_THEN_CALL
                Task.Factory.StartNew(() => {
                    delegateToCall(result_);
                });
#else
                delegateToCall(result_);

#endif
            }
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
            return Then((Action<object>)null, postProcessRejected);
        }

        public Promise<T> Catch<T>(
            Func<Exception, Promise<T>> postProcessRejected) {
            return Then((Func<object, Promise<T>>)null, postProcessRejected);
        }

        public Promise Then(
            Action<object> postProcessFulfilled,
            Action<Exception> postProcessRejected = null) {

            var promise = new Promise();

            PostProcessedDone(promise, postProcessFulfilled, 
                postProcessRejected);
                
            return promise;
        }

        public Promise<TOtherResult> Then<TOtherResult>(
            Func<object, Promise<TOtherResult>> postProcessFulfilled,
            Func<Exception, Promise<TOtherResult>> postProcessRejected = null) {

            var promise = new Promise<TOtherResult>();
            PostProcessedDone(promise, postProcessFulfilled, 
                postProcessRejected);

            return promise;
        }

        internal void PostProcessedDone(Promise promise,
            Func<object, object> postProcessFullfilled, 
            Func<Exception, object> postProcessRejected) {

            this.Done(
                result => promise.PostProcessedResolve(result, 
                    postProcessFullfilled),
                exception => promise.PostProcessedReject((Exception)exception,
                    postProcessRejected)
            );
        }

        internal void PostProcessedDone(Promise promise,
            Action<object> postProcessFullfilled,
            Action<Exception> postProcessRejected) {

            this.Done(
                result => promise.PostProcessedResolve(
                    result, postProcessFullfilled),
                exception => promise.PostProcessedReject(
                    (Exception)exception, postProcessRejected)
            );
        }

        private void PostProcessedResolve(object result,
            Func<object, object> postProcess) {
            if (postProcess != null) {
                try {
                    this.Resolve(postProcess(result));
                }
                catch (Exception e) {
                    this.Reject(e);
                }
            }
            else {
                this.Resolve(result);
            }
        }

        private void PostProcessedResolve(object result,
            Action<object> postProcess) {
            if (postProcess != null) {
                try {
                    postProcess(result);

                    this.Resolve(result);
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
            Func<Exception, object> postProcess) {

            if (postProcess != null) {
                try {
                    var invokeResult = postProcess(exception);

                    this.Resolve(invokeResult);
                }
                catch (Exception e) {
                    this.Reject(e);
                }
            }
            else {
                this.Reject(exception);
            }
        }

        private void PostProcessedReject(Exception exception,
            Action<Exception> postProcess) {

            if (postProcess != null) {
                try {
                    postProcess(exception);

                    try {
                        this.Reject(exception);
                    }
                    catch {
                        // TODO log the exception for debugging.
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

        public void Resolve(object obj) {
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

        public void Reject(object e) {
            Debug.Assert(e == null || e is Exception);
            SetState(States.Rejected, e);
        }

        private void Fulfill(object result) {
            SetState(States.Fulfilled, result);
        }

        private Promise GetPromiseFromThenAction(object target) {
            if (target is Action<Action<object>, Action<Exception>>) {
                return new Promise(
                    (Action<Action<object>, Action<Exception>>)target
                );
            }
            else if (target is Action<Action<object>>) {
                return new Promise((Action<Action<object>>)target);
            }
            
            return CreateNewPromiseFromThenMethod(target);
        }

        private Promise CreateNewPromiseFromThenMethod(object target) {
            var thenMethod = target.GetType().GetMethod("Then");

            if (thenMethod == null) {
                return null;
            }

            var parameterTypes = thenMethod.GetParameters();

            if (parameterTypes.Length == 1) {
                if (parameterTypes[0].ParameterType
                        == typeof(Action<object>)) {
                    return new Promise(resolve
                        => thenMethod
                            .Invoke(target, new object[] { resolve }));
                }
            }

            if(parameterTypes.Length == 2) {
                if (parameterTypes[0].ParameterType
                        == typeof(Action<object>) 
                    && parameterTypes[1].ParameterType
                        == typeof(Action<Exception>)) {
                    return new Promise((resolve, reject)
                        => thenMethod
                            .Invoke(target, new object[] { resolve, reject }));
                }
            }

            return null;
        }

        private void SetState(States newState, object result) {
            OnCallback delegateToCall = null;

            lock (lock_) {
                if (State == States.Pending) {
                    State = newState;

                    result_ = result;

                    delegateToCall = PopDelegateToCall();
                }
            }

            CallDelegate(delegateToCall);
        }
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

            var promise = new Promise();

            PostProcessedDone(promise, 
                GetNullOrConvertFunction(postProcessFulfilled), 
                GetNullOrConvertFunction(postProcessRejected));

            return promise;
        }

        public Promise<TOtherResult> Then<TOtherResult>(
            Func<T, Promise<TOtherResult>> postProcessFulfilled,
            Func<Exception, Promise<TOtherResult>> postProcessRejected = null) {

            var promise = new Promise<TOtherResult>();

            PostProcessedDone(promise,
                GetNullOrConvertFunction(postProcessFulfilled),
                GetNullOrConvertFunction(postProcessRejected));

            return promise;
        }

        public void Done(Action<T> onFulfilled, Action<Exception> onRejected) {
            DoneImpl((result) => onFulfilled((T)result), 
                (exception) => onRejected((Exception) exception));
        }

        #endregion
    }
}
