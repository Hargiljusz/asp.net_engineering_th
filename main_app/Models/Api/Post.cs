using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace main_app.Models.Api
{
    public class Comment
    {
        [JsonProperty("authorId")]
        public string AuthorId { get; set; }

        [JsonProperty("conntent")]
        public string Conntent { get; set; }

        [JsonProperty("createdTime")]
        public DateTime CreatedTime { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }
    }

    public class Post
    {
        [JsonProperty("authorId")]
        public string AuthorId { get; set; }

        [JsonProperty("comments")]
        public List<Comment> Comments { get; set; }

        [JsonProperty("createDate")]
        public DateTime CreateDate { get; set; }

        [JsonProperty("decription")]
        public string Decription { get; set; }

        [JsonProperty("groupId")]
        public string GroupId { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("main_img_src")]
        public string MainImgSrc { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("file_src")]
        public List<string> FileSrc { get; set; }
    }
}