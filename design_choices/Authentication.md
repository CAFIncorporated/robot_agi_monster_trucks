# API Access and Authentication
ingress with users with some authentication?
- probably not needed, but client for this can be drafted for demo purpose and for e2e testing. 
- e2e is tested with curl requests.

integration between service.
- internal client pod that can be used to send requests to the API for testing and demo purposes. this client can be a simple script or application that runs inside the cluster and can be accessed via kubectl exec or port forwarding. this allows for easy testing of the API without needing to expose it externally.

or is this API just an internal service that should just be available to other services in namespaced cluster.
- make netpol for services and store app names in values.yaml file.

# User Authentication
Is any authentication needed and if so is it tied to users or applications.
This would only enhance the security as rowlevel/table security could be implemented to restrict any access to data without proper authentication.
- if this is an internal API that is only used by other services in the cluster, then authentication may not be necessary, as the services can be trusted to communicate with each other within the cluster. However, if there is a possibility of external access or if there are multiple teams working on the same cluster, then implementing authentication can help ensure that only authorized users or applications can access the API and the data it manages.

for this solution it seems it is not needed and it is assumed that the API is only called by other services in the cluster where access or data processing is obfuscated and not needed to be tied to an end user or application. if there is a need for authentication in the future, it can be implemented using a simple token-based authentication mechanism or by integrating with an existing authentication service in the cluster. the database would also need to be configured to support authentication and authorization, such as by creating user accounts and assigning appropriate permissions to access the data.

# Conclusion
Authentication is not needed for this solution as it is assumed that the API is only called by internal services.

