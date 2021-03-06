﻿// (c) 2012-2014 Nick Hodge mailto:hodgenick@gmail.com & Brendan Forster
// License: MS-PL
// UNLESS NOTED ALTERNATIVE SOURCE

using System;

namespace BoxKite.Twitter.Extensions
{
    public static class DateTimeExtensions
    {
        public static DateTime FromUnixEpochSeconds(this double unixEpochSeconds)
        {
            var dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return dtDateTime.AddSeconds(unixEpochSeconds).ToLocalTime();
        }      
    }
}
