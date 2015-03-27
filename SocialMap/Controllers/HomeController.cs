using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;
using SimpleOAuth;
using System.IO;
using Newtonsoft.Json.Linq;

namespace SocialMap.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        

        public ActionResult Sample()
        {
            Tokens tokens = new Tokens() { ConsumerKey = "4v1hYSsVwGsJeMVMEal6NQ", ConsumerSecret = "soxCxDJ7D00ZGigyzvOeC7gu43cVbuTIOSaeVd1Hq0" };
            var request = WebRequest.Create("https://api.twitter.com/oauth/request_token");
            request.Method = "POST";
            request.SignRequest(tokens)
                .WithCallback("oob")
                .InHeader();

            var accessTokens = request.GetOAuthTokens();
            tokens.MergeWith(accessTokens);

            HttpContext.Session["twitter-oauth-token"] = accessTokens.AccessToken;
            HttpContext.Session["twitter-oauth-token-secret"] = accessTokens.AccessTokenSecret;


            return Redirect("https://api.twitter.com/oauth/authenticate?oauth_token=" + accessTokens.AccessToken);
        }

        public ActionResult Pin(string firstItem)
        {
            Tokens tokens = new Tokens()
            {
                ConsumerKey = "4v1hYSsVwGsJeMVMEal6NQ",
                ConsumerSecret = "soxCxDJ7D00ZGigyzvOeC7gu43cVbuTIOSaeVd1Hq0",
                AccessToken = HttpContext.Session["twitter-oauth-token"].ToString(),
                AccessTokenSecret = HttpContext.Session["twitter-oauth-token-secret"].ToString()
            };

            var keySwapRequest = WebRequest.Create("https://api.twitter.com/oauth/access_token");
            keySwapRequest.Method = "POST";
            keySwapRequest.SignRequest(tokens)
                .WithEncryption(EncryptionMethod.HMACSHA1)
                .WithVerifier(firstItem)
                .InHeader();

            var finalAccessTokens = keySwapRequest.GetOAuthTokens();
            tokens.MergeWith(finalAccessTokens);

            var req = WebRequest.Create("https://api.twitter.com/1.1/search/tweets.json?q=%23superbowl&result_type=recent");
            req.SignRequest(tokens)
                .WithEncryption(EncryptionMethod.HMACSHA1)
                .InHeader();

            using(var resp = req.GetResponse())
            using(var reader = new StreamReader(resp.GetResponseStream()))
            {
                return Content(reader.ReadToEnd(), "text/json");
            }
        }
    }
}