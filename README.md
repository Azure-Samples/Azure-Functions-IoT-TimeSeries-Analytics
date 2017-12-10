# IoT Alerting System Powered by Azure IoT Edge, Azure Functions and Time Series Insights

## Overview

**IoT Alerting System** helps to monitor temperature and humidity conditions and infrom responsible technicians for unexpected changes in the values. 

The project has the following architecture:

![IoT Alerting System Architecture](https://github.com/Azure-Samples/Azure-Functions-IoT-TimeSeries-Analytics/blob/master/Images/Architecture.PNG "IoT Alerting System Architecture")

A group of devices is monitoring machine temperature and ambient humidity. On the devices are deployed **two functions - one for monitoring machine temperature and another one for ambient humidity**. The functions monitor if the values are above predefined threshold and if so, they send the data to a **IoT Hub**. In the IoT Hub the data is filtered and it is sent to one of the configured **Event Hubs** to persist in **Time Series Insights** or pass it for aggregation by **Azure Stream Analytics**. If there are more events than predefined threshold in **Azure Stream Analytics** an **Azure Function** is triggered to send an email to the responsible technician.

A several steps walkthrough for building the project  is provided below.

## Walkthrough

### 1. Setting Up Edge Devices

### 2. Azure Function on IoT Edge

### 3. Configuring IoT Hub

### 4. Azure Functions for Alerting

### 5. Analyzing Data with Time Series Insights 


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
