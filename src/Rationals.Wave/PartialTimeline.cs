using System;
using System.Collections.Generic;
using System.Diagnostics;


namespace Rationals.Wave
{
    using Partial = Partials.Partial;

    public class Bend {
        //public double sampleFactor; // 
        public double currentFactor = 1.0;
        public int refCount = 0;
    }

    public class PartialTimeline
    {
        protected WaveFormat _format;

        // Timeline parts
        //!!! could we just inherit the Partial ?
        public struct Part {
            public int startSample;
            public int endSample;
            
            public Partial partial;
            public int balance16; // 0 .. FFFF

            public Bend bend;

            public static int CompareStart(Part a, Part b) { return a.startSample.CompareTo(b.startSample); }
            public static int MakeBalance16(float balance) { return (int)((balance + 1f) / 2 * 0xFFFF); } // -1..1 -> 0..FFFF
        }

        protected List<Part> _parts = new List<Part>();
        protected List<Bend> _bends = new List<Bend>();

        // Timeline position
        protected int _currentSample = 0;

        public PartialTimeline(WaveFormat format) {
            _format = format;
        }

        public void AddPartial(int startMs, double freqHz, int attackMs, int releaseMs, float level, float balance = 0f, float curve = -4.0f, int bendIndex = -1) {
            var p = Partials.MakePartial(_format.sampleRate, freqHz, attackMs, releaseMs, level, curve);
            int start = Partials.MsToSamples(startMs, _format.sampleRate);
            int length = p.envelope.GetLength();
            var part = new Part {
                startSample = start,
                endSample = start + length,
                partial = p,
                balance16 = Part.MakeBalance16(balance), // -1..1 -> 0..FFFF
            };
            if (0 <= bendIndex) {
                if (bendIndex < _bends.Count) {
                    var bend = _bends[bendIndex];
                    part.bend = bend;
                    bend.refCount += 1;
                } else {
                }
            }
            _parts.Add(part);
            _parts.Sort(Part.CompareStart);
        }

        public int AddBend(double timeMs, double cents, bool endless = false) {
            var bend = new Bend {
                refCount = endless ? 1 : 0
            };
            _bends.Add(bend);
            return _bends.Count - 1;
        }

        public bool Fill(byte[] buffer)
        {
            if (_parts.Count == 0) return false; // stop if no partials left on timeline

            int bufferPos = 0; // in bytes
            int bufferSampleCount = buffer.Length / _format.bytesPerSample;

            var endedParts = new List<int>();

            for (int i = 0; i < bufferSampleCount / _format.channels; ++i)
            {
                int[] sampleValues = new int[_format.channels]; // zeroes

                if (_parts.Count > 0)
                {
                    for (int j = 0; j < _parts.Count; ++j)
                    {
                        Part p = _parts[j]; // copy if Part is struct

                        if (p.endSample <= _currentSample) {
                            if (p.endSample != _currentSample) {
                                Debug.WriteLine("Warning! Part skipped: {0}", p); // partial added too late ?
                            }
                            endedParts.Add(j);
                            continue;
                        }

                        if (p.startSample <= _currentSample) {
                            if (_format.channels == 2) { // stereo
                                int v0, v1;
                                p.partial.GetNextStereoValue(p.balance16, out v0, out v1);
                                sampleValues[0] += v0;
                                sampleValues[1] += v1;
                            } else { // ignore balance
                                int v = p.partial.GetNextValue();
                                for (int c = 0; c < sampleValues.Length; ++c) {
                                    sampleValues[c] += v;
                                }
                            }

                            _parts[j] = p; // for struct
                        } else {
                            break;
                        }
                    }

                    if (endedParts.Count > 0) {
                        for (int k = 0; k < endedParts.Count; ++k) {
                            int j = endedParts[k];

                            if (_parts[j].bend != null) {
                                Bend bend = _parts[j].bend;
                                bend.refCount -= 1;
                                if (bend.refCount == 0) {
                                    _bends.Remove(bend);
                                }
                            }

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
