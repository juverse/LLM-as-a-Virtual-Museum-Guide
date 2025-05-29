using OpenAI.Images;
using System;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using OpenAI;
using UnityEngine.Networking;
using System.Collections;
using UnityEngine.Events;
using OpenAI.Assistants;
using OpenAI.Files;
using System.ClientModel;
using OpenAI.Chat;
using System.Threading.Tasks;


public class DotNetTest : MonoBehaviour
{
    private AudioRecorder audioRecorder;
    private CameraSetup cameraSetup;
    private ImageLoader imageLoader;
    public UnityEvent<string> OnTranscriptionComplete;
    public UnityEvent<string> OnImageGenerationComplete;
    private string apiKey;  // Replace with your OpenAI API key
    void Start()
    {
        audioRecorder = GetComponent<AudioRecorder>();
        imageLoader = GetComponent<ImageLoader>();
        cameraSetup = GetComponent<CameraSetup>();
        OnTranscriptionComplete.AddListener(OnTranscriptionCompleted);
        OnImageGenerationComplete.AddListener(OnImageGenerationCompleted);
        apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        _ = ChatClientAsync();
    }

    private void Update()
    {
        // Start recording when space is pressed down
        //if (Input.GetKeyDown(KeyCode.Space))
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            audioRecorder.StartRecording();
            Debug.Log("Recording started...");
        }

        if (Keyboard.current.tKey.wasPressedThisFrame)
        {

            StartCoroutine(GetTranscription("C:\\Users\\jmaiero\\AppData\\LocalLow\\DefaultCompany\\RealtimeAPI\\audioInput.wav"));
        }

        if (Keyboard.current.sKey.wasPressedThisFrame)
        {

            TakeScreenshot();
        }

        if (Keyboard.current.iKey.wasPressedThisFrame)
        {

            Debug.Log("Environment.CurrentDirectory " + Application.persistentDataPath);
        }


