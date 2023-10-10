using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web;

namespace main_app.Helper
{
    public class MyAPI
    {
        public HttpClient Init()
        {
            var client = new HttpClient();
            client.BaseAddress = new Uri("http://localhost:8080/");
            return client;
        }
    }
}