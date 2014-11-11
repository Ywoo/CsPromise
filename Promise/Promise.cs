using System;
using System.Collections.Generic;
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

        protected delegate void OnFullFill(Object result);
        protected delegate void OnReject(Exception e);

        private OnFullFill onFulfill_ = null;
        private OnReject onReject_ = null;

        private Object result_ = null;

        protected Object lock_ = new Object();

        static private bool asyncHandler_ = true;

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
                // if you want to start handler asynchronous,
                // set asyncHandler_ as true.
                if (asyncHandler_) {
                    Task.Factory.StartNew(() => {
                        callDelegate.DynamicInvoke(result_);
                    });
                }
                else {
                    // but I would like to call handler asap.
                    callDelegate.DynamicInvoke(result_);
                }
            }
        }

        private void ResetHandler() {
            onFulfill_ = null;
            onReject_ = null;
        }

        // not a standard. but it might be useful for c#.
        public bool Wait() {
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
            return State == States.Fulfilled;
        }

        public Promise Catch(Action<Exception> postProcessRejected) {
            return Then(null, postProcessRejected);
        }

        public Promise<T> Catch<T>(Func<Exception, T> postProcessRejected) {
            return Then((Func<Object, T>)null, postProcessRejected);
        }

        public Promise Then(
            Action<Object> postProcessFulfilled,
            Action<Exception> postProcessRejected = null) {

            var postFulfilled = ConvertAsObjectFunc(postProcessFulfilled);
            var postRejected = ConvertAsObjectFunc(postProcessRejected);

            var promise = new Promise();

            ChainedThen(promise, postFulfilled, false, postRejected, false);

            return promise;
        }

        public Promise<TOtherResult> Then<TOtherResult>(
            Func<Object, Promise<TOtherResult>> postProcessFulfilled,
            Func<Exception, Promise<TOtherResult>> postProcessRejected = null) {

            var postFulfilled = ConvertAsObjectFunc(postProcessFulfilled);
            var postRejected = ConvertAsObjectFunc(postProcessRejected);

            var promise = new Promise<TOtherResult>();
            ChainedThen(promise, postFulfilled, true, postRejected, true);

            return promise;
        }

        public Promise<TOtherResult> Then<TOtherResult>(
            Func<Object, TOtherResult> postProcessFulfilled,
            Func<Exception, TOtherResult> postProcessRejected = null) {

            var postFulfilled = ConvertAsObjectFunc(postProcessFulfilled);
            var postRejected = ConvertAsObjectFunc(postProcessRejected);

            var promise = new Promise<TOtherResult>();
            ChainedThen(promise, postFulfilled, true, postRejected, true);

            return promise;
        }

        // Action<T> => Action<Object>
        protected Action<Object> ConvertAsObjectAction<T>(Action<T> action) {
            return action == null ? null : new Action<Object>(
                (value) => {
                    action((T)value);
                }
            );
        }

        // Action<T> => Func<T, Object>
        protected Func<T, Object> ConvertAsObjectFunc<T>(Action<T> action) {
            return ConvertAsObjectFunc<T, T>(action);
        }

        // Action<T> => Func<TNew, Object>
        protected Func<TNew, Object> ConvertAsObjectFunc<TOrg, TNew>(
            Action<TOrg> action) {
            return action == null ? null : new Func<TNew, Object>(
                (result) => {
                    action((TOrg)(object)result);
                    return null;
                }
            );
        }

        // Func<T1, T2> => Func<Object, Object>
        protected Func<Object, Object> ConvertAsObjectFunc<T, TResult>(
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
        protected void ChainedThen(Promise promise,
            Func<Object, Object> postProcessFullfilled, 
            bool postFulfilledReturnObject,
            Func<Exception, Object> postProcessRejected, 
            bool postRejectedReturnObject) {

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
                ((Promise)obj).Done(
                    (result) => this.Resolve(result),
                    (exception) => this.Reject(exception)
                );

                return;
            }

            // 3. for object and function
            try {
                var promise = GetPromiseFromThenAction(obj);

                if (promise != null) {
                    Resolve(promise);

                    return;
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

        private Promise GetPromiseFromThenAction(
            Object target) {

            if (target is Action<Action<Object>, Action<Exception>>) {
                return new Promise(
                    (Action<Action<Object>, Action<Exception>>)target
                );
            }
            else if (target is Action<Action<Object>>) {
                return new Promise((Action<Action<Object>>)target);
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

            var postFulfill = ConvertAsObjectFunc<T, Object>(postProcessFulfilled);
            var postRejected = ConvertAsObjectFunc(postProcessRejected);

            var promise = new Promise();

            ChainedThen(promise, postFulfill, false, postRejected, false);

            return promise;
        }

        public Promise<TOtherResult> Then<TOtherResult>(
            Func<T, Promise<TOtherResult>> postProcessFulfilled,
            Func<Exception, Promise<TOtherResult>> postProcessRejected = null) {

            var postFulfill = ConvertAsObjectFunc(postProcessFulfilled);
            var postRejected = ConvertAsObjectFunc(postProcessRejected);

            var promise = new Promise<TOtherResult>();

            ChainedThen(promise, postFulfill, true, postRejected, true);

            return promise;
        }

        public Promise<TOtherResult> Then<TOtherResult>(
            Func<T, TOtherResult> postProcessFulfilled,
            Func<Exception, TOtherResult> postProcessRejected = null) {

            var postFulfill = ConvertAsObjectFunc(postProcessFulfilled);
            var postRejected = ConvertAsObjectFunc(postProcessRejected);

            var promise = new Promise<TOtherResult>();

            ChainedThen(promise, postFulfill, true, postRejected, true);

            return promise;
        }

        public void Done(Action<T> onFulfilled, Action<Exception> onRejected) {
            Done(ConvertAsObjectAction(onFulfilled), onRejected);
        }
        #endregion
    }
}
