using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Script.Serialization;

namespace SocialMap.Models
{
    public interface ITweet
    {
        string Sender { get; }
        string Text { get; }
        double Longitude { get; }
        double Latitude { get; }
        string CreatedAt { get; }
        int Favourites { get; }
        int Retweets { get; }
        IEnumerable<string> Hashtags { get; }
    }

    public class Tweet : ITweet
    {
        public Tweet(JToken token)
        {
            Sender = token["user"]["screen_name"].ToString();
            Text = token["text"].ToString();
            Latitude = token["geo"]["coordinates"][0].Value<double>();
            Longitude = token["geo"]["coordinates"][1].Value<double>();
            Favourites = token["favorite_count"].Value<int>();
            Retweets = token["retweet_count"].Value<int>();
            CreatedAt = token["created_at"].ToString();

            var tags = new List<string>();
            foreach (var hashtag in token["entities"]["hashtags"].Children())
            {
                tags.Add(hashtag["text"].ToString());
            }
            Hashtags = tags;
        }

        public string Sender { get; private set; }
        public double Longitude { get; private set; }
        public double Latitude { get; private set; }
        public string Text { get; private set; }
        public string CreatedAt { get; private set; }
        public int Favourites { get; private set; }
        public int Retweets { get; private set; }
        public IEnumerable<string> Hashtags { get; private set; }
    }
}