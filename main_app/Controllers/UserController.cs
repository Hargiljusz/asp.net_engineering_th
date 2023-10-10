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
    [Authorize]
    [AuthorizeSessionAttribute]
    public class UserController : Controller
    {
        // GET: User
        public async Task<ActionResult> Index(int? page,string currentFilter, string searchString)
        {
            int pageSize = 10;
            int pageNumber = (page ?? 1);


            if (searchString != null)
            {
                page = 1;
            }
            else
            {
                searchString = currentFilter;
            }

            ViewBag.CurrentFilter = searchString;

            HttpResponseMessage result = null;

            if (String.IsNullOrEmpty(searchString))
            {
                result = await ConnectAPI.GetAuth("api/users/?site=" + (pageNumber - 1) + "&size=" + pageSize + "&type=_id", Session, User.Identity.Name);
            }
            else
            {
                result = await ConnectAPI.GetAuth("api/users/query/?query=" + searchString + "&site=" + (pageNumber - 1) + "&size=" + pageSize, Session, User.Identity.Name);
            }
            Root<SimpleUser> content;

            if (result.IsSuccessStatusCode)
            {
                content = JsonConvert.DeserializeObject<Root<SimpleUser>>(await result.Content.ReadAsStringAsync());

                List<SimpleUser> users = new List<SimpleUser>();
                int counter = 0;

                for (int i = 0; i < content.TotalElements; i++)
                {
                    if ((i >= (pageNumber - 1) * pageSize) && (i < (pageNumber) * pageSize))
                    {
                        users.Add(content.Content[counter]);
                        counter++;
                        continue;
                    }
                    users.Add(null);
                }


                return View(users.ToPagedList(pageNumber, pageSize));
            }
            return View();
        }

        // GET: User/Details/5
        [Authorize]
        public async Task<ActionResult> Details(string id)
        {
            bool yourID = false;
            if(id == null)
            {
                id = (string)Session["UserID"];
                yourID = true;
            }
            var result = await ConnectAPI.GetAuth("api/users/getSimple/" + id, Session, User.Identity.Name);

            if(result == null)
            {
                return RedirectToAction("Error_API", "Home");
            }
            if (result.IsSuccessStatusCode)
            {
                SimpleUser user = null;
                user = JsonConvert.DeserializeObject<SimpleUser>(await result.Content.ReadAsStringAsync());
                ViewBag.yourID = yourID;
                return View(user);
            }
            else
            {
                return RedirectToAction("Error_API","Home");
            }
        }

        // GET: User/Create
        //public ActionResult Create()
        //{
        //    return View();
        //}

        //// POST: User/Create
        //[HttpPost]
        //public ActionResult Create(FormCollection collection)
        //{
        //    try
        //    {
        //        // TODO: Add insert logic here

        //        return RedirectToAction("Index");
        //    }
        //    catch
        //    {
        //        return View();
        //    }
        //}

        // GET: User/Edit/5
        [Authorize]
        public async Task<ActionResult> Edit(string id)
        {
            var result = await ConnectAPI.GetAuth("api/users/getSimple/" + id, Session, User.Identity.Name);

            if (result == null)
            {
                return RedirectToAction("Error_API", "Home");
            }
            if (result.IsSuccessStatusCode)
            {
                SimpleUser user = null;
                user = JsonConvert.DeserializeObject<SimpleUser>(await result.Content.ReadAsStringAsync());
                return View(user);
            }
            else
            {
                return RedirectToAction("Error_API", "Home");
            }
        }

        

        // POST: User/Edit/5
        [HttpPost]
        public async Task<ActionResult> Edit(string id, SimpleUser model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            UserRegister user = new UserRegister()
            {
                Name = model.Name,
                Surename = model.Surname,
                PhoneNumber = model.PhoneNumber
            };
            var content = JsonConvert.SerializeObject(user);

            var httpContent = new StringContent(content, Encoding.UTF8, "application/json");

            HttpPostedFileBase file = Request.Files["main_img"];
            var contentM = new MultipartFormDataContent();
            if (file != null && file.ContentLength > 0)
            {
                contentM.Add(new StreamContent(new MemoryStream(FileHelper.Convert(file))), "img", file.FileName);
            }
            contentM.Add(httpContent, "data");

            var response = await ConnectAPI.PutAuth("api/users/" + id, contentM, Session, User.Identity.Name);


            return RedirectToAction("Index","Home");
        }

        // GET: User/Delete/5
        //public ActionResult Delete(int id)
        //{
        //    return View();
        //}

        //// POST: User/Delete/5
        //[HttpPost]
        //public ActionResult Delete(int id, FormCollection collection)
        //{
        //    try
        //    {
        //        // TODO: Add delete logic here

        //        return RedirectToAction("Index");
        //    }
        //    catch
        //    {
        //        return View();
        //    }
        //}
    }

    
}
