using main_app.Attributes;
using main_app.Helper;
using main_app.Models.Api;
using main_app.Models.Api.Page;
using Newtonsoft.Json;
using PagedList;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace main_app.Controllers
{
    public class HomeController : Controller
    {
        public async Task<ActionResult> Index(int? page)
        {
            int pageSize = 10;
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

        [Authorize]
        [AuthorizeSessionAttribute]
        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        public ActionResult Succes()
        {
            ViewBag.Message = "Succes Register";

            return View();
        }

        public ActionResult Code_409()
        {
            ViewBag.Message = "Email is taken";

            return View();
        }

        public ActionResult Error_API()
        {
            ViewBag.Message = "Error_API";

            return View();
        }
    }
}