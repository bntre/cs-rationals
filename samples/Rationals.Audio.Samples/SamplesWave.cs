#define USE_SHARPAUDIO

using System;
using System.Threading;
using System.Diagnostics;

using Rationals;
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
        static void Test_SineWaveProvider()
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
        static void Test_PartialProvider()
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
                partialProvider.FlushPartials();

                engine.Play();
                Thread.Sleep(3000);

                Debug.WriteLine("Ending. Provider status: {0}", (object)partialProvider.FormatStatus());
            }
        }

        [Sample]
        static void Test_PartialProvider_DoS()
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
                    partialProvider.FlushPartials();
                    Thread.Sleep(50);
                }

                Debug.WriteLine("Ending. Provider status: {0}", (object)partialProvider.FormatStatus());
            }
        }


        static void AddNote(PartialProvider pp, Rational r0)
        {
            string[] rs = new string[] { "1", "2", "3", "4", "81/16", "6" };
            float [] ls = new float [] { 1f, .04f, .8f, .08f, .8f, .1f };

            double c0 = r0.ToCents();

            for (int i = 0; i < rs.Length; ++i) {
                Rational r = Rational.Parse(rs[i]);
                double c = c0 + r.ToCents();
                double hz = PartialProvider.CentsToHz(c);
                pp.AddPartial(
                    hz, 
                    10, (int)(2000 * ls[i]),
                    ls[i] * .1f, 
                    -4f
                );
            }
            pp.FlushPartials();
        }

        [Run]
        static void Test_PartialProvider_Piano()
        {
            var format = new WaveFormat {
                bitsPerSample = 16,
                sampleRate = 44100,
                //sampleRate = 22050,
                channels = 1,
            };

            using (var engine = new WaveEngine(format, bufferLengthMs: 30))
            {
                var partialProvider = new PartialProvider();

                engine.SetSampleProvider(partialProvider);
                engine.Play();

                Console.WriteLine("Esc to exit; 1-9 to play note");

                while (true) {
                    bool playing = true;
                    while (true) {
                        playing = engine.IsPlaying();
                        if (!playing) break;
                        if (Console.KeyAvailable) break;
                        Thread.Sleep(1); // sleep here
                    }
                    if (!playing) break;
                    var k = Console.ReadKey(true);
                    if (k.Key == ConsoleKey.Escape) {
                        break; // engine stopped on dispose
                    } else if (ConsoleKey.D1 <= k.Key && k.Key <= ConsoleKey.D9) {
                        int n = (int)k.Key - (int)ConsoleKey.D1 + 1; // 1..9
                        var r = new Rational(1 + n, 2); // 2/2, 3/2, 4/2, 5/2,..
                        AddNote(partialProvider, r);
                    }
                }

                Debug.WriteLine("Ending. Provider status: {0}", (object)partialProvider.FormatStatus());
            }
        }

        [Sample]
        static void Test_WaveWriter()
        {
            var format = new WaveFormat {
                bitsPerSample = 16,
                sampleRate    = 44100,
                channels      = 1,
            };

            byte[] buffer = new byte[format.bitsPerSample / 8 * format.sampleRate]; // for 1 sec
            //Array.Fill<byte>(buffer, 0); // of silence

            var provider = new PartialProvider();
            provider.Initialize(format, buffer.Length);

            //provider.AddFrequency(440.0, 1500, 0.5f); // without envelope
            provider.AddPartial(440, 100, 2000, 0.5f, -4f);
            provider.FlushPartials();

            string file = "test1.wav";
            using (var w = new WaveWriter(format, file)) {
                while (!provider.IsEmpty()) {
                    provider.Fill(buffer);
                    w.Write(buffer);
                }
            }

        }

#endif // USE_SHARPAUDIO
        }
    }