# Promise/c-sharp
      
## Motivation

The massive call for asynchronous API will make massive task 
if you use TAP. 
     
In .net 4.0, I could not handle them in proper way. For example, if 
you should read data in socket after asynchronous call, massive 
task will make the socker reader get time-out error due to task 
scheduler.
     
EAP can eliminate such time-out error. but EAP, I cannot help 
understanding the code due to callback hell. But, I found promise 
pattern for asynchronous frequent call is very good to be 
understood which can be implemented by using callback function.
            
for promises pattern. check following two url.
            
 * <https://www.promisejs.org/>
 * <https://github.com/promises-aplus>

those codes was written by refering <https://www.promisejs.org/implementing/>

by comparing the standard. I implemented pattern by using generic,
creating one more API (Wait) and not supporting 
    '2.2.4 : `onFulfilled` or `onRejected` must not be called until 
            the execution context stack contains only platform code' 
        by default.
       
for not invoking thread, I did not support delayed 'onFulfilled' or 
'onRejected' call. If you want
delayed call, set Promise.asyncHander_ as true.

## Comparison of TAP and Promise.

