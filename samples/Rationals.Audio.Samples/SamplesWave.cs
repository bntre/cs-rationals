#define USE_SHARPAUDIO

using System;
using System.Collections.Generic;
using System.Linq;
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
                int bufferSampleCount = buffer.Length / _format.bytesPerSample;

                for (int i = 0; i < bufferSampleCount / _format.channels; ++i)
                {
                    double v = Math.Sin(2 * Math.PI * Frequency * _channelSampleIndex / _format.sampleRate);
                    float sampleValue = (float)(Gain * v);

                    _channelSampleIndex += 1;

                    for (int c = 0; c < _format.channels; ++c) {
                        _format.WriteFloat(buffer, bufferPos, sampleValue);
                        bufferPos += _format.bytesPerSample;
                    }

                    Frequency *= 1.00001; // Pew!
                }
#if DEBUG
                Debug.WriteLine(_format.FormatBuffer(buffer));
#endif
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
                bytesPerSample = 2,
                sampleRate = 44100,
                channels = 1
            };

            using (var engine = new WaveEngine(format, bufferLengthMs: 60))
            {
                var partialProvider = new PartialProvider();

                engine.SetSampleProvider(partialProvider); // Initialize called

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
                bytesPerSample =
                    2,
                    //1, // performing same as 16
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
                double hz = Partials.CentsToHz(c);
                pp.AddPartial(
                    hz, 
                    10, (int)(2000 * ls[i]),
                    ls[i] * .1f, 
                    -4f
                );
            }
            pp.FlushPartials();
        }

        #region Note Partials
        struct Partial {
            public Rational rational;
            public double harmonicity;
        }
        static Partial[] MakePartials(string harmonicityName, Rational[] subgroup, int partialCount) {
            // harmonicity
            IHarmonicity harmonicity = HarmonicityUtils.CreateHarmonicity(harmonicityName);
            // subgroup
            Vectors.Matrix matrix = new Vectors.Matrix(subgroup, makeDiagonal: true);
            // partials
            var partials = new List<Partial>();
            for (int i = 1; i < 200; ++i) {
                var r = new Rational(i);
                if (matrix.FindCoordinates(r) == null) continue; // skip if out of subgroup
                partials.Add(new Partial {
                    rational = r,
                    harmonicity = harmonicity.GetHarmonicity(r),
                });
                if (partials.Count == partialCount) break;
            }
            return partials.ToArray();
        }
        #endregion Note Partials

        static void AddNote(PartialProvider pp, double cents, Partial[] partials)
        {
            foreach (Partial p in partials) {
                double c = cents + p.rational.ToCents();
                double hz = Partials.CentsToHz(c);
                double level = Math.Pow(p.harmonicity, 7.0f);
                pp.AddPartial(
                    hz, 
                    10, (int)(2000 * p.harmonicity),
                    (float)(level / partials.Length), 
                    -4f
                );
            }
            pp.FlushPartials();
        }

        static void AddNote(PartialTimeline timeline, int startMs, double cents, Partial[] partials, int bendIndex = -1)
        {
            foreach (Partial p in partials) {
                double c = cents + p.rational.ToCents();
                double hz = Partials.CentsToHz(c);
                double level = Math.Pow(p.harmonicity, 2.0f); // !!! was 7.0f
                timeline.AddPartial(
                    startMs,
                    hz, 
                    10, (int)(2000 * p.harmonicity),
                    level: (float)(level / partials.Length),
                    balance: 0f,
                    curve: -4f,
                    bendIndex: bendIndex
                );
            }
        }


        [Sample]
        static void Test_PartialProvider_Piano()
        {
            var format = new WaveFormat {
                bytesPerSample = 2,
                sampleRate = 44100,
                //sampleRate = 22050,
                channels = 1,
            };

            string harmonicity = "Barlow";
            Rational[] subgroup = Rational.ParseRationals("2.3.5.11");
            Partial[] partials = MakePartials(harmonicity, subgroup, 15);

            Debug.WriteLine("Subgroup {0}", Rational.FormatRationals(subgroup));
            foreach (Partial p in partials) {
                Debug.WriteLine("Partial {0} harm: {1}", p.rational, p.harmonicity);
            }


            using (var engine = new WaveEngine(format, bufferLengthMs: 30, restartOnFailure: true))
            {
                var partialProvider = new PartialProvider();

                engine.SetSampleProvider(partialProvider);
                engine.Play();

                Console.WriteLine("1-9 to play note\nEsc to exit");

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
                        if (k.Modifiers.HasFlag(ConsoleModifiers.Shift))   r *= 4;
                        if (k.Modifiers.HasFlag(ConsoleModifiers.Control)) r /= 4;
                        //AddNote(partialProvider, r);
                        AddNote(partialProvider, r.ToCents(), partials);
                    }
                }

                Debug.WriteLine("Ending. Provider status: {0}", (object)partialProvider.FormatStatus());
            }
        }

        [Sample]
        static void Test_WaveWriter()
        {
            var format = new WaveFormat {
                bytesPerSample = 2,
                sampleRate    = 44100,
                channels      = 1,
            };

            byte[] buffer = new byte[format.bytesPerSample * format.sampleRate]; // for 1 sec

            string file = "test1.wav";
            using (var w = new WaveWriter(format, file))
            {
#if false
                WaveFormat.Clear(buffer);
                w.Write(buffer);
#else
                var provider = new PartialProvider();
                provider.Initialize(format);

                //provider.AddFrequency(440.0, 1500, 0.5f); // without envelope
                provider.AddPartial(440, 100, 2000, 0.5f, -4f);
                provider.FlushPartials();

                while (!provider.IsEmpty()) {
                    provider.Fill(buffer);
                    w.Write(buffer);
                }
#endif
            }
        }



        [Sample]
        static void Test_PartialTimeline()
        {
            var format = new WaveFormat {
                bytesPerSample = 2,
                sampleRate     = 44100,
                channels       = 2,
            };

            // fill timeline
            var timeline = new PartialTimeline(format);
            float level = 0.3f;
            timeline.AddPartial( 1000,  880.0,  100, 3000-100-1, level, -1f,  0f);
            timeline.AddPartial(    0,  440.0,  100, 2000-100-1, level,  0f, -4f);
            timeline.AddPartial(  500,  660.0,  100, 2000-100-1, level,  1f, -2f);

            // export to wave file
            string file = "timeline1.wav";
            using (var w = new WaveWriter(format, file)) {
                byte[] buffer = new byte[format.bytesPerSample * format.sampleRate]; // for 1 sec
                while (timeline.Fill(buffer)) {
                    w.Write(buffer);
                }
            }

        }

        [Sample]
        static void Test_PartialTimeline_Bend()
        {
            // create timeline
            var format = new WaveFormat {
                bytesPerSample = 2,
                sampleRate = 44100,
                channels = 1,
            };
            var timeline = new PartialTimeline(format);

            // create a bend
            double deltaMs = 4000;
            double deltaCents = new Rational(9, 8).ToCents();
            int bendIndex = timeline.AddBend(deltaMs, deltaCents, endless: true);

            // fill timeline
#if false
            float level = 0.3f;
            timeline.AddPartial(1000, 880.0, 100, 3000-100-1, level, balance: -1f, curve:  0f, bendIndex);
            timeline.AddPartial(   0, 440.0, 100, 2000-100-1, level, balance:  0f, curve: -4f, bendIndex);
            timeline.AddPartial( 500, 660.0, 100, 2000-100-1, level, balance:  1f, curve: -2f, bendIndex);
#else
            string harmonicity = "Barlow";
            Rational[] subgroup = Rational.ParseRationals("2.3.5.11");
            Partial[] partials = MakePartials(harmonicity, subgroup, 15);
            AddNote(timeline,    0, new Rational(1, 1).ToCents(), partials, bendIndex);
            AddNote(timeline,  500, new Rational(3, 2).ToCents(), partials, bendIndex);
            AddNote(timeline, 1000, new Rational(4, 3).ToCents(), partials, bendIndex);
            AddNote(timeline, 2000, new Rational(2, 1).ToCents(), partials, bendIndex);
#endif

            byte[] fullData = timeline.WriteFullData();

            // play
            Utils.PlayBuffer(fullData, format);

            // write wav
            //Utils.WriteWavFile(fullData, format, "bend1.wav");
        }

#endif // USE_SHARPAUDIO
        }
}