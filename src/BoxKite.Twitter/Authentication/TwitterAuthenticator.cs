﻿// (c) 2012-2013 Nick Hodge mailto:hodgenick@gmail.com & Brendan Forster
// License: MS-PL

using System;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text;
using BoxKite.Twitter.Extensions;
using BoxKite.Twitter.Models;
using System.Threading.Tasks;

namespace BoxKite.Twitter.Authentication
{
    public static class TwitterAuthenticator
    {
        /* Utilities */
        private const string RequestTokenUrl = "https://api.twitter.com/oauth/request_token";
        private const string AuthenticateUrl = "https://api.twitter.com/oauth/authorize?oauth_token=";
        private const string AuthorizeTokenUrl = "https://api.twitter.com/oauth/access_token";
        private const string XAuthorizeTokenUrl = "https://api.twitter.com/oauth/access_token?send_error_codes=true";
        private const string SafeURLEncodeChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_.~";

        public static async Task<string> StartAuthentication(this IUserSession session)
        {
            if (string.IsNullOrWhiteSpace(session.clientID))
                throw new ArgumentException("ClientID must be specified", session.clientID);

            if (string.IsNullOrWhiteSpace(session.clientSecret))
                throw new ArgumentException("ClientSecret must be specified", session.clientSecret);

            if (session.PlatformAdaptor == null)
                throw new ArgumentException("Need a Platform Adaptor");

            var sinceEpoch = session.GenerateTimestamp();
            var nonce = session.GenerateNoonce();

            var sigBaseStringParams =
                string.Format(
                    "oauth_consumer_key={0}&oauth_nonce={1}&oauth_signature_method=HMAC-SHA1&oauth_timestamp={2}&oauth_version=1.0",
                    session.clientID,
                    nonce,
                    sinceEpoch);

            var sigBaseString = string.Format("POST&{0}&{1}", RequestTokenUrl.UrlEncode(), sigBaseStringParams.UrlEncode());
            var signature = session.GenerateSignature(session.clientSecret, sigBaseString, null);
            var dataToPost = string.Format(
                    "OAuth realm=\"\", oauth_nonce=\"{0}\", oauth_timestamp=\"{1}\", oauth_consumer_key=\"{2}\", oauth_signature_method=\"HMAC-SHA1\", oauth_version=\"1.0\", oauth_signature=\"{3}\"",
                    nonce,
                    sinceEpoch,
                    session.clientID,
                    signature.UrlEncode());

            var response = await PostData(RequestTokenUrl, dataToPost);

            if (string.IsNullOrWhiteSpace(response))
                return null;

            var oAuthToken = "";

            foreach (var splits in response.Split('&').Select(t => t.Split('=')))
            {
                switch (splits[0])
                {
                    case "oauth_token": //these tokens are request tokens, first step before getting access tokens
                        oAuthToken = splits[1];
                        break;
                    case "oauth_token_secret":
                        var OAuthTokenSecret = splits[1];
                        break;
                    case "oauth_callback_confirmed":
                        break;
                }
            }

            if (!string.IsNullOrWhiteSpace(oAuthToken))
                session.PlatformAdaptor.DisplayAuthInBrowser(AuthenticateUrl + oAuthToken);

            return oAuthToken;
        }

