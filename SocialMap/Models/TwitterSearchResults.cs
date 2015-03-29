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
        private readonly IDictionary<int, IEnumerable<string>> hashtags;

        public IEnumerable<IHashtagBucket> HashTags
        {
            get
            {
                foreach (var pair in hashtags)
                {
                    yield return new HashtagBucket(pair.Key, pair.Value);
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

            var json = JObject.Parse(jsonStr);
            var hashtagLookup = new Dictionary<string, int>();
            var hashtagBuckets = new Dictionary<int, SortedList<string, string>>();

            foreach(var rawTweet in json["statuses"].Children()) {
                // Parse the status and store it
                var tweet = new Tweet(rawTweet);
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
                        hashtagBuckets[count - 1].Remove(hashtag);
                    }

                    // get the new bucket, creating it if necessary
                    SortedList<string, string> newBucket;
                    if (!hashtagBuckets.TryGetValue(count, out newBucket))
                    {
                        hashtagBuckets[count] = newBucket = new SortedList<string, string>();
                    }

                    // add the tag to it's new bucket
                    newBucket.Add(hashtag, hashtag);
                }
            }

            // Finished with statuses and hashtags, use a sorted dictionary to sort the hashtags, skipping any empty buckets
            hashtags = new SortedDictionary<int, IEnumerable<string>>();
            foreach (var pair in hashtagBuckets)
            {
                if (pair.Value.Count > 0)
                {
                    hashtags[pair.Key] = pair.Value.Values;
                }
            }
        }
    }
}