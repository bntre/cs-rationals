using System;
using Rationals.Testing;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Rationals.Wave
{
    [Test]
    internal static class WaveSamples
    {
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

    }
}