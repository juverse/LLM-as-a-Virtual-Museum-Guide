import asyncio
import websockets

# Function to handle the chat client
async def chat():
    async with websockets.connect('ws://localhost:12345') as websocket:
        while True:
            # Prompt the user for a message
            message = input("Enter message: ")
            # Send the message to the server
            await websocket.send(message)
            # Receive a message from the server
            response = await websocket.recv()
            print(f"Received: {response}")

# Run the client
if __name__ == "__main__":
    asyncio.run(chat())