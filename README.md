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

These codes was written by refering <https://www.promisejs.org/implementing/>

by comparing the standard. I implemented pattern by using generic,
creating one some API for c# framework (Wait, AsyncCall) 

## Some additional feature

### Synchronous method call
If you want to call the 'then' method in same thread, undefine the 
`ASYNC_THEN_CALL`

### Additional function for EAP. (IAsyncResult)

```
Promise<IPHostEntry> promise = PromiseExtensions.CallAsync<IPHostEntry>(
    (callback) => Dns.BeginGetHostEntry(host, callback, null),
    (result) => Dns.EndGetHostEntry(result)
);
```
