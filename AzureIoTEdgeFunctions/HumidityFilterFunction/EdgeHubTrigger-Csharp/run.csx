#r "Microsoft.Azure.Devices.Client"
#r "Newtonsoft.Json"


using System.IO;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;

public static async Task Run(Message messageReceived, IAsyncCollector<Message> output, TraceWriter log)
{
    // Threshold for Humidity
    const int humidityThreshold = 24;

    byte[] messageBytes = messageReceived.GetBytes();
    var messageString = System.Text.Encoding.UTF8.GetString(messageBytes);

    if (!string.IsNullOrEmpty(messageString))
    {
        // Get the body of the message and deserialize it
        var messageBody = JsonConvert.DeserializeObject<MessageBody>(messageString);

        // Check for humidity value
        if (messageBody != null && messageBody.ambient.humidity > humidityThreshold)
        {
            // We will send the message to the output as the temperature value is greater than the threashold
            var filteredMessage = new Message(messageBytes);
            // We need to copy the properties of the original message into the new Message object
            foreach (KeyValuePair<string, string> prop in messageReceived.Properties)
            {
                filteredMessage.Properties.Add(prop.Key, prop.Value);
            }
            // We are adding a new property to the message to indicate it is a humidty alert
            filteredMessage.Properties.Add("MessageType", "HumidityAlert");
            // Send the message        
            await output.AddAsync(filteredMessage);
            log.Info("Received and transferred a message with temperature above the threshold");
        }
    }
}

// Objects representing the message we are getting from IoT Devices
class MessageBody
{
    public Machine machine {get;set;}
    public Ambient ambient {get; set;}
    public string timeCreated {get; set;}
}
class Machine
{
   public double temperature {get; set;}
   public double pressure {get; set;}         
}
class Ambient
{
   public double temperature {get; set;}
   public int humidity {get; set;}         
}