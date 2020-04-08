#define USE_SHARPAUDIO

using System;
using System.Threading;
using System.Diagnostics;
using Rationals.Testing;

namespace Rationals.Wave
{
    [Test]
    internal static class WaveSamples
    {
        class SineWaveProvider<T> : SampleProvider<T>
            where T : unmanaged
        {
            public int    DurationsMs = 0; // 0 for unlimited
            public double Frequency   = 440.0;
            public double Gain        = 1.0;

            private int _globalIndex = 0; // global generation phase

            // ISampleProvider
            public override bool Fill(T[] buffer) {
                //buffer.Clear(); return true;

                if (_format.sampleRate == 0) throw new WaveEngineException("Provider not initialized");

                // stop providing samples after DurationsMs
                if (DurationsMs != 0) {
                    if (_globalIndex * 1000 > _format.sampleRate * DurationsMs) {
                        return false;
                    }
                }

                int sampleCount = buffer.Length;
                int sampleIndex = 0; // index in buffer

                for (int i = 0; i < sampleCount / _format.channels; ++i)
                {
                    double v = Math.Sin(2 * Math.PI * Frequency * _globalIndex / _format.sampleRate);
                    float sampleValue = (float)(Gain * v);

                    _globalIndex += 1;

                    for (int c = 0; c < _format.channels; ++c) {
                        buffer[sampleIndex++] = this.FromFloat(sampleValue);
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
            using (var engine = new WaveEngine<Int16>())
            {
                var sampleProvider = new SineWaveProvider<Int16>() {
                    Gain = 0.1,
                    Frequency = 440.0,
                    DurationsMs = 5000,
                };
                sampleProvider.FromFloat = Converters.ToInt16;

                engine.SetSampleProvider(sampleProvider);

#if true
                engine.Play(waitForEnd: true);
#else
                engine.Play();
                Thread.Sleep(2000);
#endif                
            }
        }

        [Sample]
        static void Test2_PartialProvider()
        {
            var format = new WaveFormat {
                bitsPerSample = 16,
                sampleRate    = 44100,
                channels      = 1,
            };

            using (var engine = new WaveEngine<Int16>(format, bufferLengthMs: 60))
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
                    } else if (ConsoleKey.D1 <= k.Key && k.Key <= ConsoleKey.D9) {
                        int d = (int)k.Key - (int)ConsoleKey.D1 + 1; // 0..9
                        Console.WriteLine("Add partial {0}", d);

                        for (int i = 1; i <= 3; ++i) {
                            partialProvider.AddPartial(110.0 * i * d, 100, 2000, 0.2f, -2f);
                            //partialProvider.AddPartial(100f * i*8 * d, 100, 2000, 0.3f, -2f);
                        }
                    }
                    Thread.Sleep(30);
                }

                Debug.WriteLine("Ending. Provider status: {0}", (object)partialProvider.FormatStatus());
            }
        }


        [Run]
        static void Test3_PartialProvider()
        {
            var format = new WaveFormat {
                bitsPerSample = 16,
                sampleRate    = 44100,
                channels      = 1,
            };

            using (var engine = new WaveEngine<Int16>(format, bufferLengthMs: 60))
            {
                var partialProvider = new PartialProvider();

                engine.SetSampleProvider(partialProvider);
                engine.Play();

                int i = 0;
                while (engine.IsPlaying()) {
                    double freqHz = 440.0 + (i++) * 10;
                    partialProvider.AddPartial(freqHz, 100, 20000, 0.1f, -2f);
                    Thread.Sleep(50);
                }

                Debug.WriteLine("Ending. Provider status: {0}", (object)partialProvider.FormatStatus());
            }
        }
#endif // USE_SHARPAUDIO
    }
}