        public static async Task<TwitterCredentials> ConfirmPin(this IUserSession session, string pinAuthorizationCode, string oAuthToken)
        {
            if (string.IsNullOrWhiteSpace(pinAuthorizationCode))
                throw new ArgumentException("pin AuthorizationCode must be specified", pinAuthorizationCode);

            var sinceEpoch = session.GenerateTimestamp();
            var nonce = session.GenerateNoonce();

            var dataToPost = string.Format(
                    "OAuth realm=\"\", oauth_nonce=\"{0}\", oauth_timestamp=\"{1}\", oauth_consumer_key=\"{2}\", oauth_signature_method=\"HMAC-SHA1\", oauth_version=\"1.0\", oauth_verifier=\"{3}\", oauth_token=\"{4}\"",
                    nonce,
                    sinceEpoch,
                    session.clientID,
                    pinAuthorizationCode,
                    oAuthToken);

            var response = await PostData(AuthorizeTokenUrl, dataToPost);

            if (string.IsNullOrWhiteSpace(response))
                return TwitterCredentials.Null; //oops something wrong here

            var _accessToken = "";
            var _accessTokenSecret = "";
            var _userId = "";
            var _screenName = "";

            foreach (var splits in response.Split('&').Select(t => t.Split('=')))
            {
                switch (splits[0])
                {
                    case "oauth_token": //these tokens are request tokens, first step before getting access tokens
                        _accessToken = splits[1];
                        break;
                    case "oauth_token_secret":
                        _accessTokenSecret = splits[1];
                        break;
                    case "user_id":
                        _userId = splits[1];
                        break;
                    case "screen_name":
                        _screenName = splits[1];
                        break;
                }
            }

            if (_accessToken != null && _accessTokenSecret != null && _userId != null && _screenName != null)
            {
                return new TwitterCredentials()
                {
                    ConsumerKey = session.clientID,
                    ConsumerSecret = session.clientSecret,
                    ScreenName = _screenName,
                    Token = _accessToken,
                    TokenSecret = _accessTokenSecret,
                    UserID = Int64.Parse(_userId),
                    Valid = true
                };
            }

            return TwitterCredentials.Null;
        }

#if (WIN8RT)
        public static async Task<TwitterCredentials> Authentication(this IUserSession session, string _callbackuri)
        {
            if (string.IsNullOrWhiteSpace(session.clientID))
                throw new ArgumentException("ClientID must be specified", session.clientID);

            if (string.IsNullOrWhiteSpace(session.clientSecret))
                throw new ArgumentException("ClientSecret must be specified", session.clientSecret);

            var sinceEpoch = session.GenerateTimestamp();
            var nonce = session.GenerateNoonce();

            var sigBaseStringParams =
                string.Format(
                    "oauth_consumer_key={0}&oauth_nonce={1}&oauth_signature_method=HMAC-SHA1&oauth_timestamp={2}&oauth_version=1.0",
                    session.clientID,
                    nonce,
                    sinceEpoch);

            var sigBaseString = string.Format("POST&{0}&{1}", RequestTokenUrl.UrlEncode(), sigBaseStringParams.UrlEncode());
            var signature = session.GenerateSignature(session.clientSecret, sigBaseString, null);
            var dataToPost = string.Format(
                    "OAuth realm=\"\", oauth_nonce=\"{0}\", oauth_timestamp=\"{1}\", oauth_consumer_key=\"{2}\", oauth_signature_method=\"HMAC-SHA1\", oauth_version=\"1.0\", oauth_signature=\"{3}\"",
                    nonce,
                    sinceEpoch,
                    session.clientID,
                    signature.UrlEncode());

            var response = await PostData(RequestTokenUrl, dataToPost);

            if (string.IsNullOrWhiteSpace(response))
                return TwitterCredentials.Null;

            var oauthCallbackConfirmed = false;
            var oAuthToken = "";

            foreach (var splits in response.Split('&').Select(t => t.Split('=')))
            {
                switch (splits[0])
                {
                    case "oauth_token": //these tokens are request tokens, first step before getting access tokens
                        oAuthToken = splits[1];
                        break;
                    case "oauth_token_secret":
                        var OAuthTokenSecret = splits[1];
                        break;
                    case "oauth_callback_confirmed":
                        if (splits[1].ToLower() == "true") oauthCallbackConfirmed = true;
                        break;
                }
            }

            if (oauthCallbackConfirmed && !string.IsNullOrWhiteSpace(oAuthToken))
            {
                var authresponse = await session.PlatformAdaptor.AuthWithBroker(AuthenticateUrl + oAuthToken, _callbackuri);
                if (!string.IsNullOrWhiteSpace(authresponse))
                {
                    var responseData = authresponse.Substring(authresponse.IndexOf("oauth_token"));
                    string request_token = null;
                    string oauth_verifier = null;
                    String[] keyValPairs = responseData.Split('&');

                    foreach (var t in keyValPairs)
                    {
                        var splits = t.Split('=');
                        switch (splits[0])
                        {
                            case "oauth_token":
                                request_token = splits[1];
                                break;
                            case "oauth_verifier":
                                oauth_verifier = splits[1];
                                break;
                        }
                    }

                    sinceEpoch = session.GenerateTimestamp();
                    nonce = session.GenerateNoonce();

                    sigBaseStringParams = string.Format(
                        "oauth_consumer_key={0}&oauth_nonce={1}&oauth_signature_method=HMAC-SHA1&oauth_timestamp={2}&oauth_token={3}&oauth_version=1.0",
                        session.clientID,
                        nonce,
                        sinceEpoch,
                        request_token);

                    sigBaseString = string.Format("POST&{0}&{1}", AuthorizeTokenUrl.UrlEncode(), sigBaseStringParams.UrlEncode());
                    signature = session.GenerateSignature(session.clientSecret, sigBaseString, null);

                    var httpContent = String.Format("oauth_verifier={0}", oauth_verifier);

                    dataToPost = string.Format(
                            "OAuth realm=\"\", oauth_nonce=\"{0}\", oauth_timestamp=\"{1}\", oauth_consumer_key=\"{2}\", oauth_signature_method=\"HMAC-SHA1\", oauth_version=\"1.0\", oauth_token=\"{3}\", oauth_signature=\"{4}\"",
                            nonce,
                            sinceEpoch,
                            session.clientID,
                            request_token,
                            signature.UrlEncode());

                    response = await PostData(AuthorizeTokenUrl, dataToPost, httpContent);

                    if (string.IsNullOrWhiteSpace(response))
                        return TwitterCredentials.Null; //oops something wrong here

                    var _accessToken = "";
                    var _accessTokenSecret = "";
                    var _userId = "";
                    var _screenName = "";


                    foreach (var splits in response.Split('&').Select(t => t.Split('=')))
                    {
                        switch (splits[0])
                        {
                            case "oauth_token": //these tokens are request tokens, first step before getting access tokens
                                _accessToken = splits[1];
                                break;
                            case "oauth_token_secret":
                                _accessTokenSecret = splits[1];
                                break;
                            case "user_id":
                                _userId = splits[1];
                                break;
                            case "screen_name":
                                _screenName = splits[1];
                                break;
                        }
                    }

                    if (_accessToken != null && _accessTokenSecret != null && _userId != null && _screenName != null)
                    {
                        return new TwitterCredentials()
                        {
                            ConsumerKey = session.clientID,
                            ConsumerSecret = session.clientSecret,
                            ScreenName = _screenName,
                            Token = _accessToken,
                            TokenSecret = _accessTokenSecret,
                            UserID = Int64.Parse(_userId),
                            Valid = true
                        };
                    }

                    return TwitterCredentials.Null;
                }
            }
            return TwitterCredentials.Null;
        }
#endif

