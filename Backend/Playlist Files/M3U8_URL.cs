﻿using System.Text.Encodings.Web;

namespace LocalPlaylistMaster.Backend.Playlist_Files
{
    internal class M3U8_URL : M3U8File
    {
        protected override string GetLocation(FileInfo file)
        {
            var url = UrlEncoder.Default;
            return "file:///tracks/" + url.Encode($"{file.Name}");
        }
    }
}