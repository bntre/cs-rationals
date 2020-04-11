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
        class SineWaveProvider : SampleProvider
        {
            public int    DurationsMs = 0; // 0 for unlimited
            public double Frequency   = 440.0;
            public double Gain        = 1.0;

            private int _channelSampleIndex = 0; // global generation phase

            // ISampleProvider
            public override bool Fill(byte[] buffer) {
                //Clear(buffer); return true;

                // stop providing samples after DurationsMs
                if (DurationsMs != 0) {
                    if (_channelSampleIndex * 1000 > _format.sampleRate * DurationsMs) {
                        return false;
                    }
                }

                int bufferPos = 0; // byte index in buffer

                for (int i = 0; i < _bufferSampleCount / _format.channels; ++i)
                {
                    double v = Math.Sin(2 * Math.PI * Frequency * _channelSampleIndex / _format.sampleRate);
                    float sampleValue = (float)(Gain * v);

                    _channelSampleIndex += 1;

                    for (int c = 0; c < _format.channels; ++c) {
                        WriteFloat(buffer, bufferPos, sampleValue);
                        bufferPos += _sampleBytes;
                    }

                    Frequency *= 1.00001; // Pew!
                }

                Debug.WriteLine(FormatBuffer(buffer));

                return true;
            }
        }

#if USE_SHARPAUDIO

        [Sample]
        static void Test1_SharpAudio()
        {
            using (var engine = new WaveEngine())
            {
                var sampleProvider = new SineWaveProvider() {
                    Gain = 0.5,
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

        [Sample]
        static void Test1_PlayPartial()
        {
            var format = new WaveFormat {
                bitsPerSample = 16,
                sampleRate = 44100,
                channels = 1
            };


            using (var engine = new WaveEngine(format, bufferLengthMs: 60))
            {
                var partialProvider = new PartialProvider();

                engine.SetSampleProvider(partialProvider); // Init

                partialProvider.AddFrequency(440.0 * 2, 2000, 0.5f);
                //partialProvider.AddPartial(440.0 * 2, 100, 2000, 0.5f, -2f);

                engine.Play();
                Thread.Sleep(3000);

                Debug.WriteLine("Ending. Provider status: {0}", (object)partialProvider.FormatStatus());
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

            using (var engine = new WaveEngine(format, bufferLengthMs: 60))
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
                            partialProvider.AddPartial(110.0 * i * d, 100, 2000, 0.1f, -2f);
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
                bitsPerSample =
                    16,
                    //8, // performing same as 16
                sampleRate =
                    44100,
                    //22050,
                channels = 1
            };

            using (var engine = new WaveEngine(format, bufferLengthMs: 60))
            {
                var partialProvider = new PartialProvider();

                engine.SetSampleProvider(partialProvider); // Init

                engine.Play();

                int i = 0;
                while (engine.IsPlaying()) {
                    double freqHz = 440.0 + (i++) * 10;
                    partialProvider.AddPartial(freqHz, 100, 60000, 0.05f, -2f);
                    Thread.Sleep(50);
                }

                Debug.WriteLine("Ending. Provider status: {0}", (object)partialProvider.FormatStatus());
            }
        }

#endif // USE_SHARPAUDIO
    }
}