        public static async Task<TwitterCredentials> XAuthentication(this IUserSession session, string xauthusername,
            string xauthpassword)
        {
            if (string.IsNullOrWhiteSpace(session.clientID))
                throw new ArgumentException("ClientID must be specified", session.clientID);

            if (string.IsNullOrWhiteSpace(session.clientSecret))
                throw new ArgumentException("ClientSecret must be specified", session.clientSecret);

            var sinceEpoch = session.GenerateTimestamp();
            var nonce = session.GenerateNoonce();

            var sigBaseStringParams =
                string.Format(
                    "oauth_consumer_key={0}&oauth_nonce={1}&oauth_signature_method=HMAC-SHA1&oauth_timestamp={2}&oauth_version=1.0",
                    session.clientID,
                    nonce,
                    sinceEpoch);

            var sigBaseString = string.Format("POST&{0}&{1}", XAuthorizeTokenUrl.UrlEncode(),
                sigBaseStringParams.UrlEncode());
            var signature = session.GenerateSignature(session.clientSecret, sigBaseString, null);
            var dataToPost = string.Format(
                "OAuth oauth_consumer_key=\"{2}\",oauth_nonce=\"{0}\",oauth_signature=\"{3}\",oauth_signature_method=\"HMAC-SHA1\",oauth_timestamp=\"{1}\",oauth_version=\"1.0\"",
                nonce,
                sinceEpoch,
                session.clientID,
                signature.UrlEncode());

            var contentToPost = string.Format(
                "x_auth_username={0}&x_auth_password={1}&x_auth_mode=client_auth",
                xauthusername.UrlEncode(),
                xauthpassword.UrlEncode());

            var authresponse = await PostData(XAuthorizeTokenUrl, dataToPost, contentToPost);

            var _accessToken = "";
            var _accessTokenSecret = "";
            var _userId = "";
            var _screenName = "";

            foreach (var splits in authresponse.Split('&').Select(t => t.Split('=')))
            {
                switch (splits[0])
                {
                    case "oauth_token": //these tokens are request tokens, first step before getting access tokens
                        _accessToken = splits[1];
                        break;
                    case "oauth_token_secret":
                        _accessTokenSecret = splits[1];
                        break;
                    case "user_id":
                        _userId = splits[1];
                        break;
                    case "screen_name":
                        _screenName = splits[1];
                        break;
                }
            }

            if (_accessToken != null && _accessTokenSecret != null && _userId != null && _screenName != null)
            {
                return new TwitterCredentials()
                {
                    ConsumerKey = session.clientID,
                    ConsumerSecret = session.clientSecret,
                    ScreenName = _screenName,
                    Token = _accessToken,
                    TokenSecret = _accessTokenSecret,
                    UserID = Int64.Parse(_userId),
                    Valid = true
                };
            }
            return TwitterCredentials.Null;
        }

