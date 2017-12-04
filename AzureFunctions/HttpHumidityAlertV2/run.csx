#r "Newtonsoft.Json"
#r "SendGrid"

using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json; 
using SendGrid.Helpers.Mail; 

public async static Task<IActionResult> Run(HttpRequest req, IAsyncCollector<SendGridMessage> messages, TraceWriter log)
{
    log.Info("SendGrid message");
    string body;  
    var stream = req.Body;
    byte[] result = new byte[stream.Length];
    await stream.ReadAsync(result, 0, (int)stream.Length);
    body = System.Text.Encoding.UTF8.GetString(result);
    var message = new SendGridMessage();
    message.AddTo("tsushi@microsoft.com");
    message.AddContent("text/html", body);
    message.SetFrom("iot@alert.com");
    message.SetSubject("[Alert] IoT Hub Notrtification");
    await messages.AddAsync(message); 
    return (ActionResult)new OkObjectResult("The E-mail has been sent.");
}
