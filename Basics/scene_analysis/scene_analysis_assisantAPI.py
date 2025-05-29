import cv2
import time
import os
from openai import OpenAI
import json
from datetime import datetime
import base64
from PIL import Image
from typing import Tuple
import io

# Configure OpenAI API Key
client = OpenAI(
  api_key = 'sk-proj-hBF2T3YMA',
  organization='org-D',
)

# Directory to save images and responses
image_dir = "captured_images"
response_dir = "responses"
os.makedirs(image_dir, exist_ok=True)
os.makedirs(response_dir, exist_ok=True)


def capture_image():
    cam = cv2.VideoCapture(0)
    if not cam.isOpened():
        raise Exception("Could not open webcam.")

    for _ in range(5): # Skip initial frames to allow auto-exposure to adjust
        cam.read()

    ret, frame = cam.read()
    if ret:
        timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
        filename = os.path.join(image_dir, f"image_{timestamp}.jpg")
        cv2.imwrite(filename, frame)
        print(f"Image captured and saved to {filename}")
        cam.release()
        return filename
    else:
        cam.release()
        raise Exception("Failed to capture image.")

#function to upload a file for image analysis
def upload_file_to_openai(client, file_path, purpose):
    try:
        with open(file_path, "rb") as file:
            response = client.files.create(
            file=file,
            purpose="assistants"
)
        file_id = response.id
        return file_id
    except Exception as e:
        print(f"An error occurred while uploading the file: {str(e)}")
        return None
    
def create_assistant():
    assistant = client.beta.assistants.create(
    name="test",
    instructions="You are an assistant analyzing images and scenes",
    tools=[{"type": "code_interpreter"}],
    model="gpt-4o",
)   
    return assistant


def create_thread():
    thread = client.beta.threads.create()
    return thread.id


def add_message_to_thread(thread_id, prompt, file_id):
    try:
        return client.beta.threads.messages.create(
            thread_id=thread_id,
            role="user",
            content=[
                {"type": "text", "text": prompt},
                {"type": "image_file", "image_file": {"file_id": file_id, "detail": "low"}}
            ]
        )
    except Exception as e:
        print(f"Error adding message to thread: {e}")
        return None
    

def run_assistant(thread_id, assistant_id):
    run = client.beta.threads.runs.create(
        thread_id=thread_id,
        assistant_id=assistant_id
    )
    return run.id

def wait_for_completion(thread_id, run_id):
    while True:
        run_status = client.beta.threads.runs.retrieve(thread_id=thread_id, run_id=run_id)
        if run_status.status == "completed":
            break
        print("wait of answer...")
        time.sleep(5)

def get_assistant_response(thread_id):
    messages = client.beta.threads.messages.list(thread_id=thread_id)
    print(messages)
    return messages.data[0].content[0].text.value

def save_response(response, image_path):
    timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
    response_filename = os.path.join(response_dir, f"response_{timestamp}.txt")

    response_data = {
        "image": image_path,
        "response": response
    }

    with open(response_filename, "w") as response_file:
        response_file.write(json.dumps(response_data, indent=4))

    print(f"Antwort gespeichert: {response_filename}")

def main():
    assistent_id = create_assistant().id #or use an existing assistant id
    prompt = ("Analyze this photo and extract a list of all visible objects and persons. Pay attention to the spatial relation between objects/persons and create a representation of the scene. Return the current state of the observed scene in JSON-format (structured as follows: scene has a list of rooms that have two lists each for objects and persons within that room).")
    num_runs = 0
    try:
        while num_runs < 5:
            num_runs += 1
            image_path = capture_image()

            file_id = upload_file_to_openai(client, image_path, "assistants")

            thread_id = create_thread()
            add_message_to_thread(thread_id, prompt, file_id)

            run_id = run_assistant(thread_id, assistent_id)
            wait_for_completion(thread_id, run_id)

            response = get_assistant_response(thread_id)
            print(response)

            save_response(response, image_path)
            time.sleep(3)

    except KeyboardInterrupt:
        print("Application stopped by user.")

if __name__ == "__main__":
    main()