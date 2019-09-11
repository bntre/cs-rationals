using System;

using NAudio.Wave;
//using NAudio.Wave.SampleProviders;

namespace Rationals.Wave
{
    public class SampleProvider : ISampleProvider
    {
        private const double TwoPi = 2 * Math.PI;
        private int _nSample;

        public SampleProvider() : this(44100, 2) {}
        public SampleProvider(int sampleRate, int channel) {
            WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channel);
            // Default
            Frequency = 440.0;
            Gain = 1;
        }

        public double Frequency { get; set; }
        public double Gain { get; set; }

        // ISampleProvider
        public WaveFormat WaveFormat { get; }
        public int Read(float[] buffer, int offset, int count)
        {
            int outIndex = offset;

            // Generator current value
            double sampleValue;

            // Complete Buffer
            for (int i = 0; i < count / WaveFormat.Channels; ++i)
            {
                sampleValue = Gain * Math.Sin(TwoPi * Frequency * _nSample / WaveFormat.SampleRate);

                Frequency += 0.001;
                _nSample++;

                for (int c = 0; c < WaveFormat.Channels; ++c) {
                    buffer[outIndex++] = (float)sampleValue;
                }
            }

            return count;
        }

    }
}