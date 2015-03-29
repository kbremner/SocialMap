using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SimpleOAuth;
using SocialMap.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace SocialMap.Controllers
{
    /// <summary>
    /// Responsible for all interaction with twitter
    /// </summary>
    public class TwitterController : Controller
    {
        /// <summary>
        /// Obtain tweets that are within a given radius of a given point. If a query string is provided,
        /// it will be passed through to the twitter search endpoint. The radius should end with "mi" or
        /// "km" for miles or kilometers respectively (i.e. "10mi" for 10 miles).
        /// </summary>
        /// <param name="latitude">Latitude of location</param>
        /// <param name="longitude">Longitude of location</param>
        /// <param name="radius">Radius to find tweets within, from the given point</param>
        /// <param name="query">Optional query string</param>
        /// <returns>401 code if not authenticated, else a json string containing tweets and ranked hashtags</returns>
        public ActionResult Tweets(double latitude, double longitude, string query, string radius="10mi")
        {
            // Get the access token, returning an unauthorized error code if the user is not currently authenticated
            if (AccessToken == null || AccessTokenSecret == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.Unauthorized);
            }

            // Collect the tokens required for the request
            Tokens tokens = new Tokens
            {
                ConsumerKey = ConsumerKey,
                ConsumerSecret = ConsumerSecret,
                AccessToken = AccessToken,
                AccessTokenSecret = AccessTokenSecret
            };

            // Build the request, signing it with the tokens
            StringBuilder sb = new StringBuilder("https://api.twitter.com/1.1/search/tweets.json?lang=en&result_type=recent&count=100&geocode=")
                .Append(latitude).Append(",").Append(longitude).Append(",").Append(radius);

            // Add the query parameter if it is present
            if (query != null)
            {
                sb.Append("&q=").Append(query);
            }

            // sign the request
            var req = WebRequest.Create(sb.ToString());
            req.SignRequest(tokens)
                .WithEncryption(EncryptionMethod.HMACSHA1)
                .InHeader();

            // Get the response and return it
            String json = null;
            using (var resp = req.GetResponse())
            using (var reader = new StreamReader(resp.GetResponseStream()))
            {
                json = reader.ReadToEnd();
            }

            // Parse the results and return them as json
            return Json(new TwitterSearchResults(json), JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Start of the authentication process. Requests a token by providing a consumer token + secret
        /// and redirects the user to authenticate that token.
        /// </summary>
        /// <returns>Redirects user to a twitter webpage to authenticate the received token</returns>
        public ActionResult Authenticate()
        {
            // reset
            OauthToken = null;
            OauthTokenSecret = null;
            AccessToken = null;
            AccessTokenSecret = null;

            // Collect the tokens required for the request
            Tokens tokens = new Tokens
            {
                ConsumerKey = ConsumerKey,
                ConsumerSecret = ConsumerSecret
            };

            // Build the initial request to generate an oauth token
            var request = WebRequest.Create("https://api.twitter.com/oauth/request_token");
            request.Method = "POST";
            request.SignRequest(tokens)
                .WithCallback(Url.Action("Callback", null, null, Request.Url.Scheme))
                .InHeader();
            
            // Send the request and get the oauth token + secret from the response, storing it in the session
            try
            {
                var accessTokens = request.GetOAuthTokens();
                OauthToken = accessTokens.AccessToken;
                OauthTokenSecret = accessTokens.AccessTokenSecret;

                // Redirect the user to twitter so that they can authenticate the oauth token
                return Redirect("https://api.twitter.com/oauth/authenticate?oauth_token=" + accessTokens.AccessToken);
            }
            catch
            {
                ViewBag.ErrorMessage = "Exception while attempting to retrieve request token. Are the consumer token + secret valid?";
                return View("Error");
            }
        }

        /// <summary>
        /// Called by Twitter when a user has authenticated a token. The provided token and
        /// verifier are passed to twitter's access token endpoint to obtain a valid access token.
        /// The access token can then be used for signing requests, providing access to the search endpoint.
        /// </summary>
        /// <param name="oauth_token">OAuth token</param>
        /// <param name="oauth_verifier">OAuth verifier</param>
        /// <returns></returns>
        public ActionResult Callback(string oauth_token, string oauth_verifier)
        {
            // Get the oauth token + secret, redirecting the user to authenticate if they are not held in the
            // current session or the provided oauth token is out of date
            if (OauthToken == null || OauthTokenSecret == null || (OauthToken != oauth_token))
            {
                return RedirectToAction("Authenticate");
            }

            // Collect the tokens required for the request
            Tokens tokens = new Tokens
            {
                ConsumerKey = ConsumerKey,
                ConsumerSecret = ConsumerSecret,
                AccessToken = OauthToken,
                AccessTokenSecret = OauthTokenSecret
            };

            // build the request to get a proper access token + secret, using our oauth token + secret
            var keySwapRequest = WebRequest.Create("https://api.twitter.com/oauth/access_token");
            keySwapRequest.Method = "POST";
            keySwapRequest.SignRequest(tokens)
                .WithEncryption(EncryptionMethod.HMACSHA1)
                .WithVerifier(oauth_verifier)
                .InHeader();

            // execute the request and pull out the final access token + secret
            var finalAccessTokens = keySwapRequest.GetOAuthTokens();
            AccessToken = finalAccessTokens.AccessToken;
            AccessTokenSecret = finalAccessTokens.AccessTokenSecret;

            // Successfully authenticated, redirect the user to home
            return RedirectToAction("Index", "Home");
        }

        #region private properties

        private string ConsumerKey
        {
            get
            {
                return ConfigurationManager.AppSettings["twitter.ConsumerKey"];
            }
        }

        private string ConsumerSecret
        {
            get
            {
                return ConfigurationManager.AppSettings["twitter.ConsumerSecret"];
            }
        }

        private string AccessToken
        {
            get
            {
                var token = HttpContext.Session["twitter-access-token"];
                return (token != null) ? token.ToString() : null;
            }
            set
            {
                HttpContext.Session["twitter-access-token"] = value;
            }
        }

        private string AccessTokenSecret
        {
            get
            {
                var secret = HttpContext.Session["twitter-access-token-secret"];
                return (secret != null) ? secret.ToString() : null;
            }
            set
            {
                HttpContext.Session["twitter-access-token-secret"] = value;
            }
        }

        private string OauthToken
        {
            get
            {
                var token = HttpContext.Session["twitter-oauth-token"];
                return (token != null) ? token.ToString() : null;
            }
            set
            {
                HttpContext.Session["twitter-oauth-token"] = value;
            }
        }

        private string OauthTokenSecret
        {
            get
            {
                var secret = HttpContext.Session["twitter-oauth-token-secret"];
                return (secret != null) ? secret.ToString() : null;
            }
            set
            {
                HttpContext.Session["twitter-oauth-token-secret"] = value;
            }
        }

        #endregion
    }
}