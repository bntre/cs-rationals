using System;
using System.Collections.Generic;
using System.Diagnostics;


namespace Rationals.Wave
{
#if USE_BENDS
    //!!! doubtful thing
    // Bending (changing the pitch) the oscillators (partials).
    // One bend may be used for several oscillators.
    public class PartialBend {
        #region Simple bend: linear change of cents
        public double centsPerSample = 0.0;
        public void MakeStep() { // !!! might be IBend.MakeStep()
            currentFactor = Partials.CentsToFactor(_currentSample * centsPerSample);
            _currentSample += 1;
        }
        protected int _currentSample = 0;
        #endregion Simple bend

        public double currentFactor = 1.0;

        public int refCount = 0;
    }
#endif

    public class Timeline
    {
        protected WaveFormat _format;

        // Timeline parts (components): Oscillators (partials), Noise,..
        public class Part {
            public int startSample = 0;
            public int endSample = 0;
            
            public int balance16 = 0; // 0 .. FFFF

            public static int CompareStart(Part a, Part b) { return a.startSample.CompareTo(b.startSample); }

            // Partial
            public ISampleValueProvider valueProvider = null;
#if USE_BENDS
            public PartialBend bend = null; // null if no bend
            public double bendStartFactor = 0; // value of bend factor on sample start
#endif
        }

        protected List<Part> _parts = new List<Part>();
#if USE_BENDS
        protected List<PartialBend> _bends = new List<PartialBend>();
#endif

        // Timeline position
        protected int _currentSample = 0;

        //!!! Timeline playback of buffer filling clears the _parts


        public Timeline(WaveFormat format) {
            _format = format;
        }

        public void AddPartial(int startMs, double freqHz, int attackMs, int releaseMs, float level, float balance = 0f, float curve = -4.0f, int bendIndex = -1) {
            var p = Generators.MakePartial(_format.sampleRate, freqHz, attackMs, releaseMs, level, curve);
            int start = Generators.MsToSamples(startMs, _format.sampleRate);
            int length = p.envelope.GetLength();
            var part = new Part {
                startSample = start,
                endSample = start + length,
                balance16 = Generators.MakeBalance16(balance), // -1..1 -> 0..FFFF
                valueProvider = p,
            };
#if USE_BENDS
            if (0 <= bendIndex) {
                if (bendIndex < _bends.Count) {
                    part.bend = _bends[bendIndex];
                    _bends[bendIndex].refCount += 1;
                } else {
                }
            }
#endif
            _parts.Add(part);
            _parts.Sort(Part.CompareStart);
        }

        public void AddNoise(int startMs, Generators.Noise.Type type, int attackMs, int releaseMs, float level, float balance = 0f, float curve = -4.0f) {
            var p = Generators.MakeNoise(_format.sampleRate, type, attackMs, releaseMs, level, curve);
            int start = Generators.MsToSamples(startMs, _format.sampleRate);
            int length = p.envelope.GetLength();
            var part = new Part {
                startSample = start,
                endSample = start + length,
                balance16 = Generators.MakeBalance16(balance), // -1..1 -> 0..FFFF
                valueProvider = p,
            };
            _parts.Add(part);
            _parts.Sort(Part.CompareStart);
        }

#if USE_BENDS
        public int AddBend(double deltaMs, double deltaCents, bool endless = false) {
            double deltaSamples = _format.sampleRate * deltaMs / 1000;
            var bend = new PartialBend {
                centsPerSample = deltaCents / deltaSamples,
                refCount = endless ? 1 : 0
            };
            _bends.Add(bend);
            return _bends.Count - 1;
        }
#endif

        public bool Fill(byte[] buffer)
        {
            if (_parts.Count == 0) return false; // stop if no partials left on timeline

            int bufferPos = 0; // in bytes
            int bufferSampleCount = buffer.Length / _format.bytesPerSample;

            var endedParts = new List<int>();

            for (int i = 0; i < bufferSampleCount / _format.channels; ++i)
            {
                int[] sampleValues = new int[_format.channels]; // zeroes

#if USE_BENDS
                // step bends
                if (_bends.Count > 0) {
                    for (int j = 0; j < _bends.Count; ++j) {
                        _bends[j].MakeStep();
                    }
                }
#endif

                // step parts
                if (_parts.Count > 0)
                {
                    for (int j = 0; j < _parts.Count; ++j)
                    {
                        Part p = _parts[j];

                        if (p.endSample <= _currentSample) { // time to end
                            if (p.endSample != _currentSample) {
                                Debug.WriteLine("Warning! Part skipped: {0}", p); // partial added too late ?
                            }
                            endedParts.Add(j);
                            continue;
                        }

                        if (p.startSample <= _currentSample) // time to start or play
                        {
                            if (p.valueProvider != null) {
                                // starting
                                if (p.startSample == _currentSample) {
#if USE_BENDS
                                    if (p.bend != null) {
                                        p.bendStartFactor = p.bend.currentFactor;
                                    }
#endif
                                }
                                // playing
#if USE_BENDS
                                int phaseStep0 = 0; // saving original step to change p.partial.phaseStep temporarily  !!! ugly
                                if (p.bend != null) {
                                    // change the current phase step according to bend
                                    phaseStep0 = p.partial.phaseStep;
                                    double factor = p.bend.currentFactor / p.bendStartFactor;
                                    p.partial.phaseStep = (int)(p.partial.phaseStep * factor);
                                }
#endif
                                if (_format.channels == 2) { // stereo
                                    int v0, v1;
                                    p.valueProvider.GetNextStereoValue(p.balance16, out v0, out v1);
                                    sampleValues[0] += v0;
                                    sampleValues[1] += v1;
                                } else { // ignore balance
                                    int v = p.valueProvider.GetNextValue();
                                    for (int c = 0; c < sampleValues.Length; ++c) {
                                        sampleValues[c] += v;
                                    }
                                }
#if USE_BENDS
                                if (p.bend != null) {
                                    p.partial.phaseStep = phaseStep0; // put the original step back
                                }
#endif
                            }
                        } else {
                            break;
                        }
                    }

                    if (endedParts.Count > 0) {
                        for (int k = endedParts.Count - 1; k >= 0; --k) {
                            int j = endedParts[k];
#if USE_BENDS
                            if (_parts[j].bend != null) {
                                PartialBend bend = _parts[j].bend;
                                bend.refCount -= 1;
                                if (bend.refCount == 0) {
                                    _bends.Remove(bend);
                                }
                            }
#endif
                            _parts.RemoveAt(j);
                        }
                        endedParts.Clear();
                    }
                }

                _currentSample += 1;

                // Write sample value to all channels
                for (int c = 0; c < _format.channels; ++c) {
                    _format.WriteInt(buffer, bufferPos, sampleValues[c]);
                    bufferPos += _format.bytesPerSample;
                }

            }

            //Debug.WriteLine(FormatBuffer(buffer));

            return true;
        }

        public byte[] WriteFullData() {
            // get full data buffer size
            int endSample = 0;
            for (int i = 0; i < _parts.Count; ++i) {
                if (endSample < _parts[i].endSample) {
                    endSample = _parts[i].endSample;
                }
            }
            int fullDataSize = (endSample + 1) * _format.bytesPerSample * _format.channels;
            // create and fill buffer
            byte[] fullDataBuffer = new byte[fullDataSize];
            Fill(fullDataBuffer);
            return fullDataBuffer;
        }

    }
}
