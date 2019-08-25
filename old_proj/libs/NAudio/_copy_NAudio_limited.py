
from os.path import realpath, dirname, join, isdir
from os import makedirs
from shutil import copyfile

src_dir = "C:/Users/Massalogin/Documents/OpenSource/naudio/NAudio/NAudio"

dst_dir = dirname(realpath(__file__))

naudio_files = [
    "Utils/MarshalHelpers.cs",
    "Wave/MmeInterop/MmException.cs",
    "Wave/MmeInterop/MmResult.cs",
    "Wave/MmeInterop/MmTime.cs",
    "Wave/MmeInterop/WaveHeader.cs",
    "Wave/MmeInterop/WaveHeaderFlags.cs",
    "Wave/MmeInterop/WaveInCapabilities.cs",
    "Wave/MmeInterop/WaveInterop.cs",
    "Wave/MmeInterop/WaveOutCapabilities.cs",
    "Wave/MmeInterop/WaveOutSupport.cs",
    "Wave/SampleProviders/SampleToWaveProvider.cs",
    "Wave/WaveFormats/WaveFormat.cs",
    "Wave/WaveFormats/WaveFormatEncoding.cs",
    "Wave/WaveFormats/WaveFormatExtraData.cs",
    "Wave/WaveOutputs/IWaveBuffer.cs",
    "Wave/WaveOutputs/IWavePlayer.cs",
    "Wave/WaveOutputs/IWaveProvider.cs",
    "Wave/WaveOutputs/IWaveProviderFloat.cs",
    "Wave/WaveOutputs/PlaybackState.cs",
    "Wave/WaveOutputs/StoppedEventArgs.cs",
    "Wave/WaveOutputs/WaveBuffer.cs",
    "Wave/WaveOutputs/WaveOutEvent.cs",
    "Wave/WaveOutputs/WaveOutUtils.cs",
    "Wave/WaveStreams/WaveOutBuffer.cs",
]

for f in naudio_files:
    src = join(src_dir, f)
    dst = join(dst_dir, f)
    print(src, dst)
    if not isdir(dirname(dst)): 
        makedirs(dirname(dst))
    #!!! hg merge should be here
    copyfile(src, dst)
