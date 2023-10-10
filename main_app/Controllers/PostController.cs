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
    public class PostController : Controller
    {
        // GET: Post
        public async Task<ActionResult> Index(int? page,int? pageC, string currentFilter, string searchString,bool? showForm)
        {
            if (searchString != null)
            {
                page = 1;
            }
            else
            {
                searchString = currentFilter;
            }
            ViewBag.CurrentFilter = searchString;

            int pageSize = 2;
            int pageNumber = (page ?? 1);
            int pageComment = (pageC ?? 1);
            int pageCommentSize = 5;
            showForm = showForm ?? true;

            HttpResponseMessage result = null;

            if (String.IsNullOrEmpty(searchString))
            {
                result = await ConnectAPI.GetAuth("api/posts/?siteC="+(pageComment-1)+"&siteP="+(pageNumber -1)+"&sizeC="+pageCommentSize+"&sizeP="+pageSize, Session, User.Identity.Name);
            }
            else
            {
                result = await ConnectAPI.GetAuth("api/posts/?authorId="+searchString+"&siteC=" + (pageComment - 1) + "&siteP=" + (pageNumber - 1) + "&sizeC=" + pageCommentSize + "&sizeP=" + pageSize, Session, User.Identity.Name);
            }

            List<Post> content;

            if(result == null)
            {
                return RedirectToAction("Error_API", "Home");
            }

            if (result.IsSuccessStatusCode)
            {
                content = JsonConvert.DeserializeObject<List<Post>>(await result.Content.ReadAsStringAsync());

              

                ViewBag.showForm = showForm;
                ViewBag.Page = pageNumber;
                ViewBag.isNext = (content.Count > 0 && content.Count == pageSize) ? true : false;
                return View(content);
            }
            else
            {
                return RedirectToAction("Error_API", "Home");
            }
        }

        // GET: Post/Details/5
        public async Task<ActionResult> Details(string id, int? pageC)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            int pageComment = (pageC ?? 1);
            int pageCommentSize = 5;

            var result = await ConnectAPI.GetAuth("api/posts/"+id+"?siteC=" + (pageComment - 1)+ "&sizeC=" + pageCommentSize, Session, User.Identity.Name);
            Post post= null;

            if(result ==null)
            {
                return RedirectToAction("Error_API", "Home");
            }

            if (result.IsSuccessStatusCode)
            {
                post = JsonConvert.DeserializeObject<Post>(await result.Content.ReadAsStringAsync());
            }
            else if (result.StatusCode == HttpStatusCode.NotFound)
            {
                return HttpNotFound();
            }
            else
            {
                return RedirectToAction("Error_API", "Home");
            }

            return View(post);
        }

        // GET: Post/Create
        public ActionResult Create(string groupID)
        {
            if (groupID == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ViewBag.groupID = groupID;
            return View();
        }

        // POST: Post/Create
        [HttpPost]
        public async  Task<ActionResult> Create(Post post)
        {
            
            if (ModelState.IsValid)
            {

               post.AuthorId = (string)Session["UserID"];
                post.CreateDate = DateTime.Now;

                var content = JsonConvert.SerializeObject(post);

                var httpContent = new StringContent(content, Encoding.UTF8, "application/json");

                #region img
                HttpPostedFileBase file = Request.Files["main_img"];
                var contentM = new MultipartFormDataContent();

                if (file != null && file.ContentLength > 0)
                {
                    contentM.Add(new StreamContent(new MemoryStream(FileHelper.Convert(file))), "img", file.FileName);
                }
                #endregion

                #region files
                IList<HttpPostedFileBase> files = Request.Files.GetMultiple("filesForPost");
               
                    foreach (var item in files)
                    {
                        if (item.ContentLength > 0)
                        {
                            contentM.Add(new StreamContent(new MemoryStream(FileHelper.Convert(item))), "files", item.FileName);
                        }
                    }
                
                #endregion

                #region data
                contentM.Add(httpContent, "data");
                #endregion

                var response = await ConnectAPI.PostAuth("api/posts/", contentM, Session, User.Identity.Name);

                if(response == null)
                {

                }
                if (response.IsSuccessStatusCode)
                {
                    return RedirectToAction(nameof(Details), nameof(Group), new { id =post.GroupId });
                }
                else
                {
                   return  RedirectToAction("Error_API", "Home");
                }

            }


            return View(post);
        }

        // GET: Post/Edit/5
        public async Task<ActionResult> Edit(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            int pageComment = 1;
            int pageCommentSize = 0;

            var result = await ConnectAPI.GetAuth("api/posts/"+id+"?siteC=" + (pageComment - 1) + "&sizeC=" + pageCommentSize, Session, User.Identity.Name);
            Post post = null;

            if (result == null)
            {
                return RedirectToAction("Error_API", "Home");
            }

            if (result.IsSuccessStatusCode)
            {
                post = JsonConvert.DeserializeObject<Post>(await result.Content.ReadAsStringAsync());
            }
            else if (result.StatusCode == HttpStatusCode.NotFound)
            {
                return HttpNotFound();
            }
            else
            {
                return RedirectToAction("Error_API", "Home");
            }

            return View(post);
        }

        // POST: Post/Edit/5
        [HttpPost]
        public async Task<ActionResult> Edit(string id, Post post)
        {
            if (id == null )
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            if (ModelState.IsValid)
            {

                post.CreateDate = DateTime.Now;

                var content = JsonConvert.SerializeObject(post);

                var httpContent = new StringContent(content, Encoding.UTF8, "application/json");


                HttpPostedFileBase file = Request.Files["main_img"];
                var contentM = new MultipartFormDataContent();

                if (file != null && file.ContentLength > 0)
                {
                    contentM.Add(new StreamContent(new MemoryStream(FileHelper.Convert(file))), "img", file.FileName);
                }

                #region files
                IList<HttpPostedFileBase> files = Request.Files.GetMultiple("filesForPost");
                if (files != null)
                {
                    foreach (var item in files)
                    {
                        if (item.ContentLength > 0)
                        {
                            contentM.Add(new StreamContent(new MemoryStream(FileHelper.Convert(item))), "files", item.FileName);
                        }
                    }
                }
                #endregion

                contentM.Add(httpContent, "data");

                var response = await ConnectAPI.PutAuth("api/posts/"+id, contentM, Session, User.Identity.Name);

                if (response == null)
                {
                    RedirectToAction("Error_API", "Home");
                }
                if (response.IsSuccessStatusCode)
                {

                    Post temp = JsonConvert.DeserializeObject<Post>(await response.Content.ReadAsStringAsync());
                    return RedirectToAction(nameof(Details), nameof(Group), new { id = temp.GroupId });
                }
                else
                {
                  return  RedirectToAction("Error_API", "Home");
                }

            }


            return View(post);
        }

        // GET: Post/Delete/5
        public async Task<ActionResult> Delete(string id, string groupID)
        {
            if (id == null || groupID == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }


            var response = await ConnectAPI.DeleteAuth("api/posts/" + id, Session, User.Identity.Name);

            if(response == null)
            {
                return RedirectToAction("Error_API", "Home");
            }

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction(nameof(Details), nameof(Group), new { id = groupID });
            }
            else if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return HttpNotFound();
            }
            return RedirectToAction("Error_API", "Home");
        }

        public async Task<ActionResult> PostByGroupPartialView(string groupID,int? page,int? pageC)
        {
            int pageSize = 10;
            int pageNumber = (page ?? 1);
            int pageComment = (pageC ?? 1);
            int pageCommentSize = 5;

            var result = await ConnectAPI.GetAuth("api/posts/group/" + groupID+"?siteC=" + (pageComment - 1) + "&siteP=" + (pageNumber - 1) + "&sizeC=" + pageCommentSize + "&sizeP=" + pageSize, Session, User.Identity.Name);

            List<Post> content;

            if (result == null)
            {
                return null;
            }

            if (result.IsSuccessStatusCode)
            {
                content = JsonConvert.DeserializeObject<List<Post>>(await result.Content.ReadAsStringAsync());

                ViewBag.isGroupOwner = await isOwner(groupID, (string)Session["UserID"]);
                ViewBag.authorId = (string)Session["UserID"];
                ViewBag.groupId = groupID;
                return PartialView(content);
            }
            else
            {
                return null;
            }
           
        }



        private async Task<bool> isOwner(string groupId, string userId)
        {
            var result = await ConnectAPI.GetAuth("api/groups/" + groupId, Session, User.Identity.Name);


           Group content;

            if (result == null)
            {
                return false;
            }

            if (result.IsSuccessStatusCode)
            {
                content = JsonConvert.DeserializeObject<Group>(await result.Content.ReadAsStringAsync());
                return userId.Equals(content.OwnerId);
            }
            else
            {
                return false;
            }
        }
    }
}
