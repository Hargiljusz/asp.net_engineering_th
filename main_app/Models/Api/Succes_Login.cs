using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace main_app.Models.Api
{

    public class Role
    {
        [JsonProperty("authority")]
        public string Authority { get; set; }
    }

    public class Succes_Login
    {
        [JsonProperty("currentTime")]
        public long CurrentTime { get; set; }

        [JsonProperty("roles")]
        public List<Role> Roles { get; set; }

        [JsonProperty("timeEXP")]
        public long TimeEXP { get; set; }

        [JsonProperty("token")]
        public string Token { get; set; }

        [JsonProperty("userID")]
        public string UserID { get; set; }

        [JsonProperty("username")]
        public string Username { get; set; }
    }
}