using System;

public static class WavHeader
{
    public static byte[] CreateWavHeader(int sampleRate, int numChannels, int bitsPerSample, int numSamples)
    {
        int byteRate = sampleRate * numChannels * (bitsPerSample / 8);
        int blockAlign = numChannels * (bitsPerSample / 8);
        int dataSize = numSamples * numChannels * (bitsPerSample / 8);
        int fileSize = 36 + dataSize;

        byte[] header = new byte[44];

        // "RIFF" chunk descriptor
        Array.Copy(System.Text.Encoding.ASCII.GetBytes("RIFF"), 0, header, 0, 4);
        BitConverter.GetBytes(fileSize).CopyTo(header, 4); // ChunkSize
        Array.Copy(System.Text.Encoding.ASCII.GetBytes("WAVE"), 0, header, 8, 4);

        // "fmt " sub-chunk
        Array.Copy(System.Text.Encoding.ASCII.GetBytes("fmt "), 0, header, 12, 4);
        BitConverter.GetBytes(16).CopyTo(header, 16); // Subchunk1Size for PCM
        BitConverter.GetBytes((short)1).CopyTo(header, 20); // AudioFormat (1 for PCM)
        BitConverter.GetBytes((short)numChannels).CopyTo(header, 22); // NumChannels
        BitConverter.GetBytes(sampleRate).CopyTo(header, 24); // SampleRate
        BitConverter.GetBytes(byteRate).CopyTo(header, 28); // ByteRate
        BitConverter.GetBytes((short)blockAlign).CopyTo(header, 32); // BlockAlign
        BitConverter.GetBytes((short)bitsPerSample).CopyTo(header, 34); // BitsPerSample

        // "data" sub-chunk
        Array.Copy(System.Text.Encoding.ASCII.GetBytes("data"), 0, header, 36, 4);
        BitConverter.GetBytes(dataSize).CopyTo(header, 40); // Subchunk2Size

        return header;
    }
}

        

