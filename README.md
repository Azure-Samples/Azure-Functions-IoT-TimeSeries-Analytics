# IoT Alerting System Powered by Azure IoT Edge, Azure Functions and Time Series Insights

## Overview

**IoT Alerting System** helps to monitor temperature and humidity conditions and inform responsible technicians for unexpected changes in the values. 

The project has the following architecture:

![IoT Alerting System Architecture](https://github.com/Azure-Samples/Azure-Functions-IoT-TimeSeries-Analytics/blob/master/Images/Architecture.PNG "IoT Alerting System Architecture")

A group of devices is monitoring machine temperature and ambient humidity. On the devices are deployed **two functions - one for monitoring machine temperature and another one for ambient humidity**. The functions monitor if the values are above predefined threshold and if so, they send the data to a **IoT Hub**. In the IoT Hub the data is filtered and it is sent to one of the configured **Event Hubs** to persist in **Time Series Insights** or pass it for aggregation by **Azure Stream Analytics**. If there are more events than predefined threshold in **Azure Stream Analytics** an **Azure Function** is triggered to send an email to the responsible technician.

A several steps walkthrough for building the project  is provided below.

## Walkthrough

### 1. Setting Up Edge Devices

Finding and using a real IoT device is sometimes difficult that is why we will use Azure VM instead.

Please follow the instructions in this tutorial to [deploy Azure IoT Edge runtime on Azure Linux VM](https://docs.microsoft.com/en-us/azure/iot-edge/tutorial-simulate-device-linux "deploy Azure IoT Edge runtime on Azure Linux VM"). 

### 2. Azure Function on IoT Edge

Next, we will deploy our first Azure Function that will be responsible for monitoring machines temperature. For that step follow the tutorial for [deploying Azure Function to IoT Edge](https://docs.microsoft.com/en-us/azure/iot-edge/tutorial-deploy-function "deploying Azure Function to IoT Edge"). You can find all required assets for this function in the folder [Temperature Filter Function](https://github.com/Azure-Samples/Azure-Functions-IoT-TimeSeries-Analytics/tree/master/AzureIoTEdgeFunctions/TemperatureFilterFunction "Temperature Filter Function")

And the source code the functions is:

```csharp

    #r "Microsoft.Azure.Devices.Client"
    #r "Newtonsoft.Json"

    using System.IO;
    using Microsoft.Azure.Devices.Client;
    using Newtonsoft.Json;

    public static async Task Run(Message messageReceived, IAsyncCollector<Message> output, TraceWriter log)
    {
        // Temperature Threshold 
        const int temperatureThreshold = 25;
        byte[] messageBytes = messageReceived.GetBytes();
        var messageString = System.Text.Encoding.UTF8.GetString(messageBytes);

        if (!string.IsNullOrEmpty(messageString))
        {
            // Get the body of the message and deserialize it
            var messageBody = JsonConvert.DeserializeObject<MessageBody>(messageString);

            // Check temperature value
            if (messageBody != null && messageBody.machine.temperature > temperatureThreshold)
            {
                // We will send the message to the output as the temperature value is greater than the threashold
                var filteredMessage = new Message(messageBytes);
                // We need to copy the properties of the original message into the new Message object
                foreach (KeyValuePair<string, string> prop in messageReceived.Properties)
                {
                    filteredMessage.Properties.Add(prop.Key, prop.Value);
                }
                // We are adding a new property to the message to indicate it is a temperature alert
                filteredMessage.Properties.Add("MessageType", "Alert");
                // Send the message        
                await output.AddAsync(filteredMessage);
                log.Info("Received and transferred a message with temperature above the threshold");
            }
        }
    }

```
The other function  will monitor ambient humidity and again you can follow the tutorial for [deploying Azure Function to IoT Edge](https://docs.microsoft.com/en-us/azure/iot-edge/tutorial-deploy-function "deploying Azure Function to IoT Edge") with few modifications:

* In **Create a function project** change the function code slightly. You can find all required assets for this function in the folder [Humidity Filter Function](https://github.com/Azure-Samples/Azure-Functions-IoT-TimeSeries-Analytics/tree/master/AzureIoTEdgeFunctions/HumidityFilterFunction "Humidity Filter Function"). The code for the function is below:

```csharp
    ...

    public static async Task Run(Message messageReceived, IAsyncCollector<Message> output, TraceWriter log)
    {
        // Threshold for Humidity
        const int humidityThreshold = 24;

        byte[] messageBytes = messageReceived.GetBytes();
        var messageString = System.Text.Encoding.UTF8.GetString(messageBytes);

        if (!string.IsNullOrEmpty(messageString))
        {
            ...
            
            // Check for humidity value
            if (messageBody != null && messageBody.ambient.humidity > humidityThreshold)
            {
                ...
                
                filteredMessage.Properties.Add("MessageType", "HumidityAlert");
                // Send the message        
                await output.AddAsync(filteredMessage);
                log.Info("Received and transferred a message with ambient humidity above the threshold");
            }
        }
    }

```


* Also, the IoT Hub routes now will be extended to the following configuration:

```json
     {
      "routes": {
        "sensorToFilter": "FROM /messages/modules/tempSensor/outputs/temperatureOutput INTO BrokeredEndpoint(\"/modules/temperatureFilter/inputs/input1\")",
        "sensorToHumidityFilter": "FROM /messages/modules/tempSensor/outputs/temperatureOutput INTO BrokeredEndpoint(\"/modules/humidityFilter/inputs/input1\")",
        "filterToIoTHub": "FROM /messages/modules/temperatureFilter/outputs/* INTO $upstream",
        "humidityFilterToIoTHub": "FROM /messages/modules/humidityFilter/outputs/* INTO $upstream"
        }
      }
```

### 3. Configure IoT Hub

Next, the connections to 3 Event Hubs instances need to be configured. These Event Hub instances will allow be responsible for getting the data from IoT Hub and pass it for further processing.

First, IoT Hub Endpoints will be defined for each of the Event Hubs as shown below:

![IoT Hub Endpoints](https://github.com/Azure-Samples/Azure-Functions-IoT-TimeSeries-Analytics/blob/master/Images/IotHubEndpoints.PNG "IoT Hub Endpoints")

Next, the rules for sending the data to the correct IoT Hub instance is defined. The rules are called "routes" in IoT Hub and the final configuration looks like:

![IoT Hub Routes](https://github.com/Azure-Samples/Azure-Functions-IoT-TimeSeries-Analytics/blob/master/Images/IotHubRoutes.PNG "IoT Hub Routes")

And here is the configuration for the humidity route:

![IoT Hub Humidity Route](https://github.com/Azure-Samples/Azure-Functions-IoT-TimeSeries-Analytics/blob/master/Images/IotHubHumidityRoute.PNG "IoT Hub Humidity Route")

Two of the Event Hubs are connected to Stream Analytics Jobs that are aggregating the values and based on predefined threshold trigger an Azure Function for sending an email.

This is the Azure Stream Analytics job for humidity:

```sql
SELECT
    System.TimeStamp AS Time,
    COUNT(*) AS [Count]
INTO
    humidityout
FROM
    humidity TIMESTAMP BY TIMECREATED
GROUP BY
    TumblingWindow(second, 180)
HAVING
    [Count] >= 5
```

And this is the Azure Stream Analytics job for temperature:

```sql
SELECT
    System.TimeStamp AS Time,
    COUNT(*) AS [Count]
INTO
    AlertOutput
FROM
    temperature TIMESTAMP BY TIMECREATED
GROUP BY
    TumblingWindow(second, 180)
HAVING
    [Count] >= 5
```

### 4. Azure Functions for Alerting

An email will be send when we have more than 5 events per 3 minutes related to elevated temperature or ambient humidity.
Azure Stream Analytics has first party integration with Azure Functions. We use Azure Function Runtime v2 so please follow the [steps](https://docs.microsoft.com/en-us/azure/azure-functions/functions-versions "Configure Azure Function App to use runtime version 2") to configure your Function App. The code for the function is below: 

```csharp
#r "Newtonsoft.Json"
#r "SendGrid"

using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json; 
using SendGrid.Helpers.Mail; 
using System.Text;

public async static Task<IActionResult> Run(HttpRequest req, IAsyncCollector<SendGridMessage> messages, TraceWriter log)
{
    log.Info("SendGrid message"); 
    using (StreamReader reader = new StreamReader(req.Body, Encoding.UTF8))
    {
        var body = await reader.ReadToEndAsync();
        var message = new SendGridMessage();
        message.AddTo("technicians@iot.com");
        message.AddContent("text/html", body);
        message.SetFrom("iot@alert.com");
        message.SetSubject("[Alert] IoT Hub Notrtification");
        await messages.AddAsync(message); 
        return (ActionResult)new OkObjectResult("The E-mail has been sent.");
    }
}
```

The configuration of the function is in function.json file:

```json
    {
    "bindings": [
      {
        "authLevel": "function",
        "name": "req",
        "type": "httpTrigger",
        "direction": "in"
      },
      {
        "name": "$return",
        "type": "http",
        "direction": "out"
      },
      {
        "type": "sendGrid",
        "name": "messages",
        "apiKey": "SendGridAttribute.ApiKey",
        "direction": "out"
      }
    ],
    "disabled": false
  }
```

This function requires to set a SendGrid key to an App Setting property called "SendGridAttribute.ApiKey" inside the Function App. In the Azure portal you can create free account for SendGrid with 20 000 email per month. 

All the assets for the email function can be found [here](https://github.com/Azure-Samples/Azure-Functions-IoT-TimeSeries-Analytics/tree/master/AzureFunctions/HttpHumidityAlertV2 "Folder for Azure Function responsible for sending emails")
Also, you can find [Node](https://github.com/Azure-Samples/Azure-Functions-IoT-TimeSeries-Analytics/tree/master/AzureFunctions/HttpTemperatureAlert "Node sample for sending emails") and [C# V1](https://github.com/Azure-Samples/Azure-Functions-IoT-TimeSeries-Analytics/tree/master/AzureFunctions/HttpHumidityAlert "C# V1 sample for sending emails") samples.


### 5. Analyzing Data with Time Series Insights 

IoT Hub is sending all the data it gets to an instance of Event Hub. This Event Hub is configured to be a source of data for [Time Series Insights](https://azure.microsoft.com/en-us/services/time-series-insights/) instance:

![Time Series Insights Data Source](https://github.com/Azure-Samples/Azure-Functions-IoT-TimeSeries-Analytics/blob/master/Images/TimeSeriesInsights-EventSources.PNG "Time Series Insights Data Source")

Time Series Insights is built in mind for IoT scenarios. It combines database and powerful visualization capabilities in one product designed for massive data throughput.

Time Series Insights automatically parses the data and shows a default graph for event count:

![Deafult dashboard for Time Series Insights](https://github.com/Azure-Samples/Azure-Functions-IoT-TimeSeries-Analytics/blob/master/Images/TimeSeriesInsights.png "Deafult dashboard for Time Series Insights")

It is extremely easy to configure multiple queries whose graphs can overlay:

![Mutiple queries in Time Series Insights](https://github.com/Azure-Samples/Azure-Functions-IoT-TimeSeries-Analytics/blob/master/Images/TimeSeriesInsights-Queries.PNG "Mutiple queries in Time Series Insights")

This can be easily done by cloning a query and modifying it.

But it is better if you can plot all your queries on separate part of screen for better visibility:

![Time Series Insights with mutiple queries displayed on a dashboard](https://github.com/Azure-Samples/Azure-Functions-IoT-TimeSeries-Analytics/blob/master/Images/Time%20Series%20Dashboard.PNG "Time Series Insights with mutiple queries displayed on a dashboard")

**Congratulations! You have just built complete IoT solution including edge logic, IoT Hub, alerting email functions and detailed data view powered by Time Series Insights!**  

## Features

This project framework provides the following features:

* Feature 1
* Feature 2
* ...

## Getting Started

### Prerequisites

(ideally very short, if any)

- OS
- Library version
- ...

### Installation

(ideally very short)

- npm install [package name]
- mvn install
- ...

### Quickstart
(Add steps to get up and running quickly)

1. git clone [repository clone url]
2. cd [respository name]
3. ...


## Demo

A demo app is included to show how to use the project.

To run the demo, follow these steps:

(Add steps to start up the demo)

1.
2.
3.

## Resources

(Any additional resources or related projects)

- Link to supporting information
- Link to similar sample
- ...
