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
However, there IS a correlation to be found in the properties.  Notice how the request on AI B has a `Parent Id` which is equal to AI A `Dependency Id`.  The UI or map on either side though does not show this correlation between both.

- Submission Id:        7eaeb56f-1a9f-451f-9e55-ffc54acc12e7
- Fn request properties in App Insights (telemetrytype: 'request'):
  - TriggerReason:      New ServiceBus message detected on 'aicorr-svcbusqueue'.
  - Request Id:         |c2314dd473c52c43869c75125cc5a583.1347ee1852a3504b.
  - Operation Id: 	    c2314dd473c52c43869c75125cc5a583
  - Parent Id:          |c2314dd473c52c43869c75125cc5a583.0ab2335a5550cd4c.
  - Request Source:	    type:Azure Service Bus | name:aicorr-svcbusqueue | endpoint:sb://aicorr-svcbusns.servicebus.windows.net/
  - Custom properties
    - HostInstanceId:   880449e7-fef4-4a50-8cad-369f36c5d598
    - InvocationId: 	a4d65d54-1688-4b70-abc7-44efd1433afe
    - MessageId:        0edeaa16c20f4f3698b4bf49ff3d6721

**How this works**:

Azure Functions records metadata on the service bus message, most in particular:
- `$AzureWebJobsParentId` is set to the `InvocationId` of the dependency entry (which in turn is also the same as the request entry - as the InvocationId indicates the unique invocation identifier of the function)
- `DiagnosticId` is set to the `Dependency Id` of the dependency entry of the function which submitted the message
- Function B takes this `DiagnosticId` metadata and uses that as it's `Parent Id`

### Recording custom telemetry on a function invocation's telemetry

Recording custom properties on the telemetry created during a function invocation is as easy as:

~~~cs
using System.Diagnostics;
// ...
Activity currActivity = Activity.Current;
currActivity.AddTag("MySubmissionId", submissionId);
~~~

Note: this records the custom property on the `request` telemetry item not on any dependency items this invocation created.

### Function making HTTP call

The interesting properties that appear as headers on the outbound HTTP call are:

| Header                   | Value                                                   |
| ------------             | -----------------------------------                     |
| **TraceParent**          | 00-2ba1dcbb920c0c489823e86ee57e557d-82c588a5d78c3f4a-00 |
| **Request-Id**           | \|2ba1dcbb920c0c489823e86ee57e557d.82c588a5d78c3f4a.    |
| **Request-Context**      | appId=cid-v1:6b06625e-3d1f-4bb4-8b50-b22b20b5a6d6       |
| X-Request-Id             | c39043fc-5693-4ca6-ba3c-56cf059acbad                    |

(Note that our application id for App Insights A == 6b06625e-3d1f-4bb4-8b50-b22b20b5a6d6)

... and what we see on App Insights:

Request telemetry item:

| Property                 | Value                                                   |
| ------------             | -----------------------------------                     |
| OperationId              | 2ba1dcbb920c0c489823e86ee57e557d                        |
| ParentId                 | 2ba1dcbb920c0c489823e86ee57e557d                        |
| Request-Id               | \|2ba1dcbb920c0c489823e86ee57e557d.509b52d5c4ba6145.    |
| (custom) HostInstanceId  | 0a57ccf6-58ea-4392-9da0-aaf0c043715a                    |
| (custom) InvocationId    | f860490b-30c2-4cb7-8688-10076b02ba7d                    |

Dependency telemetry item:

| Property                 | Value                                                   |
| ------------             | -----------------------------------                     |
| Remote Dependency Name   | GET /r/1ccpqtm1                                         |
| Operation Id             | 2ba1dcbb920c0c489823e86ee57e557d                        |
| Parent Id                | \|2ba1dcbb920c0c489823e86ee57e557d.509b52d5c4ba6145.    |
| **Dependency Id**        | \|2ba1dcbb920c0c489823e86ee57e557d.82c588a5d78c3f4a.    |
| (custom) HostInstanceId  | 0a57ccf6-58ea-4392-9da0-aaf0c043715a                    |
| (custom) InvocationId    | f860490b-30c2-4cb7-8688-10076b02ba7d                    |


**How this works**:

This means that the dependency telemetry item's `Dependency Id` is set on the outbound HTTP call's `TraceParent` and `Request-Id` so it can be picked up by the receiver.  In addition to this parent/child relation, also the identifier for the caller's App Insights resource is passed on in the HTTP call in the `Request-Context` header.  This is to enable cross-workspace discovery to power tooling with like the app map.


### Fn A -> Service Bus -> Fn B (with custom code to correlate across workspaces)

