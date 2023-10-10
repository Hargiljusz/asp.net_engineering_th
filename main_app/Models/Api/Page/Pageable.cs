using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace main_app.Models.Api.Page
{
    public class Pageable
    {
        [JsonProperty("offset")]
        public int Offset { get; set; }

        [JsonProperty("pageNumber")]
        public int PageNumber { get; set; }

        [JsonProperty("pageSize")]
        public int PageSize { get; set; }

        [JsonProperty("paged")]
        public bool Paged { get; set; }

        [JsonProperty("sort")]
        public Sort Sort { get; set; }

        [JsonProperty("unpaged")]
        public bool Unpaged { get; set; }
    }
}