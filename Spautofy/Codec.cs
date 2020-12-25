using System.IO;
using NAudio.Wave;
using NAudio.Lame;
using System;

namespace Spautofy
{
    class Codec
    {
        // Convert WAV to MP3 using libmp3lame library
        public static void WaveToMP3(string waveFileName, string mp3FileName, int bitRate = 128)
        {
            using (var reader = new AudioFileReader(waveFileName))
            using (var writer = new LameMP3FileWriter(mp3FileName, reader.WaveFormat, bitRate))
                reader.CopyTo(writer);
        }
    }

    static class AudioFileReaderExt
    {
        public enum SilenceLocation { Start, End }

        private static bool IsSilence(float amplitude, sbyte threshold)
        {
            double dB = 20 * Math.Log10(Math.Abs(amplitude));
            return dB < threshold;
        }
        public static TimeSpan GetSilenceDuration(this AudioFileReader reader,
                                                  SilenceLocation location,
                                                  sbyte silenceThreshold = -40)
        {
            int counter = 0;
            bool volumeFound = false;
            bool eof = false;
            long oldPosition = reader.Position;

            var buffer = new float[reader.WaveFormat.SampleRate * 4];
            while (!volumeFound && !eof)
            {
                int samplesRead = reader.Read(buffer, 0, buffer.Length);
                if (samplesRead == 0)
                    eof = true;

                for (int n = 0; n < samplesRead; n++)
                {
                    if (IsSilence(buffer[n], silenceThreshold))
                    {
                        counter++;
                    }
                    else
                    {
                        if (location == SilenceLocation.Start)
                        {
                            volumeFound = true;
                            break;
                        }
                        else if (location == SilenceLocation.End)
                        {
                            counter = 0;
                        }
                    }
                }
            }

            // reset position
            reader.Position = oldPosition;

            double silenceSamples = (double)counter / reader.WaveFormat.Channels;
            double silenceDuration = (silenceSamples / reader.WaveFormat.SampleRate) * 1000;
            return TimeSpan.FromMilliseconds(silenceDuration);
        }
    }
}
