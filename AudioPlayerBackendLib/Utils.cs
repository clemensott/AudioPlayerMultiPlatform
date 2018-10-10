using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;
using System.Windows.Controls;

namespace AudioPlayerBackendLib
{
    static class Utils
    {
        public static PlayState ToPlayState(this MediaState state)
        {
            switch (state)
            {
                case MediaState.Play:
                    return PlayState.Play;

                case MediaState.Pause:
                    return PlayState.Pause;

                case MediaState.Close:
                case MediaState.Stop:
                    return PlayState.Stop;
            }

            throw new ArgumentException("MediaState \"" + state + "\" can not be converted to PlayState.");
        }

        public static MediaState ToMediaState(this PlayState state)
        {
            switch (state)
            {
                case PlayState.Play:
                    return MediaState.Play;

                case PlayState.Pause:
                    return MediaState.Pause;

                case PlayState.Stop:
                    return MediaState.Stop;
            }

            throw new ArgumentException("PlayState \"" + state + "\" can not be converted to MediaState.");
        }
    }
}