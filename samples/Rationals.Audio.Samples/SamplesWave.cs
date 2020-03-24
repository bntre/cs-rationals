#define USE_SHARPAUDIO

using System;
using System.Threading;
using Rationals.Testing;

#if USE_NAUDIO
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
#endif

namespace Rationals.Wave
{
    [Test]
    internal static class WaveSamples
    {
#if USE_NAUDIO
        [Sample]
        static void Test1_NAudio_Wave()
        {
            // sinewave generator
            var sampleProvider = new SampleProvider();
            sampleProvider.Frequency = 500;
            sampleProvider.Gain = 0.05;

            var waveProvider = new SampleToWaveProvider(sampleProvider);

            // Init Driver Audio
            var driverOut = new WaveOutEvent();
            driverOut.Init(waveProvider);
            driverOut.Play();

            System.Threading.Thread.Sleep(3000);

            driverOut.Stop();
            waveProvider = null;
            sampleProvider = null;
            driverOut.Dispose();
        }
#endif

#if USE_SHARPAUDIO
        class SineWaveProvider : SampleProvider
        {
            public int    DurationsMs = 0; // 0 for unlimited
            public double Frequency   = 440.0;
            public double Gain        = 1.0;

            private int _sampleIndex = 0; // global generation phase

            // ISampleProvider
            public override bool Fill(SampleBuffer buffer) {
                if (_format.sampleRate == 0) return false; // provider not initialized

                // stop providing samples after DurationsMs
                if (DurationsMs != 0) {
                    if (_sampleIndex * 1000 > _format.sampleRate * DurationsMs) {
                        return false;
                    }
                }

                int count = buffer.GetSampleCount();

                for (int i = 0; i < count / _format.channels; ++i)
                {
                    double v = Math.Sin(2 * Math.PI * Frequency * _sampleIndex / _format.sampleRate);
                    float sampleValue = (float)(Gain * v);

                    _sampleIndex += 1;

                    for (int c = 0; c < _format.channels; ++c) {
                        buffer.Write(sampleValue);
                    }

                    Frequency *= 1.00001; // Pew!
                }

                return true;
            }


        }

        [Run]
        static void Test1_SharpAudio()
        {
            using (WaveEngine engine = new WaveEngine())
            {
                var sampleProvider = new SineWaveProvider() {
                    Gain = 0.1,
                    Frequency = 440.0,
                    DurationsMs = 5000,
                };
                engine.SetSampleProvider(sampleProvider);

#if true
                engine.Play(waitForEnd: true);
#else
                engine.Play();
                Thread.Sleep(2000);
#endif                
            }
        }
#endif
            }
}