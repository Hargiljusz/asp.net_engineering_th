using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace main_app.Models.Api
{
    public class Member
    {

        [JsonProperty("isJoin")]
        public bool IsJoin { get; set; }

        [JsonProperty("isMember")]
        public bool? IsMember { get; set; }
    }

    public class MemberData
    {
        [JsonProperty("surname")]
        public string Surname { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("phonenumber")]
        public string Phonenumber { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }
    }

}