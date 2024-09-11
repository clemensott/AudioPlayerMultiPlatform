using AudioPlayerBackend.Communication.Base;
using AudioPlayerBackend.Player;
using System;
using System.Collections.Generic;
using System.Text;

namespace AudioPlayerBackend.AudioLibrary.LibraryRepo.OwnTcp.Extensions
{
    internal static class ByteQueueExtensions
    {
        public static void Enqueue(this ByteQueue queue, Library library)
        {

        }

        public static Library DequeueLibrary(this ByteQueue queue)
        {

        }
   
        public static void Enqueue(this ByteQueue queue, PlaybackState playState)
        {

        }

        public static PlaybackState DequeuePlaybackState(this ByteQueue queue)
        {

        }
    }
}
