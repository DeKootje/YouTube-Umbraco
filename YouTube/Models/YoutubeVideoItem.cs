using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YouTube.Models
{
    public class YoutubeVideoItem
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public DateTime? PublishDate { get; set; }
        public string Url { get; set; }
        public string ExternalUrl { get; set; }
        public string Image { get; set; }
        public string MaxresImage { get; set; }
        public string WatchCount { get; set; }

        public YoutubeVideoItem(string title, string id)
        {
            this.Title = title;
            this.Id = id;
        }

        public YoutubeVideoItem()
        {

        }
    }
}
