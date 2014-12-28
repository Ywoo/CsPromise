# Promise A+/c-sharp
      
For promises a+. check following two url.
            
 * <https://www.promisejs.org/>
 * <https://github.com/promises-aplus>

These codes were written by refering <https://www.promisejs.org/implementing/>.
Also, The codes was tested by test case which is similar with <https://github.com/promises-aplus/promises-tests>

## Some additional feature
By comparing the standard. I implemented pattern by using generic,
creating one some API for c# framework (Wait, AsyncCall) 

### Synchronous method call
If you want to call the 'then' method in same thread, undefine the 
`ASYNC_THEN_CALL`. If you want to handle massive request, please undefine 
it. But, you should define it for passing unit test for promise 
a+ specification.

### Additional function for EAP. (IAsyncResult)
I added two functions which can be used for EAP. see following the example.

```
PromiseExtensions.CreatePromiseFromIAsyncResult<IPHostEntry>(
    (callback) => Dns.BeginGetHostEntry(host, callback, null),
    (result) => Dns.EndGetHostEntry(result)
)
.Then((entry) => {
   ....
}
```

