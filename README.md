# EventHubReader
Reader from Event Hub that handles all partitions and filters. 

Update the following in app.config
1. Event hub Connection string
2. Storage Connection string

This tool uses blocking collection to write event data as JSON and also to write status to WPF UI using dispatcher.

Following are mandatory fields

1. Event Hub Name : Name of Event Hub
2. Path : Path to write JSON.

If consumer group is not provided, it uses the default consumer group.

For every event data, it saves two files
1. EventData_GUID.json : contains body of event data.
2. EventData_Properties : Properties of Event Data.

Moved out the base implemenatation of processing from Event Hub to library so that it can be plug and played with any unit test (or) console application

For the filter, it just supports ActivityId,  VIN. Will be undergoing changes to handle in generic way to configure filters
