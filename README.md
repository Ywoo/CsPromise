# Promise/c-sharp
      
## Promise pattern
For promises pattern. check following two url.
            
 * <https://www.promisejs.org/>
 * <https://github.com/promises-aplus>

These codes were written by refering <https://www.promisejs.org/implementing/>.

## Some additional feature
By comparing the standard. I implemented pattern by using generic,
creating one some API for c# framework (Wait, AsyncCall) 

### Synchronous method call
If you want to call the 'then' method in same thread, undefine the 
`ASYNC_THEN_CALL`. If you want to handle massive request, please undefine 
it. You should define it for passing unit test (promise specification)

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
