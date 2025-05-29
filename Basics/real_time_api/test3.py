import asyncio
import openai
from openai import AsyncOpenAI
import os
import sounddevice as sd
import numpy as np
import wave
import tempfile
import io
import speech_recognition as sr
import pyaudio
from gtts import gTTS
import playsound

async def record_audio():
    recognizer = sr.Recognizer()
    with sr.Microphone() as source:
        print("Listening...")
        recognizer.adjust_for_ambient_noise(source)
        audio = recognizer.listen(source)
    
    with tempfile.NamedTemporaryFile(delete=False, suffix=".wav") as temp_audio:
        temp_audio.write(audio.get_wav_data())
        return temp_audio.name

def text_to_speech(text):
    tts = gTTS(text=text, lang='en')
    tts.save("response.mp3")
    playsound.playsound("response.mp3")

async def main():
    client = AsyncOpenAI(
        api_key=os.getenv('OPENAI_API_KEY'),
        organization=os.getenv('OPENAI_ORG_ID'),
    )
    
    async with client.beta.realtime.connect(model="gpt-4o-realtime-preview") as connection:
        await connection.session.update(session={'modalities': ['audio']})
        
        conversation = []

        while True:
            audio_path = await record_audio()
            recognizer = sr.Recognizer()
            with sr.AudioFile(audio_path) as source:
                audio = recognizer.record(source)
                try:
                    user_message = recognizer.recognize_google(audio)
                    print(f"You: {user_message}")
                except sr.UnknownValueError:
                    print("Could not understand audio")
                    continue
                except sr.RequestError:
                    print("Error with speech recognition service")
                    continue

            if user_message.lower() in ['exit', 'quit']:
                print("Ending conversation.")
                break

            conversation.append({
                "type": "message",
                "role": "user",
                "content": [{"type": "input_text", "text": user_message}],
            })

            await connection.conversation.item.create(
                item=conversation[-1]
            )
            await connection.response.create()

            response_text = ""
            async for event in connection:
                if event.type == 'response.text.delta':
                    response_text += event.delta
                elif event.type == 'response.text.done':
                    print(f"AI: {response_text}")
                    text_to_speech(response_text)
                    break
                elif event.type == "response.done":
                    break

asyncio.run(main())
