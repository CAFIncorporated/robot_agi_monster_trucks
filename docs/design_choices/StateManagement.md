would redis run as sidecar to the robot service. seems redis is usually good for caching information if multiple pods would need the cached data. otherwise the service could just store data in service and as long as requests are routed correctly data would be stored in memory of the service.

psql should maybe be behind its own service and act as a shared db for the multiple services in the namespace. 
depends on how many services are already using a central database and if there is already a database service running in the cluster that can be used.
also depends on amount of request being made and if there would be a bottleneck impact from other services that would harm the performance of the API. 

psql is used for source of truth this is only needed if the solution would need long term storage (problem would be if early requests are pushed out of memory by later requests. as there is no guarantee a database is needed.

# Conclusion
Psql database is best solution as no shared cached data is needed between services. 
Services cannot guarentee that data will be in memory and need a source of truth for the data.
