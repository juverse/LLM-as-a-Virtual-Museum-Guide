import asyncio
import websockets

# Set of connected clients
connected_clients = set()

# Function to handle each client connection
async def handle_client(websocket, path):
    # Add the new client to the set of connected clients
    connected_clients.add(websocket)
    try:
        # Listen for messages from the client
        async for message in websocket:
            # Broadcast the message to all other connected clients
            for client in connected_clients:
                if client != websocket:
                    await client.send(message)
    except websockets.exceptions.ConnectionClosed:
        pass
    finally:
        # Remove the client from the set of connected clients
        connected_clients.remove(websocket)

# Main function to start the WebSocket server
async def main():
    server = await websockets.serve(handle_client, 'localhost', 12345)
    await server.wait_closed()

# Run the server
if __name__ == "__main__":
    asyncio.run(main())