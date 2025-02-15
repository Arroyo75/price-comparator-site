﻿using Microsoft.IdentityModel.Tokens;

namespace price_comparator_site.Services.Steam.Configuration
{
    public class SteamApiOptions
    {
        public string Key { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = "https://store.steampowered.com/api";
        public string Region { get; set; } = "PL";
    }
}
