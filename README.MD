# App Insights Correlation Playground

## How does correlation work?

See: 
- https://docs.microsoft.com/en-us/azure/azure-monitor/app/correlation

## Scenarios

### Fn A -> Service Bus

**Scenario**: Function A submits message into Service Bus Queue

- no custom correlation code in function
- no special configuration on Service Bus

**Results**: 

Works out of box

- Submission Id:        7eaeb56f-1a9f-451f-9e55-ffc54acc12e7
- Fn request properties in App Insights (telemetrytype: 'request'):
  - Request Id:         |c2314dd473c52c43869c75125cc5a583.025ecd4141103743.
  - Operation Id: 	    c2314dd473c52c43869c75125cc5a583
  - Parent Id:          c2314dd473c52c43869c75125cc5a583
  - Custom properties
    - HostInstanceId:   7d0dd5e9-6b9a-4643-b30b-432758cedc45
    - InvocationId: 	b45af8f8-9a7f-417c-bc78-7b3c07d946b5
- Svc bus dep properties in App Insights (telemetrytype: 'dependency'):
  - Dependency Type:    Azure Service Bus
  - Operation Id:       c2314dd473c52c43869c75125cc5a583
  - Parent Id:          |c2314dd473c52c43869c75125cc5a583.025ecd4141103743.
  - Dependency Id:      |c2314dd473c52c43869c75125cc5a583.0ab2335a5550cd4c.
  - Custom properties:
    - HostInstanceId:   7d0dd5e9-6b9a-4643-b30b-432758cedc45
    - InvocationId:     b45af8f8-9a7f-417c-bc78-7b3c07d946b5

**View in App Insights**

![fn-to-svcbus](./images/fn-to-svcbus.png)


### Fn A -> Service Bus -> Fn B

**Scenario**: Same as previous scenario, but now there is another function linked to another App Insights resource, which receives the message
- no custom correlation code in function
- no special configuration on Service Bus

**Results**: 

Fn B logs the invocation to app insights but does not mention or show a correlation or dependency to the service bus queue or message
However, there IS a correlation to be found in the properties.  Notice how the request on AI B has a `Parent ID` which is equal to AI A `Dependency Id`.  The UI or map on either side though does not show this correlation between both.

- Submission Id:        7eaeb56f-1a9f-451f-9e55-ffc54acc12e7
- Fn request properties in App Insights (telemetrytype: 'request'):
  - Request Id:         |c2314dd473c52c43869c75125cc5a583.1347ee1852a3504b.
  - Operation Id: 	    c2314dd473c52c43869c75125cc5a583
  - Parent Id:          |c2314dd473c52c43869c75125cc5a583.0ab2335a5550cd4c.
  - Custom properties
    - HostInstanceId:   880449e7-fef4-4a50-8cad-369f36c5d598
    - InvocationId: 	a4d65d54-1688-4b70-abc7-44efd1433afe
    - MessageId:        0edeaa16c20f4f3698b4bf49ff3d6721
