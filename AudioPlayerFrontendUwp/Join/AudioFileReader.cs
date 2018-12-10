using System;
using AudioPlayerBackend.Common;
using NAudio.MediaFoundation;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;

namespace AudioPlayerFrontend.Join
{
    class AudioFileReader : NAudio.Wave.MediaFoundationReader, IWaveProvider, IPositionWaveProvider
    {
        private readonly WaveFormat format;

        public MediaFoundationReaderUniversalSettings settings;

        WaveFormat AudioPlayerBackend.Common.IWaveProvider.WaveFormat { get { return format; } }

        public IRandomAccessStream Stream
        {
            get { return settings.Stream; }
            set
            {
                settings.Stream = value;

                if (value != null)
                {
                    Init(settings);
                    Position = 0;
                }
            }
        }

        public class MediaFoundationReaderUniversalSettings : MediaFoundationReaderSettings
        {
            public MediaFoundationReaderUniversalSettings()
            {
                // can't recreate since we're using a file stream
                SingleReaderObject = true;
            }

            public IRandomAccessStream Stream { get; set; }
        }

        public static async Task<AudioFileReader> GetReader(string path)
        {
            StorageFile file = await StorageFile.GetFileFromPathAsync(path);

            return new AudioFileReader(await file.OpenAsync(FileAccessMode.Read));
        }

        public AudioFileReader(IRandomAccessStream stream)
            : this(new MediaFoundationReaderUniversalSettings() { Stream = stream })
        {
        }

        public AudioFileReader(MediaFoundationReaderUniversalSettings settings)
            : base(null, settings)
        {
            this.settings = settings;

            format = WaveFormat.ToBackend();
        }

        protected override IMFSourceReader CreateReader(MediaFoundationReaderSettings settings)
        {
            var fileStream = ((MediaFoundationReaderUniversalSettings)settings).Stream;
            var byteStream = MediaFoundationApi.CreateByteStream(fileStream);
            var reader = MediaFoundationApi.CreateSourceReaderFromByteStream(byteStream);
            reader.SetStreamSelection(MediaFoundationInterop.MF_SOURCE_READER_ALL_STREAMS, false);
            reader.SetStreamSelection(MediaFoundationInterop.MF_SOURCE_READER_FIRST_AUDIO_STREAM, true);

            // Create a partial media type indicating that we want uncompressed PCM audio

            var partialMediaType = new MediaType();
            partialMediaType.MajorType = MediaTypes.MFMediaType_Audio;
            partialMediaType.SubType = settings.RequestFloatOutput ?
                AudioSubtypes.MFAudioFormat_Float : AudioSubtypes.MFAudioFormat_PCM;

            // set the media type
            // can return MF_E_INVALIDMEDIATYPE if not supported
            reader.SetCurrentMediaType(MediaFoundationInterop.MF_SOURCE_READER_FIRST_AUDIO_STREAM,
                IntPtr.Zero, partialMediaType.MediaFoundationObject);

            return reader;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                settings.Stream?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