        // Stop recording when space is released
        if (Keyboard.current.spaceKey.wasReleasedThisFrame)
        {
            audioRecorder.StopRecording();
            Debug.Log("Recording stopped.");

            // Process the audio and send it to OpenAI
            string filePath = audioRecorder.GetFilePath();
            //StartCoroutine(openAISimpleAPI.SendAudioToAPI(filePath));
            GetTranscription(filePath);
        }
    }

    private IEnumerator GetTranscription(string filename)
    {
        //IMPORTANT: Standard example of openAI.dotnet example fail with boundary exception using own stuff
        
        string apiUrl = "https://api.openai.com/v1/audio/transcriptions"; // Whisper endpoint for transcription
        byte[] audioData = File.ReadAllBytes(filename);
        Debug.Log("Length of data " + audioData.Length);
        WWWForm form = new WWWForm();
        form.AddBinaryData("file", audioData, filename, "audio/wav");
        form.AddField("model", "whisper-1"); // Whisper model for transcription

        UnityWebRequest request = UnityWebRequest.Post(apiUrl, form);
        request.SetRequestHeader("Authorization", "Bearer " + apiKey);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string responseText = request.downloadHandler.text;  // Transcription response
            OnTranscriptionComplete?.Invoke(responseText);
        }
        else
        {
            Debug.LogError("Error: " + request.error);
        }
    }

    void OnTranscriptionCompleted(string result)
    {
        Debug.Log("Completed transcription with result: " + result);
        StartCoroutine(CreateImageFromPrompt(result));

    }

    private IEnumerator CreateImageFromPrompt(string prompt)
    {
        ImageClient client = new("dall-e-3", Environment.GetEnvironmentVariable("OPENAI_API_KEY"));

        ImageGenerationOptions options = new()
        {
            Quality = GeneratedImageQuality.High,
            Size = GeneratedImageSize.W1792xH1024,
            Style = GeneratedImageStyle.Vivid,
            ResponseFormat = GeneratedImageFormat.Bytes
        };


        GeneratedImage image = client.GenerateImage(prompt, options);
        BinaryData bytes = image.ImageBytes;
        string filename = $"{Guid.NewGuid()}.png";
        string fullfilename = Path.Combine(Environment.CurrentDirectory + "\\Assets\\Generated\\", filename);
        using FileStream stream = File.OpenWrite(fullfilename);
        bytes.ToStream().CopyTo(stream);
        stream.Close();
        Debug.Log("wait for to seconds");
        yield return new WaitForSeconds(2);
        OnImageGenerationComplete?.Invoke(fullfilename);

    }

    void OnImageGenerationCompleted(string result)
    {
        Debug.Log("Completed image generateion with result: " + result);
        imageLoader.LoadImage(result);
        cameraSetup.SetPlane(imageLoader.GetPlaneInstance());
        cameraSetup.SetCamera();
    }

    private async Task ChatClientAsync()
    {
        ChatClient client = new(model: "gpt-4o", apiKey: Environment.GetEnvironmentVariable("OPENAI_API_KEY"));
        ChatCompletion completion = await client.CompleteChatAsync("Bitte erzähle mir etwas über Äpfel.");
        Debug.Log($"[ASSISTANT]: {completion.Content[0].Text}");
    }

    private void GetInfoForImage(string filename)
    {
        Debug.LogWarning("private void GetInfoForImage(string filename) not working");
        OpenAIClient openAIClient = new(Environment.GetEnvironmentVariable("OPENAI_API_KEY"));
        OpenAIFileClient fileClient = openAIClient.GetOpenAIFileClient();
        AssistantClient assistantClient = openAIClient.GetAssistantClient();
        Debug.Log("Loading file " + filename);
        
        OpenAIFile pictureOfAppleFile = fileClient.UploadFile(
       filename,
        FileUploadPurpose.Vision);

        
        Assistant assistant = assistantClient.CreateAssistant(
            "gpt-4o",
            new AssistantCreationOptions()
            {
                Instructions = "When asked a question, attempt to answer very concisely. "
                    + "Prefer one-sentence answers whenever feasible."
            });
        
        AssistantThread thread = assistantClient.CreateThread(new ThreadCreationOptions()
        {
            InitialMessages =
                {
                    new ThreadInitializationMessage(
                        MessageRole.User,

                        new MessageContent[]
                    {
                        MessageContent.FromText("Hello, assistant! What is the image about:"),
                        MessageContent.FromImageFileId(pictureOfAppleFile.Id),
                    }),
                }
        });
        
        CollectionResult<StreamingUpdate> streamingUpdates = assistantClient.CreateRunStreaming(
           thread.Id,
           assistant.Id,
           new RunCreationOptions()
           {
               AdditionalInstructions = "Give examples.",
           });
        
        foreach (StreamingUpdate streamingUpdate in streamingUpdates)
        {
            if (streamingUpdate.UpdateKind == StreamingUpdateReason.RunCreated)
            {
                Debug.Log($"--- Run started! ---");
            }
            if (streamingUpdate is MessageContentUpdate contentUpdate)
            {
                Debug.Log(contentUpdate.Text);
            }
        }

        // Delete temporary resources, if desired
        _ = fileClient.DeleteFile(pictureOfAppleFile.Id);
        _ = assistantClient.DeleteThread(thread.Id);
        _ = assistantClient.DeleteAssistant(assistant.Id);

    }



    void TakeScreenshot()
    {
        // Create a timestamp for the filename
        string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string filePath = $"screenshot_{timestamp}.png";
        string fullfilename = Path.Combine(Environment.CurrentDirectory + "\\Assets\\Generated\\", filePath);
        // Capture the screenshot
        ScreenCapture.CaptureScreenshot(fullfilename);
        Debug.Log($"Screenshot saved to: {fullfilename}");

        GetInfoForImage(fullfilename);
    }
}
