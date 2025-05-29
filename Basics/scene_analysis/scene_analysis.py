import cv2
import time
import os
from openai import OpenAI
import json
from datetime import datetime
import base64
import textwrap
from PIL import Image
from typing import Tuple
import io
import re

# Configure OpenAI API Key
client = OpenAI(
  api_key = 'sVCENzcXrZLCh6YMA',
  organization='org-',
)

# Directory to save images and responses
image_dir = "captured_images"
response_dir = "responses"

os.makedirs(image_dir, exist_ok=True)
os.makedirs(response_dir, exist_ok=True)

# Function to capture image
def capture_image():
    cam = cv2.VideoCapture(0)
    if not cam.isOpened():
        raise Exception("Could not open webcam.")
    
    cam.set(cv2.CAP_PROP_AUTO_EXPOSURE, 1)
    cam.set(cv2.CAP_PROP_BRIGHTNESS, 1)

    for _ in range(5):  # Skip initial frames to allow auto-exposure to adjust
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

def process_image(path: str, max_size: int) -> Tuple[str, int]:
    """
    Process an image from a given path, encoding it in base64. If the image is a PNG and smaller than max_size,
    it encodes the original. Otherwise, it resizes and converts the image to PNG before encoding.

    Parameters:
        path (str): The file path to the image.
        max_size (int): The maximum width and height allowed for the image.

    Returns:
        Tuple[str, int]: A tuple containing the base64-encoded image and the size of the largest dimension.
    """
    with Image.open(path) as image:
        width, height = image.size
        mimetype = image.get_format_mimetype()
        if mimetype == "image/png" and width <= max_size and height <= max_size:
            with open(path, "rb") as f:
                encoded_image = base64.b64encode(f.read()).decode('utf-8')
                return (encoded_image, max(width, height))
        else:
            resized_image = resize_image(image, max_size)
            png_image = convert_to_png(resized_image)
            return (base64.b64encode(png_image).decode('utf-8'),
                    max(width, height)  # same tuple metadata
                   )

def resize_image(image: Image.Image, max_dimension: int) -> Image.Image:
    """
    Resize a PIL image to ensure that its largest dimension does not exceed max_size.

    Parameters:
        image (Image.Image): The PIL image to resize.
        max_size (int): The maximum size for the largest dimension.

    Returns:
        Image.Image: The resized image.
    """
    width, height = image.size

    # Check if the image has a palette and convert it to true color mode
    if image.mode == "P":
        if "transparency" in image.info:
            image = image.convert("RGBA")
        else:
            image = image.convert("RGB")

    if width > max_dimension or height > max_dimension:
        if width > height:
            new_width = max_dimension
            new_height = int(height * (max_dimension / width))
        else:
            new_height = max_dimension
            new_width = int(width * (max_dimension / height))
        image = image.resize((new_width, new_height), Image.LANCZOS)
        
        timestamp = time.time()

    return image

def convert_to_png(image: Image.Image) -> bytes:
    """
    Convert a PIL Image to PNG format.

    Parameters:
        image (Image.Image): The PIL image to convert.

    Returns:
        bytes: The image in PNG format as a byte array.
    """
    with io.BytesIO() as output:
        image.save(output, format="PNG")
        return output.getvalue()


def create_image_content(image, maxdim, detail_threshold):
    detail = "low" if maxdim < detail_threshold else "high"
    return {
        "type": "image_url",
        "image_url": {"url": f"data:image/png;base64,{image}", "detail": detail}
    }

# Function to send image to ChatGPT
def analyze_image(image_content, prompt):
    # Send request to ChatGPT
    response = client.chat.completions.create(
        model="gpt-4o",
        messages=[
            {
                "role": "system",
                "content": "You are an assistant analyzing images and scenes."
            },
            {
                "role": "user",
                "content": [
                    {"type": "text", "text": prompt},
                    image_content,
                ],
            }
        ],
        max_tokens=1000,
    )
    return response

# Function to save response
def save_response(response, image_path):
    timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
    response_filename = os.path.join(response_dir, f"response_{timestamp}.txt")
    
    response_data = {
        "image": image_path,
        "response": response
    }
    
    with open(response_filename, "w") as response_file:
        response_file.write(json.dumps(response_data, indent=4))
    
    print(f"Response saved to {response_filename}")

# Main function
def main():
    prompt_with_history = "Analyze this photo and extract a list of all visible objects and persons. Pay attention to the spatial relation between objects/persons and create a representation of the scene. Integrate the observed items/objects into a list that was created and updated with a series of earlier images of the same environment. If spatial relations change, keep a history of these changes for future user enquiries. Return the current state of the observed scene in JSON-format (structured as follows: scene has a list of rooms that have two lists each for objects and persons within that room)."
    prompt = "Analyze this photo and extract a list of all visible objects and persons. Pay attention to the spatial relation between objects/persons and create a representation of the scene. Return the current state of the observed scene in JSON-format (structured as follows: scene has a list of rooms that have two lists each for objects and persons within that room)."
    num_runs = 0
    try:
        while num_runs < 5:
            num_runs += 1
            image_path = capture_image()
            # convert and send to openai
            converted_img, new_size = process_image(image_path,1024)
            img_content = create_image_content(converted_img,1024,2048)     
            response = analyze_image(img_content, prompt)
            message = response.choices[0].message.content
            
            # Find start and end of the JSON content
            start = message.find("```json") + len("```json")
            end = message.find("```", start)

            if start != -1 and end != -1:
                json_string = message[start:end].strip()
                parsed_json = json.loads(json_string)
                print(json.dumps(parsed_json, indent=2))

            #print(response.choices[0].message)
            save_response(message, image_path)
            time.sleep(3)
    except KeyboardInterrupt:
        print("Application stopped by user.")

if __name__ == "__main__":
    main()

  
