using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace main_app.Models.Api
{
    public class Event
    {
        [JsonProperty("authorId")]
        public string AuthorId { get; set; }

        [JsonProperty("creationDate")]
        public DateTime CreationDate { get; set; }

        [JsonProperty("decription")]
        public string Decription { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("img_src")]
        public string ImgSrc { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }
}