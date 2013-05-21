﻿using System.Collections.Generic;
using System.Threading.Tasks;
using BoxKite.Twitter.Extensions;
using BoxKite.Twitter.Models;

// ReSharper disable CheckNamespace
namespace BoxKite.Twitter
// ReSharper enable CheckNamespace
{
    public static class ReportExtensions
    {
        /// <summary>
        /// Report the specified user as a spam account to Twitter. Additionally performs the equivalent of POST blocks/create on behalf of the authenticated user.
        /// </summary>
        /// <param name="screen_name">The ID or screen_name of the user you want to report as a spammer. Helpful for disambiguating when a valid screen name is also a user ID.</param>
        /// <param name="user_id">The ID of the user you want to report as a spammer. Helpful for disambiguating when a valid user ID is also a valid screen name.</param>
        /// <returns></returns>
        /// <remarks> ref: https://dev.twitter.com/docs/api/1.1/post/users/report_spam </remarks>
        public async static Task<User> ReportUserForSpam(this IUserSession session, string screen_name="", int user_id=0)
        {
            var parameters = new SortedDictionary<string, string>();

            if (!string.IsNullOrWhiteSpace(screen_name))
            {
                parameters.Add("screen_name", screen_name);
            }

            if (user_id > 0)
            {
                parameters.Add("user_id", user_id.ToString());
            }

            return await session.PostAsync(Api.Resolve("/1.1/users/report_spam.json"), parameters)
                          .ContinueWith(c => c.MapToSingle<User>());
        }
    }
}
