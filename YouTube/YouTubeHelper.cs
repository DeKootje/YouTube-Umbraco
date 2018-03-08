using System;
using System.Web.Configuration;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;

namespace YouTube
{
    using System.Collections.Generic;
    using System.Linq;
    using YouTube.Models;
    using YouTube.Providers;

    public class YouTubeHelper
    {
        //CONSTANTS
        private const string _ApiKey = "AIzaSyAgXB3nYk3f00eXZd0FGsUjJySf2Fnp7KA";
        private const string _ApplicationName = "YouTube for Umbraco";
        private const int _noPerPage = 9;

        /// <summary>
        /// Gets the YouTube Service that we use for all requests
        /// </summary>
        /// <returns></returns>
        public static YouTubeService GetYouTubeService()
        {
            var youTubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = WebConfigurationManager.AppSettings["YouTube-Umbraco:ApiKey"] ?? _ApiKey,
                ApplicationName = _ApplicationName
            });

            return youTubeService;
        }

        public static SearchListResponse GetPlaylistsForChannel(string pageToken, string channelId, string searchQuery, SearchResource.ListRequest.OrderEnum orderBy)
        {
            //Get YouTube Service
            var youTube = GetYouTubeService();

            //Build up request
            var videoRequest = youTube.Search.List("snippet");
            videoRequest.ChannelId = channelId;                        //Get videos for Channel only
            videoRequest.Order = orderBy;                          //Order by the view count/date (ENum Passed in)
            videoRequest.MaxResults = _noPerPage;                       //3 per page
            videoRequest.Type = "playlist";                          //Only get videos, as searches can return results for channel & other types
            videoRequest.PageToken = pageToken;                        //If more than 3 videos, we can request more videos using a page token (previous & next)

            //If we have a search query then...
            if (!string.IsNullOrEmpty(searchQuery))
            {
                //Change the order by from Date/Views etc to relevance
                //and specify the search query
                videoRequest.Order = SearchResource.ListRequest.OrderEnum.Relevance;
                videoRequest.Q = searchQuery;
            }

            //Perform request
            var videoResponse = videoRequest.Execute();

            //Return the list of videos we find
            return videoResponse;
        }

        public static SearchListResponse GetVideosForChannel(string pageToken, string channelId, string searchQuery, SearchResource.ListRequest.OrderEnum orderBy)
        {
            //Get YouTube Service
            var youTube = GetYouTubeService();

            //Build up request
            var videoRequest = youTube.Search.List("snippet");
            videoRequest.ChannelId = channelId;                        //Get videos for Channel only
            videoRequest.Order = orderBy;                          //Order by the view count/date (ENum Passed in)
            videoRequest.MaxResults = _noPerPage;                       //3 per page
            videoRequest.Type = "video";                          //Only get videos, as searches can return results for channel & other types
            videoRequest.PageToken = pageToken;                        //If more than 3 videos, we can request more videos using a page token (previous & next)

            //If we have a search query then...
            if (!string.IsNullOrEmpty(searchQuery))
            {
                //Change the order by from Date/Views etc to relevance
                //and specify the search query
                videoRequest.Order = SearchResource.ListRequest.OrderEnum.Relevance;
                videoRequest.Q = searchQuery;
            }

            //Perform request
            var videoResponse = videoRequest.Execute();

            //Return the list of videos we find
            return videoResponse;
        }


        /// <summary>
        /// Get specific details about a video
        /// </summary>
        /// <param name="videoId">Pass in the YouTube video ID</param>
        /// <returns></returns>
        /// https://www.googleapis.com/youtube/v3/videos?part=snippet%2Cstatistics&id=gRyPjRrjS34&key=AIzaSyAgXB3nYk3f00eXZd0FGsUjJySf2Fnp7KA
        public static VideoListResponse GetVideo(string videoId)
        {
            // Get YouTube Service
            var youTube = GetYouTubeService();

            // TODO: Inspect request properly & see what we actually need or not
            var videoRequest    = youTube.Videos.List("snippet");
            videoRequest.Id = videoId;

            // Perform request
            var videoResponse = videoRequest.Execute();

            return videoResponse;
        }


        public static ChannelListResponse GetChannelFromUsername(string usernameToQuery)
        {
            var youTube = GetYouTubeService();
            var channelQueryRequest = youTube.Channels.List("snippet,id,contentDetails,statistics,topicDetails");
            channelQueryRequest.ForUsername = usernameToQuery;
            channelQueryRequest.MaxResults = 1;

            // Perform request
            var channelResponse = channelQueryRequest.Execute();

            // If no items found in channel query attempt a search and find the channel id of the first item
            if (!channelResponse.Items.Any())
            {
                var searchRequest = youTube.Search.List("snippet");
                searchRequest.Q = usernameToQuery;
                searchRequest.MaxResults = 1;
                searchRequest.Type = "channel";
                var searchResponse = searchRequest.Execute();
                if (searchResponse.Items.Any())
                {
                    var channelId = searchResponse.Items.First().Snippet.ChannelId;
                    channelResponse = GetChannelFromId(channelId);
                }
            }

            return channelResponse;
        }

        public static ChannelListResponse GetChannelFromId(string channelId)
        {
            var youTube = GetYouTubeService();

            var channelQueryRequest = youTube.Channels.List("snippet,id,contentDetails,statistics,topicDetails");
            channelQueryRequest.Id = channelId;
            channelQueryRequest.MaxResults = 1;

            // Perform request
            var channelResponse = channelQueryRequest.Execute();

            return channelResponse;
        }

        public static List<YoutubeVideoItem> GetLastestVideosForPlayLists(List<string> playListIds)
        {
            List<YoutubeVideoItem> videos = new List<YoutubeVideoItem>();
            HttpCacheProvider cacheProvider = new HttpCacheProvider();

            var youTube = GetYouTubeService();

            //Build up request
            var videoRequest = youTube.PlaylistItems.List("snippet,contentDetails");
            videoRequest.MaxResults = _noPerPage;                       //3 per page

            foreach (var playlistId in playListIds)
            {

                var cacheItem = cacheProvider.GetItem<YoutubeVideoItem>(playlistId);

                if (cacheItem != null)
                {
                    videos.Add(cacheItem);
                    continue;
                }

                videoRequest.PlaylistId = playlistId;  //Get videos for playlist only

                //Perform request
                var videoResponse = videoRequest.Execute();

                var currentPlayList = videoResponse.Items.FirstOrDefault();

                if (currentPlayList == null)
                    continue;

                int y = 1;

                while (currentPlayList != null && currentPlayList.Snippet.Title == "Private video")
                {
                    if (y > videoResponse.Items.Count())
                    {
                        break;
                    }

                    currentPlayList = videoResponse.Items.ElementAt(y);
                    y++;
                }


                Tuple<string, DateTime?> extraInfo = GetExtraInfo(currentPlayList.ContentDetails.VideoId);
                if (extraInfo == null)
                    continue;

                var videoItem = new YoutubeVideoItem()
                {
                    Title = currentPlayList.Snippet.Title,
                    Id = currentPlayList.ContentDetails.VideoId,
                    PublishDate = extraInfo.Item2,
                    Image = currentPlayList.Snippet.Thumbnails != null ? currentPlayList.Snippet.Thumbnails.Maxres != null ? currentPlayList.Snippet.Thumbnails.Maxres.Url : currentPlayList.Snippet.Thumbnails.Standard?.Url : string.Empty,
                    Url = string.Format("https://www.youtube.com/embed/{0}?rel=0", currentPlayList.ContentDetails.VideoId),
                    ExternalUrl = string.Format("https://www.youtube.com/watch?v={0}", currentPlayList.ContentDetails.VideoId),
                    WatchCount = extraInfo.Item1
                };

                videos.Add(videoItem);
                cacheProvider.InsertItem<YoutubeVideoItem>(playlistId, videoItem, TimeSpan.FromHours(1));

            }

            //Return the list of videos we find
            return videos;
        }

        public static List<YoutubeVideoItem> GetVideos(List<string> videoIds)
        {
            List<YoutubeVideoItem> videos = new List<YoutubeVideoItem>();
            HttpCacheProvider cacheProvider = new HttpCacheProvider();

            // Todo: Return result object with status message and result

            try
            {
                var youTube = GetYouTubeService();

                //Build up request
                var videoRequest = youTube.PlaylistItems.List("snippet,contentDetails, statistics");
                videoRequest.MaxResults = _noPerPage;                       //3 per page

                foreach (var videoId in videoIds)
                {

                    var cacheItem = cacheProvider.GetItem<YoutubeVideoItem>(videoId);

                    if (cacheItem != null)
                    {
                        videos.Add(cacheItem);
                        continue;
                    }

                    //Perform request
                    VideoListResponse videoResponse = GetVideo(videoId);

                    if (videoResponse == null && !videoResponse.Items.Any())
                        continue;

                    var videoItem = new YoutubeVideoItem()
                    {
                        Title = videoResponse.Items.FirstOrDefault().Snippet.Title,
                        Id = videoResponse.Items.FirstOrDefault().Id,
                        PublishDate = videoResponse.Items.FirstOrDefault().Snippet.PublishedAt,
                        Image = videoResponse.Items.FirstOrDefault().Snippet.Thumbnails.High.Url,
                        Url = string.Format("https://www.youtube.com/embed/{0}?rel=0", videoResponse.Items.FirstOrDefault().Id),
                        ExternalUrl = string.Format("https://www.youtube.com/watch?v={0}", videoResponse.Items.FirstOrDefault().Id),
                        WatchCount = videoResponse.Items.FirstOrDefault().Statistics.ViewCount.ToString()
                    };

                    videos.Add(videoItem);

                    cacheProvider.InsertItem<YoutubeVideoItem>(videoId, videoItem, TimeSpan.FromHours(1));
                }
            }
            catch (Exception ex)
            {
                // Todo: Implement error handling
            }


            //Return the list of videos we find
            return videos;

        }

        private static Tuple<string, DateTime?> GetExtraInfo(string videoId)
        {
            // Get YouTube Service
            var youTube = GetYouTubeService();

            // TODO: Inspect request properly & see what we actually need or not
            var videoRequest = youTube.Videos.List("snippet,statistics");
            videoRequest.Id = videoId;

            // Perform request
            var videoResponse = videoRequest.Execute();

            if (videoResponse.Items.FirstOrDefault() == null)
                return null;
            return new Tuple<string, DateTime?>(videoResponse.Items.FirstOrDefault().Statistics.ViewCount.ToString(), videoResponse.Items.FirstOrDefault().Snippet.PublishedAt);
        }

    }
}
