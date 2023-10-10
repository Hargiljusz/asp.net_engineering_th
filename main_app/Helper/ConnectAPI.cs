using main_app.Models.Api;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace main_app.Helper
{
    public class ConnectAPI
    {
        private static HttpClient Client { get; set; }
        
        [HandleError]
        public static async Task<HttpResponseMessage> Get(string action)
        {
            Client = new MyAPI().Init();
            return await Client.GetAsync(action);       
        }

        [HandleError]
        public static async Task<HttpResponseMessage> Post(string action,HttpContent content)
        {
            Client = new MyAPI().Init();
            return await Client.PostAsync(action,content);
        }

        [HandleError]
        public static async Task<HttpResponseMessage> Put(string action, HttpContent content)
        {
            Client = new MyAPI().Init();
            return await Client.PutAsync(action, content);
        }

        [HandleError]
        public static async Task<HttpResponseMessage> Delete(string action)
        {
            Client = new MyAPI().Init();
            return await Client.DeleteAsync(action);
        }

        [HandleError]
        public static async Task<HttpResponseMessage> GetAuth(string action, string token)
        {
            Client = new MyAPI().Init();
            Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer",token);
            return await Client.GetAsync(action);
        }

        [HandleError]
        public static async Task<HttpResponseMessage> PostAuth(string action, HttpContent content,string token)
        {
            Client = new MyAPI().Init();
            Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            return await Client.PostAsync(action, content);
        }

        [HandleError]
        public static async Task<HttpResponseMessage> PutAuth(string action, HttpContent content, string token)
        {
            Client = new MyAPI().Init();
            Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            return await Client.PutAsync(action, content);
        }

        [HandleError]
        public static async Task<HttpResponseMessage> DeleteAuth(string action, string token)
        {
            Client = new MyAPI().Init();
            Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            return await Client.DeleteAsync(action);
        }


        [HandleError]
        public static async Task<HttpResponseMessage> GetAuth(string action, HttpSessionStateBase session, string name)
        {
            Client = new MyAPI().Init();
            await CheckTokenTime(session, name);
            Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", (string)session["My_JWT"]);
            return await Client.GetAsync(action);
        }

        [HandleError]
        public static async Task<HttpResponseMessage> PostAuth(string action, HttpContent content, HttpSessionStateBase session, string name)
        {
            Client = new MyAPI().Init();
            await CheckTokenTime(session, name);
            Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", (string)session["My_JWT"]);
            return await Client.PostAsync(action, content);
        }

        [HandleError]
        public static async Task<HttpResponseMessage> PutAuth(string action, HttpContent content, HttpSessionStateBase session, string name)
        {
            Client = new MyAPI().Init();
            await CheckTokenTime(session, name);
            Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", (string)session["My_JWT"]);
            return await Client.PutAsync(action, content);
        }

        [HandleError]
        public static async Task<HttpResponseMessage> DeleteAuth(string action, HttpSessionStateBase session, string name)
        {
            Client = new MyAPI().Init();
            await CheckTokenTime(session, name);
            Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", (string)session["My_JWT"]);
            return await Client.DeleteAsync(action);
        }


        private static async Task CheckTokenTime(HttpSessionStateBase session, string name)
        {
            long currentTime = (long)DateTime.UtcNow.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;


            if ((currentTime + 100000) >= (long)session["timeEXP"])
            {
                Console.WriteLine("Refreshing TOKEN");
                HttpClient client = new MyAPI().Init();
                var content = JsonConvert.SerializeObject(new Login_API
                {
                    Password = (string)session["noooo"],
                    Username = name
                });

                Succes_Login result_model = null;

                var httpContent = new StringContent(content, Encoding.UTF8, "application/json");


                var response = await client.PostAsync("login", httpContent);
                if (response.IsSuccessStatusCode)
                {
                    result_model = JsonConvert.DeserializeObject<Succes_Login>(await response.Content.ReadAsStringAsync());
                    session["My_JWT"] = result_model.Token;
                    session["currentTime"] = result_model.CurrentTime;
                    session["timeEXP"] = result_model.TimeEXP;
                }
            }
        }

    }
}