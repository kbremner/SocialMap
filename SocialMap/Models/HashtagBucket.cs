using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SocialMap.Models
{
    public interface IHashtagBucket
    {
        int Occurances { get; }
        IEnumerable<string> Tags { get; }
    }

    public class HashtagBucket : IHashtagBucket
    {
        public HashtagBucket(int occurances, IEnumerable<string> tags)
        {
            Occurances = occurances;
            Tags = tags;
        }

        public int Occurances { get; private set; }
        public IEnumerable<string> Tags { get; private set; }
    }
}