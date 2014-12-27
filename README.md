# Promise/c-sharp
      
## Motivation

The massive call for asynchronous API will make massive task 
if you use TAP. I could not handle them in proper way. For example, if 
you should read data in socket after asynchronous call, massive 
task will make the socket reader get a time-out error due to the task 
context switching.
     
EAP can eliminate such time-out error. but EAP, I cannot help 
understanding the code due to callback hell. But, I found promise 
pattern for asynchronous frequent call is very good to be 
understood which can be implemented by using callback function.
            
for promises pattern. check following two url.
            
 * <https://www.promisejs.org/>
 * <https://github.com/promises-aplus>

These codes were written by refering <https://www.promisejs.org/implementing/>.

## Some additional feature
by comparing the standard. I implemented pattern by using generic,
creating one some API for c# framework (Wait, AsyncCall) 

### Synchronous method call
If you want to call the 'then' method in same thread, undefine the 
`ASYNC_THEN_CALL`. If you want to handle massive request, please undefine 
it. You should define it for passing unit test (promise specification)

### Additional function for EAP. (IAsyncResult)
I added two functions which can be used for EAP. see following the example.

```
Promise<IPHostEntry> promise = PromiseExtensions.CallAsync<IPHostEntry>(
    (callback) => Dns.BeginGetHostEntry(host, callback, null),
    (result) => Dns.EndGetHostEntry(result)
);
```
