Polymer({
    max_hashtags: 10,
    handleError: function (e) {
        if (e.detail.xhr.status === 401) {
            window.open(this.auth_url, "_self");
        } else {
            this.$.error.className = "";
        }
    },
    handleTweets: function (tweets) {
        // Loop over all the tweets, creating a marker with content
        for (var i = 0; i < tweets.length; i++) {
            var tweet = tweets[i];

            var markerContent = document.createElement("social-marker-content");
            markerContent.setAttribute("text", tweet.Text);
            markerContent.setAttribute("sender", tweet.Sender);
            markerContent.setAttribute("createdAt", tweet.CreatedAt);
            markerContent.setAttribute("favorites", tweet.Favourites);
            markerContent.setAttribute("retweets", tweet.Retweets);

            var marker = document.createElement("google-map-marker");
            marker.setAttribute("longitude", tweet.Longitude);
            marker.setAttribute("latitude", tweet.Latitude);
            marker.appendChild(markerContent);

            this.$.map.appendChild(marker);
        }
    },
    handleHashtagClick: function (e) {
        // getting called twice, second time with undefined hashtag... Quick sanity check
        if (e.detail.hashtag !== undefined) {
            // update the ajax element and start a request
            this.$.ajaxTweets.params = {
                "radius": this.radius,
                "latitude": this.$.loc.latitude,
                "longitude": this.$.loc.longitude,
                "query": encodeURIComponent("#" + e.detail.hashtag)
            };
            this.$.ajaxTweets.go();
        }
    },
    handleHashtags: function (hashtags) {
        var shown = 0;

        // clear the current list of hashtags
        this.$.hashtags.innerHTML = "";

        // add the new hashtags, up to the set limit, starting with the most used one
        for (var i = hashtags.length - 1; i >= 0 && shown < this.max_hashtags; i--) {
            var bucket = hashtags[i];

            for (var t = 0; t < bucket.Tags.length && shown < this.max_hashtags; t++, shown++) {
                var hashtagBtn = document.createElement("social-hashtag-button");
                hashtagBtn.setAttribute("hashtag", bucket.Tags[t]);
                hashtagBtn.setAttribute("occurances", bucket.Occurances);

                var rootElem = this;
                hashtagBtn.addEventListener("click", function (e) {
                    rootElem.handleHashtagClick.call(rootElem, e);
                });

                this.$.hashtags.appendChild(hashtagBtn);
            }
        }
    },
    handleResults: function (e) {
        // clear the map's markers
        this.$.map.innerHTML = "";
        this.handleTweets(e.detail.response.Tweets);
        this.handleHashtags(e.detail.response.HashTags);
    },
    handleRefresh: function (e) {
        // setting watchpos will force the position to update
        this.$.loc.setAttribute("watchpos", "true");
    },
    handleGeoResponse: function (e) {
        // only want one update at a time, so remove watchpos if it is set
        this.$.loc.removeAttribute("watchpos");

        // update the ajax element and start a request
        this.$.ajaxTweets.params = {
            "radius": this.radius,
            "latitude": e.detail.latitude,
            "longitude": e.detail.longitude
        };
        this.$.ajaxTweets.go();
    }
});
