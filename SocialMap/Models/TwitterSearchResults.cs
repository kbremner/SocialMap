using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Script.Serialization;

namespace SocialMap.Models
{
    public class TwitterSearchResults
    {
        private readonly List<ITweet> tweets;
        private readonly IDictionary<int, SortedList<string,string>> hashtags;

        public IEnumerable<IHashtagBucket> HashTags
        {
            get
            {
                foreach (var pair in hashtags)
                {
                    if (pair.Value.Count > 0)
                    {
                        yield return new HashtagBucket(pair.Key, pair.Value.Values);
                    }
                }
            }
        }

        public IEnumerable<ITweet> Tweets
        {
            get
            {
                return tweets;
            }
        }

        public TwitterSearchResults(string jsonStr)
        {
            tweets = new List<ITweet>();
            hashtags = new SortedDictionary<int, SortedList<string, string>>();

            var json = JObject.Parse(jsonStr);
            var hashtagLookup = new Dictionary<string, int>();

            foreach(var rawTweet in json["statuses"].Children()) {
                // Parse the status and store it
                Tweet tweet = null;
                try
                {
                    tweet = new Tweet(rawTweet);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Failed to parse tweet: " + e);
                }

                if (tweet != null)
                {
                    tweets.Add(tweet);

                    // Update the incident counts for any hashtags in the status
                    foreach (var hashtag in tweet.Hashtags)
                    {
                        // if it already has a count, increment it, else initialise to 1
                        var count = 0;
                        var exists = hashtagLookup.TryGetValue(hashtag, out count);
                        hashtagLookup[hashtag] = ++count;

                        // If it is currently in a bucket, remove it
                        if (exists)
                        {
                            hashtags[count - 1].Remove(hashtag);
                        }

                        // get the new bucket, creating it if necessary
                        SortedList<string, string> newBucket;
                        if (!hashtags.TryGetValue(count, out newBucket))
                        {
                            hashtags[count] = newBucket = new SortedList<string, string>();
                        }

                        // add the tag to it's new bucket
                        newBucket.Add(hashtag, hashtag);
                    }
                }
            }
        }
    }
}
