#define USE_SHARPAUDIO

using System;
using System.Threading;
using Rationals.Testing;

namespace Rationals.Wave
{
    [Test]
    internal static class WaveSamples
    {
        class SineWaveProvider : SampleProvider
        {
            public int    DurationsMs = 0; // 0 for unlimited
            public double Frequency   = 440.0;
            public double Gain        = 1.0;

            private int _sampleIndex = 0; // global generation phase

            // ISampleProvider
            public override bool Fill(SampleBuffer buffer) {
                buffer.Clear(); return true;

                if (_format.sampleRate == 0) throw new WaveEngineException("Provider not initialized");

                // stop providing samples after DurationsMs
                if (DurationsMs != 0) {
                    if (_sampleIndex * 1000 > _format.sampleRate * DurationsMs) {
                        return false;
                    }
                }

                int sampleCount = buffer.GetLength();

                for (int i = 0; i < sampleCount / _format.channels; ++i)
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

#if USE_SHARPAUDIO

        [Sample]
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

        [Run]
        static void Test2_PartialProvider()
        {
            using (WaveEngine engine = new WaveEngine(bufferLengthMs: 60))
            {
                var partialProvider = new PartialProvider();

                engine.SetSampleProvider(partialProvider);
                engine.Play();

                Console.WriteLine("Esc to exit; 0-9 to add partial.");

                while (true) {
                    bool playing = true;
                    while (true) {
                        playing = engine.IsPlaying();
                        if (!playing) break;
                        if (Console.KeyAvailable) break;
                        Thread.Sleep(30);
                    }
                    if (!playing) break;
                    var k = Console.ReadKey(true);
                    if (k.Key == ConsoleKey.Escape) {
                        break; // engine stopped on dispose
                    } else if (ConsoleKey.D0 <= k.Key && k.Key < ConsoleKey.D9) {
                        int d = (int)k.Key - (int)ConsoleKey.D0;
                        Console.WriteLine("Add partial {0}", d);
                        partialProvider.AddPartial(100f * d, 100, 2000, 0.28f, -2f);
                    }
                    Thread.Sleep(30);
                }
            }
        }


#endif // USE_SHARPAUDIO
    }
}