import os
import json
import websocket
import base64
import numpy as np
import soundfile as sf
import io
import datetime


OPENAI_API_KEY = os.getenv("OPENAI_API_KEY")
url = "wss://api.openai.com/v1/realtime?model=gpt-4o-realtime-preview-2024-12-17"

response_dir = "audio_responses"
os.makedirs(response_dir, exist_ok=True)

headers = [
    "Authorization: Bearer " + OPENAI_API_KEY,
    "OpenAI-Beta: realtime=v1"
]

def numpy_to_audio_bytes(audio_np, sample_rate):
    """Converts NumPy audio data into a WAV file as bytes."""
    with io.BytesIO() as buffer:
        sf.write(buffer, audio_np, samplerate=sample_rate, format='WAV')
        return buffer.getvalue()

def audio_to_item_create_event(audio_np, sample_rate):
    """Creates the WebSocket event for the audio data."""
    audio_bytes = numpy_to_audio_bytes(audio_np, sample_rate)
    audio_base64 = base64.b64encode(audio_bytes).decode('utf-8')
    
    return json.dumps({
        "type": "conversation.item.create",
        "item": {
            "type": "message",
            "role": "user",
            "content": [{
                "type": "input_audio",
                "audio": audio_base64
            }]
        }
    })

def on_open(ws):
    print("Connected to server.")


    

def on_message(ws, message):
    """Processes incoming messages and extracts audio data."""
    data = json.loads(message)
    print("Received event:", json.dumps(data, indent=2))
    
    if data.get("type") == "response.audio.delta":
        audio_base64 = data.get("delta", "")
        audio_bytes = base64.b64decode(audio_base64)
        

        timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
        response_filename = os.path.join(response_dir, f"response_{timestamp}.wav")

        # save audiodata
        with open(response_filename, "wb") as f:
            f.write(audio_bytes)
        print("Audio saved as response.wav")

def send_audio(ws, audio_np, sample_rate):
    """Sends audio data to the WebSocket connection."""
    audio_event = audio_to_item_create_event(audio_np, sample_rate)
    ws.send(audio_event)

def send_audio_from_file(ws, file_path):
    """Loads an audio file, converts it into a NumPy array and sends it."""
    
    audio_np, sample_rate = sf.read(file_path)
   
    send_audio(ws, audio_np, sample_rate)



def start_websocket():
    ws = websocket.WebSocketApp(
        url,
        header=headers,
        on_open=on_open,
        on_message=on_message,
    )
   
    audio_file_path = r"C:\Users\jlansche\Work Folders\Documents\Labrotation\realtime_api\test.wav"
    
    ws.on_open = lambda ws: send_audio_from_file(ws, audio_file_path)

    ws.run_forever()

if __name__ == "__main__":
    start_websocket()
