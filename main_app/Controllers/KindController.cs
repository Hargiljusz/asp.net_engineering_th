using main_app.Attributes;
using main_app.Helper;
using main_app.Models.Api;
using main_app.Models.Api.Page;
using Newtonsoft.Json;
using PagedList;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace main_app.Controllers
{
    [Authorize(Roles = "ROLE_ADMIN")]
    [AuthorizeSessionAttribute]
    public class KindController : Controller
    {
        // GET: Kind
        [AllowAnonymous]
        public async Task<ActionResult> Index(int? page)
        {

            int pageSize = 1;
            int pageNumber = (page ?? 1);

            var result = await ConnectAPI.Get("api/kinds/?site=" + (pageNumber - 1) + "&size=" + pageSize);
            Root <Kind> content;

            if(result == null)
            {
                return RedirectToAction("Error_API", "Home");
            }

            if (result.IsSuccessStatusCode)
            {
                content = JsonConvert.DeserializeObject<Root<Kind>>(await result.Content.ReadAsStringAsync());

                List<Kind> kinds = new List<Kind>();
                int counter = 0;

                for (int i = 0; i < content.TotalElements; i++)
                {
                    if ((i >= (pageNumber - 1) * pageSize) && (i < (pageNumber) * pageSize))
                    {
                        kinds.Add(content.Content[counter]);
                        counter++;
                        continue;
                    }
                    kinds.Add(null);
                }


                return View(kinds.ToPagedList(pageNumber, pageSize));
            }
            return View();
        }

        // GET: Kind/Details/5
        [AllowAnonymous]
        public async Task<ActionResult> Details(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var result = await ConnectAPI.Get("api/kinds/" + id);
            Kind kind = null;

            if (result.IsSuccessStatusCode)
            {
                kind = JsonConvert.DeserializeObject<Kind>(await result.Content.ReadAsStringAsync());
            }else if(result.StatusCode == HttpStatusCode.NotFound)
            {
                return HttpNotFound();
            }

            return View(kind);
        }

        // GET: Kind/Create
        public async Task<ActionResult> Create()
        {
            return View();
        }

        // POST: Kind/Create
        [HttpPost]
        public async Task<ActionResult> Create([Bind(Include = "Name")]  Kind model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var content = JsonConvert.SerializeObject(model);

            var httpContent = new StringContent(content, Encoding.UTF8, "application/json");

            CheckTokenTime();
            var response = await ConnectAPI.PostAuth("api/kinds/",httpContent,Session,User.Identity.Name);

            if (response.IsSuccessStatusCode)
            {
                if (response != null)
                    return RedirectToAction("Index");
                else
                    return RedirectToAction("Error_API", "Home");
            }
            else
                return RedirectToAction("Error_API", "Home");

        }

        // GET: Kind/Edit/5
        public async Task<ActionResult> Edit(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var result = await ConnectAPI.Get("api/kinds/" + id);
            Kind kind = null;

            if (result.IsSuccessStatusCode)
            {
                kind = JsonConvert.DeserializeObject<Kind>(await result.Content.ReadAsStringAsync());
            }

            if (kind == null)
            {
                return HttpNotFound();
            }

            return View(kind);

        }

        // POST: Kind/Edit/5
        [HttpPost]
        public async Task<ActionResult> Edit(string id, [Bind(Include = "Name")] Kind model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var content = JsonConvert.SerializeObject(model);

            var httpContent = new StringContent(content, Encoding.UTF8, "application/json");

            CheckTokenTime();
            var response = await ConnectAPI.PutAuth("api/kinds/"+id, httpContent, Session, User.Identity.Name);

            if (response.IsSuccessStatusCode)
            {
                if (response != null)
                    return RedirectToAction("Index");
                else
                    return RedirectToAction("Error_API", "Home");
            }else if(response.StatusCode == HttpStatusCode.NotFound)
            {
                return HttpNotFound();
            }
            else
            {
                return RedirectToAction("Error_API", "Home");
            }

        }

        // GET: Kind/Delete/5
        public async Task<ActionResult> Delete(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            CheckTokenTime();
            var result = await ConnectAPI.DeleteAuth("api/kinds/" + id, Session, User.Identity.Name);
            if (result.IsSuccessStatusCode)
            {
                return RedirectToAction("Index");
            }
            else if (result.StatusCode == HttpStatusCode.NotFound)
            {
                return HttpNotFound();
            }
            else
                return RedirectToAction("Error_API", "Home");
        }



        private  async void CheckTokenTime()
        {
            long currentTime = (long)DateTime.UtcNow.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
           

            if ((currentTime + 100000) >= (long)Session["timeEXP"])
            {
                HttpClient client = new MyAPI().Init();
                var content = JsonConvert.SerializeObject(new Login_API
                {
                    Password = (string)Session["noooo"],
                    Username = User.Identity.Name
                });

                Succes_Login result_model = null;

                var httpContent = new StringContent(content, Encoding.UTF8, "application/json");


                var response = await client.PostAsync("login", httpContent);
                if (response.IsSuccessStatusCode)
                {
                    Session["My_JWT"] = result_model.Token;
                    Session["currentTime"] = result_model.CurrentTime;
                    Session["timeEXP"] = result_model.TimeEXP;
                }
            }
        }


    }
}
