import asyncio
import openai
from openai import AsyncOpenAI
import os


async def main():
    client = AsyncOpenAI(
        api_key = os.getenv('OPENAI_API_KEY'),
        organization= os.getenv('OPENAI_ORG_ID'),
    )
      
    # Establish the real-time connection with the model
    async with client.beta.realtime.connect(model="gpt-4o-realtime-preview") as connection:
        # Set the session's modality to 'text'
        await connection.session.update(session={'modalities': ['text']})

        # Create an empty conversation
        conversation = []

        while True:
            # Get user input for the next message
            user_message = input("You: ")
            if user_message.lower() in ['exit', 'quit']:
                print("Ending conversation.")
                break

            # Add the user's message to the conversation context
            conversation.append({
                "type": "message",
                "role": "user",
                "content": [{"type": "input_text", "text": user_message}],
            })

            # Send the user's message to the model
            await connection.conversation.item.create(
                item=conversation[-1]
            )

            # Request a response from the model
            await connection.response.create()

            # Process the response from the model in real-time
            async for event in connection:
                if event.type == 'response.text.delta':
                    # Print out the response as it arrives
                    print(f"AI: {event.delta}", end='', flush=True)

                elif event.type == 'response.text.done':
                    # Once the response is done, print a newline
                    print("\n")
                    break

                elif event.type == "response.done":
                    # End the connection when the response is done
                    break

# Run the conversation
asyncio.run(main())
