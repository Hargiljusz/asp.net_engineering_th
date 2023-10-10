using main_app.Attributes;
using main_app.Helper;
using main_app.Models.Api;
using main_app.Models.Api.Page;
using Newtonsoft.Json;
using PagedList;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace main_app.Controllers
{
    [AuthorizeSessionAttribute]
    [Authorize(Roles = "ROLE_ADMIN")]
    public class EventController : Controller
    {
        [AllowAnonymous]
        public async Task<ActionResult> Index(int? page)
        {
            int pageSize = 1;
            int pageNumber = (page ?? 1);

            var result = await ConnectAPI.Get("api/events/?site=" + (pageNumber - 1) + "&size=" + pageSize);
            Root<Event> content;

            if (result == null)
            {
                return RedirectToAction("Error_API", "Home");
            }

            if (result.IsSuccessStatusCode)
            {
                content = JsonConvert.DeserializeObject<Root<Event>>(await result.Content.ReadAsStringAsync());

                List<Event> kinds = new List<Event>();
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
            else
            {
                return View();
            }
        }

        // GET: Event/Details/5
        [AllowAnonymous]
        public async Task<ActionResult> Details(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var result = await ConnectAPI.Get("api/events/" + id);
            Event my_event = null;

            if(result == null)
            {
                return RedirectToAction("Error_API", "Home");
            }

            if (result.IsSuccessStatusCode)
            {
                my_event = JsonConvert.DeserializeObject<Event>(await result.Content.ReadAsStringAsync());
            }
            else if (result.StatusCode == HttpStatusCode.NotFound)
            {
                return HttpNotFound();
            }

            return View(my_event);
        }

        // GET: Event/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Event/Create
        [HttpPost]
        public async Task<ActionResult> Create([Bind(Include = "Name,Decription")]  Event model)
        {
            if (ModelState.IsValid)
            {
                model.CreationDate = DateTime.Now;
                //model.AuthorId = (string)Session["UserID"];
                var content = JsonConvert.SerializeObject(model);

                var httpContent = new StringContent(content, Encoding.UTF8, "application/json");

                HttpPostedFileBase file = Request.Files["main_img"];
                var contentM = new MultipartFormDataContent();
                contentM.Add(new StreamContent(new MemoryStream(FileHelper.Convert(file))), "img", file.FileName);
                contentM.Add(httpContent, "event");

                var response = await ConnectAPI.PostAuth("api/events/", contentM,Session,User.Identity.Name);

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
            return View(model);
        }

        // GET: Event/Edit/5
        public async Task<ActionResult> Edit(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var result = await ConnectAPI.Get("api/events/" + id);
            Event my_event = null;

            if (result == null)
            {
                return RedirectToAction("Error_API", "Home");
            }

            if (result.IsSuccessStatusCode)
            {
                my_event = JsonConvert.DeserializeObject<Event>(await result.Content.ReadAsStringAsync());
            }
            else if (result.StatusCode == HttpStatusCode.NotFound)
            {
                return HttpNotFound();
            }

            return View(my_event);
        }

        // POST: Event/Edit/5
        [HttpPost]
        public async Task<ActionResult> Edit(string id, [Bind(Include = "Name,Decription")] Event model)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            if (ModelState.IsValid)
            {
                model.CreationDate = DateTime.Now;
                var content = JsonConvert.SerializeObject(model);

                var httpContent = new StringContent(content, Encoding.UTF8, "application/json");

                HttpPostedFileBase file = Request.Files["main_img"];
                var contentM = new MultipartFormDataContent();
                if (file != null && file.ContentLength > 0)
                {
                    contentM.Add(new StreamContent(new MemoryStream(FileHelper.Convert(file))), "img", file.FileName);
                }
                contentM.Add(httpContent, "event");

                var response = await ConnectAPI.PutAuth("api/events/" + id, contentM, Session, User.Identity.Name);

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
            return View(model);
        }

        // GET: Event/Delete/5
        public async Task<ActionResult> Delete(string id)
        {
            var response = await ConnectAPI.DeleteAuth("api/events/" + id, Session, User.Identity.Name);
            if(response == null)
            {
                return RedirectToAction("Error_API", "Home");
            }

            if (response.IsSuccessStatusCode)
            {
                if (response != null)
                    return RedirectToAction("Index");
                else
                    return RedirectToAction("Error_API", "Home");
            }
            else if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return HttpNotFound();
            }

            return RedirectToAction("Index");
        }

       
    }
}
