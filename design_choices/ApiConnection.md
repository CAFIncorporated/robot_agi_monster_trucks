TODO: check the business case for this. consider diff scenarios where each solution may be best.
persistent connection where commands come 1 after another in short span would need websocket, but if commands are more sporadic and not time-sensitive, HTTP may be sufficient. also consider the complexity of implementing and maintaining a websocket connection compared to HTTP, as well as the scalability implications of each approach.
# Websocket or HTTP connection to the API. This is used to send and receive messages from the API.
# The connection can be established using either WebSockets or HTTP, depending on the client's capabilities and preferences. The API will support both types of connections to ensure compatibility with a wide range of clients and use cases. The connection will be used to send requests to the API for creating, reading, updating, and deleting points, as well as to receive responses and updates from the API. The API will handle incoming requests and manage the connection to ensure efficient communication between the client and the server.

Pros for WebSockets:
- Real-time communication: WebSockets allow for full-duplex communication, enabling real-time updates and notifications from the server to the client without the need for polling.
- Lower latency: WebSockets can reduce latency compared to HTTP, as they maintain a persistent connection and avoid the overhead of establishing a new connection for each request.
- Efficient for frequent updates: If the application requires frequent updates or notifications, WebSockets can be more efficient than HTTP, as they eliminate the need for repeated HTTP requests.

Pros for HTTP:
- Simplicity: HTTP is a well-established protocol that is widely supported and easier to implement for simple request-response interactions.
- Compatibility: HTTP is compatible with a wide range of clients and tools, making it easier to integrate with existing systems and libraries.
- Statelessness: HTTP is stateless, which can simplify server-side implementation and scaling, as the server does not need to maintain connection state for each client.