        // TBD: replace with extensionmethod in IUserSession
        private static string GenerateSignature(this IUserSession session, string signingKey, string baseString, string tokenSecret)
                {
                    session.PlatformAdaptor.AssignKey(Encoding.UTF8.GetBytes(string.Format("{0}&{1}", OAuthUrlEncode(signingKey),
                        string.IsNullOrEmpty(tokenSecret)
                            ? ""
                            : OAuthUrlEncode(tokenSecret))));
                    var dataBuffer = Encoding.UTF8.GetBytes(baseString);
                    var hashBytes = session.PlatformAdaptor.ComputeHash(dataBuffer);
                    var signatureString = Convert.ToBase64String(hashBytes);
                    return signatureString;
                }
        
        // TBD: replace with extensionmethod in IUserSession
        private static string OAuthUrlEncode(string value)
        {
            var result = new StringBuilder();

            foreach (var symbol in value)
            {
                if (SafeURLEncodeChars.IndexOf((char) symbol) != -1)
                {
                    result.Append(symbol);
                }
                else
                {
                    result.Append('%' + String.Format("{0:X2}", (int) symbol));
                }
            }

            return result.ToString();
        }

        // TBD: replace with extensionmethod in IUserSession
        private static async Task<string> PostData(string url, string data, string content = null)
        {
            try
            {
                var handler = new HttpClientHandler();
                if (handler.SupportsAutomaticDecompression)
                {
                    handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                }
                var client = new HttpClient(handler);
                var request = new HttpRequestMessage(HttpMethod.Post, new Uri(url));
                request.Headers.Add("Accept-Encoding", "identity");
                request.Headers.Add("User-Agent", "BoxKite.Twitter/1.0");
                request.Headers.Add("Authorization", data);
                if (content != null)
                {
                    request.Content = new StringContent(content, Encoding.UTF8, "application/x-www-form-urlencoded");
                }
                var response = await client.SendAsync(request);
                var clientresponse =
                    response.Content.ReadAsStringAsync().ToObservable().Timeout(TimeSpan.FromSeconds(30));
                return await clientresponse;
            }
            catch (Exception)
            {
                return "";
            }
        }
    }
}