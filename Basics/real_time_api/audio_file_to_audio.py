import os
import json
import websocket
from websocket import create_connection
import base64
import numpy as np
import soundfile as sf
import io
import datetime
import logging

logging.basicConfig(level=logging.INFO, format="%(asctime)s - %(levelname)s - %(message)s")
logger = logging.getLogger(__name__)

OPENAI_API_KEY = os.getenv("OPENAI_API_KEY")
if not OPENAI_API_KEY:
    logger.error("OPENAI_API_KEY is not set. Exiting program.")
    exit(1)
url = "wss://api.openai.com/v1/realtime?model=gpt-4o-mini-realtime-preview-2024-12-17"

response_dir = "audio_responses"
os.makedirs(response_dir, exist_ok=True)

headers = [
    f"Authorization: Bearer {OPENAI_API_KEY}",
    "OpenAI-Beta: realtime=v1"
]

def numpy_to_audio_bytes(audio_np, sample_rate):
    """Converts NumPy audio data to a WAV file as bytes."""
    with io.BytesIO() as buffer:
        sf.write(buffer, audio_np, samplerate=sample_rate, format='WAV')
        return buffer.getvalue()

def audio_to_item_create_event(audio_np, sample_rate):
    """Creates a WebSocket event for the audio data."""
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

def on_open(ws, audio_file_path):
    logger.info("WebSocket connection opened.")
    send_audio_from_file(ws, audio_file_path)

def on_error(ws, error):
    logger.error(f"WebSocket error: {error}")

def on_close(ws, close_status_code, close_msg):
    logger.info(f"WebSocket connection closed: {close_status_code} - {close_msg}")

def on_message(ws, message):
    """Processes incoming messages and saves audio data."""
    try:
        event = json.loads(message)
        event_type = event.get("type", "")
        logger.info(f"Received event: {json.dumps(event, indent=2)}")

        if event_type == "response.audio.delta":
            audio_base64 = event.get("delta", "")
            if audio_base64:
                audio_bytes = base64.b64decode(audio_base64)
                timestamp = datetime.datetime.now().strftime("%Y%m%d_%H%M%S")
                response_filename = os.path.join(response_dir, f"response_{timestamp}.wav")
                
                with open(response_filename, "wb") as f:
                    f.write(audio_bytes)
                logger.info(f"Audio saved as {response_filename}")
        elif event_type == "response.audio.done":
            logger.info("Received response.audio.done. Audio response completed.")
        else:
            logger.debug(f"Unhandled event type: {event_type}")
    except json.JSONDecodeError as e:
        logger.error(f"Error decoding JSON response: {e}")

def send_audio(ws, audio_np, sample_rate):
    """Sends audio data to the WebSocket connection."""
    try:
        audio_event = audio_to_item_create_event(audio_np, sample_rate)
        ws.send(audio_event)
        logger.info("Audio data successfully sent.")
    except Exception as e:
        logger.error(f"Error sending audio data: {e}")

def send_audio_from_file(ws, file_path):
    """Loads an audio file, converts it to a NumPy array, and sends it."""
    try:
        audio_np, sample_rate = sf.read(file_path)
        if sample_rate != 16000:
            logger.warning(f"Sample rate {sample_rate} Hz detected, but OpenAI might need 16 kHz.")
        if len(audio_np.shape) > 1:  # Stereo → Mono
            audio_np = np.mean(audio_np, axis=1)
        audio_np = (audio_np * 32767).astype(np.int16)  # Normalize
        send_audio(ws, audio_np, 16000)  # Erzwinge 16 kHz
    except Exception as e:
        logger.error(f"Error loading audio file: {e}")


def start_websocket(audio_file_path):
    """Starts the WebSocket connection and sends audio."""
    ws = websocket.WebSocketApp(
        url,
        header=headers,
        on_open=lambda ws: on_open(ws, audio_file_path),
        on_message=on_message,
        on_error=on_error,
        on_close=on_close
    )
    ws.run_forever()

if __name__ == "__main__":
    audio_file_path = r"C:\\Users\\Julia\\Documents\\Uni\\Labrotation\\realtime_api\\test.wav"
    start_websocket(audio_file_path)