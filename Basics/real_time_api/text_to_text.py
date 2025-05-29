import asyncio
from openai import AsyncOpenAI
import os

async def main():
    client = AsyncOpenAI(
        api_key = os.getenv('OPENAI_API_KEY'),
        organization= os.getenv('OPENAI_ORG_ID'),
    )
    # connect with real time api
    async with client.beta.realtime.connect(model="gpt-4o-realtime-preview") as connection:
        # update the session to include text modalities
        await connection.session.update(
            session={'modalities': ['text']})

        # create a conversation and send text input
        await connection.conversation.item.create(
            item={
                "type": "message",
                "role": "user",
                "content": [{"type": "input_text", "text": "Say hello!"}],
            }
        )

        # wait to receive a response
        await connection.response.create()

        async for event in connection:
            if event.type == 'response.text.delta':
                # text response from the model in real time
                print(event.delta, flush=True, end="")

            elif event.type == 'response.text.done':
                # response is done, print a newline
                print()

            elif event.type == "response.done":
                break

asyncio.run(main())