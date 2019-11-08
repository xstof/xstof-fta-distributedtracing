# App Insights Correlation Playground

## How does correlation work?

See: 
- https://docs.microsoft.com/en-us/azure/azure-monitor/app/correlation

## Scenarios

### Fn A -> Service Bus

**Scenario**: Function A submits message into Service Bus Queue

- no custom correlation code in function
- no special configuration on Service Bus

**Results**: works out of box

- Submission Id: a5edc15c-027a-4c62-baec-ff082cf7b526
- Fn request properties in App Insights (telemetrytype: 'request'):
  - Request Id:         |5bdd545890b9334494db11b2fa6974e4.7b947f594c1a3d42.
  - Operation Id: 	    5bdd545890b9334494db11b2fa6974e4
  - Parent Id:          5bdd545890b9334494db11b2fa6974e4
  - Custom properties
    - HostInstanceId:   7aacd506-eb7f-49b2-a266-8a57ad626acd
    - InvocationId: 	1c6f7e99-ad94-42a1-b8ce-acd8a772788b
- Svc bus dep properties in App Insights (telemetrytype: 'dependency'):
  - Dependency Type:    Azure Service Bus
  - Operation Id:       5bdd545890b9334494db11b2fa6974e4
  - Parent Id:          |5bdd545890b9334494db11b2fa6974e4.7b947f594c1a3d42.
  - Dependency Id:      |5bdd545890b9334494db11b2fa6974e4.6071d21f92b5f642.
  - Custom properties:
    - HostInstanceId:   7aacd506-eb7f-49b2-a266-8a57ad626acd
    - InvocationId:     1c6f7e99-ad94-42a1-b8ce-acd8a772788b

**View in App Insights**

![fn-to-svcbus](./images/fn-to-svcbus.png)