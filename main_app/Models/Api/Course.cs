using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace main_app.Models.Api
{
    public class Lesson
    {
        [JsonProperty("conntent")]
        public string Conntent { get; set; }

        [JsonProperty("file_src")]
        public List<string> FileSrc { get; set; }

        [JsonProperty("homework")]
        public string Homework { get; set; }

        [JsonProperty("number")]
        public int Number { get; set; }

        [JsonProperty("subject")]
        public string Subject { get; set; }

        [JsonProperty("video_link")]
        public string VideoLink { get; set; }
    }

    public class Course
    {
        [JsonProperty("authorId")]
        public string AuthorId { get; set; }

        [JsonProperty("decription")]
        public string Decription { get; set; }

        [JsonProperty("groupeId")]
        public string GroupeId { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("kind")]
        public Kind Kind { get; set; }

        [JsonProperty("lessons")]
        public List<Lesson> Lessons { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("published")]
        public bool Published { get; set; }

        [JsonProperty("publishedDate")]
        public DateTime PublishedDate { get; set; }
    }
}

