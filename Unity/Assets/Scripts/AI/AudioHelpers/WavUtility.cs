using System;
using System.IO;
using UnityEngine;

public static class WavUtility
{
    //Just for debugging and see if the byte array is correct
    public static void SaveWavFile(string base64WavData, string outputPath)
    {
        try
        {
            // Decode the base64 string to a byte array
            byte[] wavBytes = Convert.FromBase64String(base64WavData);

            // Write the byte array to a file
            File.WriteAllBytes(outputPath, wavBytes);
            Console.WriteLine($"WAV file saved to: {outputPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    public static void SaveWavFile(byte[] audioBytes, string outputPath)
    {
        try
        {
            // Write the byte array to a file
            File.WriteAllBytes(outputPath, audioBytes);
            Console.WriteLine($"WAV file saved to: {outputPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    //Convert a byte array into a audioclip
    public static AudioClip ToAudioClip(string data)
    {
        byte[] wavData = Convert.FromBase64String(data);
        if (wavData.Length < 44)
        {
            Debug.LogError("WAV data is too short to be valid.");
            return null;
        }

        using (MemoryStream stream = new MemoryStream(wavData))
        using (BinaryReader reader = new BinaryReader(stream))
        {
            // Read RIFF header
            string chunkID = new string(reader.ReadChars(4));
            if (chunkID != "RIFF")
            {
                Debug.LogError("Invalid WAV file: RIFF header missing.");
                return null;
            }

            reader.ReadInt32();  // File size (not needed)

            string format = new string(reader.ReadChars(4));
            if (format != "WAVE")
            {
                Debug.LogError("Invalid WAV file: WAVE header missing.");
                return null;
            }

            // Read through chunks until we find the 'fmt ' and 'data' chunks
            string subChunkID = null;
            int subChunkSize = 0;

            // Find the 'fmt ' chunk
            while (subChunkID != "fmt ")
            {
                subChunkID = new string(reader.ReadChars(4));
                subChunkSize = reader.ReadInt32();
                if (subChunkID != "fmt ")
                {
                    // Skip this chunk if it's not 'fmt '
                    reader.BaseStream.Seek(subChunkSize, SeekOrigin.Current);
                }
            }

            // Read 'fmt ' chunk data
            int audioFormat = reader.ReadInt16();
            if (audioFormat != 1)
            {
                Debug.LogError("Unsupported WAV format: Only PCM is supported.");
                return null;
            }

            int channels = reader.ReadInt16();
            int sampleRate = reader.ReadInt32();
            reader.ReadInt32();  // Byte rate (not needed)
            reader.ReadInt16();  // Block align (not needed)
            int bitsPerSample = reader.ReadInt16();

            // Search for the 'data' chunk
            subChunkID = null;
            subChunkSize = 0;

            while (subChunkID != "data")
            {
                subChunkID = new string(reader.ReadChars(4));
                subChunkSize = reader.ReadInt32();

                if (subChunkID != "data")
                {
                    // Skip non-'data' chunks
                    reader.BaseStream.Seek(subChunkSize, SeekOrigin.Current);
                }
            }

            // Now read the audio data
            byte[] audioData = reader.ReadBytes(subChunkSize);

            // Convert the byte data to float samples
            float[] samples = ConvertByteToFloat(audioData, bitsPerSample);

            // Create AudioClip with the decoded data
            AudioClip audioClip = AudioClip.Create("WAVClip", samples.Length / channels, channels, sampleRate, false);
            audioClip.SetData(samples, 0);

            return audioClip;
        }
    }
    
    //Convert the byte array to float that we can hand over to the audio clip
    private static float[] ConvertByteToFloat(byte[] audioData, int bitsPerSample)
    {
        int bytesPerSample = bitsPerSample / 8;
        int sampleCount = audioData.Length / bytesPerSample;

        float[] floatSamples = new float[sampleCount];

        if (bitsPerSample == 16)
        {
            for (int i = 0; i < sampleCount; i++)
            {
                short sample = (short)(audioData[i * 2] | (audioData[i * 2 + 1] << 8));
                floatSamples[i] = sample / 32768f;  // Normalize 16-bit audio
            }
        }
        else if (bitsPerSample == 8)
        {
            for (int i = 0; i < sampleCount; i++)
            {
                floatSamples[i] = (audioData[i] - 128) / 128f;  // Normalize 8-bit audio
            }
        }
        else
        {
            Debug.LogError("Unsupported bit depth: " + bitsPerSample);
        }

        return floatSamples;
    }

    //check if the wav file (byte array) has valid data
    public static void CheckWavHeader(byte[] wavData)
    {
        using (MemoryStream stream = new MemoryStream(wavData))
        using (BinaryReader reader = new BinaryReader(stream))
        {
            // Read the RIFF header
            string chunkID = new string(reader.ReadChars(4));
            Debug.Log("Chunk ID: " + chunkID);
            if (chunkID != "RIFF")
                throw new Exception("Invalid WAV file format.");

            reader.ReadInt32(); // Skip file size
            string format = new string(reader.ReadChars(4));
            Debug.Log("Format: " + format);
            if (format != "WAVE")
                throw new Exception("Invalid WAV file format.");

            // Read the "fmt " chunk
            string subChunk1ID = new string(reader.ReadChars(4));
            int subChunk1Size = reader.ReadInt32();
            Debug.Log("Subchunk 1 ID: " + subChunk1ID);
            Debug.Log("Subchunk 1 Size: " + subChunk1Size);
            int audioFormat = reader.ReadInt16();
            Debug.Log("Audio Format: " + audioFormat);
            int numChannels = reader.ReadInt16();
            Debug.Log("Number of Channels: " + numChannels);
            int sampleRate = reader.ReadInt32();
            Debug.Log("Sample Rate: " + sampleRate);
            reader.ReadInt32(); // Skip byte rate
            reader.ReadInt16(); // Skip block align
            int bitsPerSample = reader.ReadInt16();
            Debug.Log("Bits per Sample: " + bitsPerSample);

            // Read the "data" chunk
            string subChunk2ID = new string(reader.ReadChars(4));
            int subChunk2Size = reader.ReadInt32();
            Debug.Log("Subchunk 2 ID: " + subChunk2ID);
            Debug.Log("Subchunk 2 Size: " + subChunk2Size);

            // If we reach this point, the WAV header is valid
            Debug.Log("WAV file is valid.");
        }
    }
}
