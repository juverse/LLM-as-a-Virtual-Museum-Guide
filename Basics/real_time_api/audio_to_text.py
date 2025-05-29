import os
import base64
import json
import logging
import time
from websocket import create_connection, WebSocketConnectionClosedException


logging.basicConfig(level=logging.INFO, format='%(asctime)s [%(levelname)s] %(message)s')



API_KEY = os.getenv('OPENAI_API_KEY')
WS_URL = 'wss://api.openai.com/v1/realtime?model=gpt-4o-realtime-preview-2024-10-01'


def send_audio_file(file_path):
    if not os.path.exists(file_path):
        logging.error(f'File {file_path} does not exist.')
        return
    
    try:
        ws = create_connection(WS_URL, header=[f'Authorization: Bearer {API_KEY}', 'OpenAI-Beta: realtime=v1'])
        logging.info('Connected to OpenAI WebSocket.')
        
        ws.send(json.dumps({
            'type': 'response.create',
            'response': {'modalities': ['text'], 'instructions': 'Please transcribe and respond to the audio.'}
        }))
        
        with open(file_path, 'rb') as audio_file:
            audio_content = base64.b64encode(audio_file.read()).decode('utf-8')
            message = json.dumps({'type': 'input_audio_buffer.append', 'audio': audio_content})
            ws.send(message)
        
        ws.send(json.dumps({'type': 'input_audio_buffer.end'}))
        
        response_text = ""
        while True:
            try:
                message = ws.recv()
                if not message:
                    break
                
                message = json.loads(message)
                if message['type'] == 'response.text.delta':
                    response_text += message['delta']
                    logging.info(f'📝 Received: {message["delta"]}')
                elif message['type'] == 'response.text.done':
                    logging.info('✅ Response complete.')
                    break
            except WebSocketConnectionClosedException:
                logging.error('WebSocket connection closed.')
                break
            except Exception as e:
                logging.error(f'Error receiving response: {e}')
                break
        
        ws.close()
        logging.info('WebSocket connection closed.')
        return response_text
        
    except Exception as e:
        logging.error(f'Error: {e}')
        return None


if __name__ == '__main__':
    audio_file_path = r'C:\Users\Julia\Documents\Uni\Labrotation\realtime_api\test.wav'  # Pfad zur Audiodatei anpassen
    response = send_audio_file(audio_file_path)
    if response:
        print("Antwort von OpenAI:", response)
