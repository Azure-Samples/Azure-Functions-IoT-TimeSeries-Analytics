#r "SendGrid"

using System;
using System.Net;
using SendGrid.Helpers.Mail;

public static HttpResponseMessage Run(HttpRequestMessage req, out Mail message , TraceWriter log)
{
log.Info("C# HTTP trigger function processed a request.");
var data = req.Content.ReadAsStringAsync().Result;
 
log.Info("data: " + data);
 
message = new Mail
{
Subject = "IoT Humidity Alert"
};
 
var personalization = new Personalization();
personalization.AddTo(new Email("YOUR_E_MAIL_HERE"));
 
Content content = new Content
{
Type = "text/plain",
Value = data
};
message.AddContent(content);
message.AddPersonalization(personalization);
 
return req.CreateResponse(HttpStatusCode.OK, "Result" );
}