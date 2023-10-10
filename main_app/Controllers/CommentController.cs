using main_app.Attributes;
using main_app.Helper;
using main_app.Models;
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
    [Authorize]
	[AuthorizeSessionAttribute]
    public class CommentController : Controller
    {
        // GET: Comment
        [Authorize(Roles ="ROLE_ADMIN")]
        public async Task<ActionResult> Index(int? page)
        {

            int pageSize = 1;
            int pageNumber = (page ?? 1);

            var result = await ConnectAPI.GetAuth("api/comments/?site=" + (pageNumber - 1) + "&size=" + pageSize, Session, User.Identity.Name);
            Root<Comment> content;

            if (result.IsSuccessStatusCode)
            {
                content = JsonConvert.DeserializeObject<Root<Comment>>(await result.Content.ReadAsStringAsync());

                List<Comment> comments = new List<Comment>();
                int counter = 0;

                for (int i = 0; i < content.TotalElements; i++)
                {
                    if ((i >= (pageNumber - 1) * pageSize) && (i < (pageNumber) * pageSize))
                    {
                        comments.Add(content.Content[counter]);
                        counter++;
                        continue;
                    }
                    comments.Add(null);
                }


                return View(comments.ToPagedList(pageNumber, pageSize));
            }
            else
            {
                return  RedirectToAction("Error_API", "Home");
            }
        }

        // GET: Comment/Details/5
        public async Task<ActionResult> Details(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var result = await ConnectAPI.GetAuth("api/comments/" + id, Session, User.Identity.Name);

            Comment comment = null;

            if (result.IsSuccessStatusCode)
            {
                comment = JsonConvert.DeserializeObject<Comment>(await result.Content.ReadAsStringAsync());
            }
            else if (result.StatusCode == HttpStatusCode.NotFound)
            {
                return HttpNotFound();
            }

            return View(comment);
        }

        // GET: Comment/Create
        public ActionResult Create(string postID,string groupID)
        {
            ViewBag.GroupID = groupID;
            return View();
        }

        // POST: Comment/Create
        [HttpPost]
        public async Task<ActionResult> Create(string postId,CommentViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            if(postId == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Comment comment = new Comment()
            {
                Conntent = model.Conntent,
                AuthorId = (string)Session["UserID"],
                CreatedTime = DateTime.Now
            };

            var content = JsonConvert.SerializeObject(comment);

            var httpContent = new StringContent(content, Encoding.UTF8, "application/json");

            var response = await ConnectAPI.PostAuth("api/comments/"+postId, httpContent, Session, User.Identity.Name);

            if (response.IsSuccessStatusCode)
            {
                if (response != null)

                    return RedirectToAction(nameof(Details), nameof(Group), new { id = model.GroupID });
                else
                    return RedirectToAction("Error_API", "Home");
            }
            else
                return RedirectToAction("Error_API", "Home");
        }

        // GET: Comment/Edit/5
        public async Task<ActionResult> Edit(string id, string groupId)
        {
            if(id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var result = await ConnectAPI.GetAuth("api/comments/" + id, Session, User.Identity.Name);

            if (result == null)
            {
                return RedirectToAction("Error_API", "Home");
            }

            Comment comment = null;

            if (result.IsSuccessStatusCode)
            {
                comment = JsonConvert.DeserializeObject<Comment>(await result.Content.ReadAsStringAsync());

                ViewBag.groupId = groupId;

            }
            else if (result.StatusCode == HttpStatusCode.NotFound)
            {
                return HttpNotFound();
            }

            return View(new CommentViewModel() { Conntent=comment.Conntent,GroupID = groupId});
        }

        // POST: Comment/Edit/5
        [HttpPost]
        public async Task<ActionResult> Edit(string id, CommentViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            Comment comment = new Comment()
            {
                Conntent = model.Conntent,
                AuthorId = (string)Session["UserID"],
                CreatedTime = DateTime.Now
            };

            var content = JsonConvert.SerializeObject(comment);

            var httpContent = new StringContent(content, Encoding.UTF8, "application/json");

            var response = await ConnectAPI.PutAuth("api/comments/"+id, httpContent, Session, User.Identity.Name);

            if (response.IsSuccessStatusCode)
            {
                if (response != null)
                    return RedirectToAction(nameof(Details), nameof(Group), new { id = model.GroupID });
                else
                    return RedirectToAction("Error_API", "Home");
            }
            else
                return RedirectToAction("Error_API", "Home");
        }

       

       
        public async Task<ActionResult> Delete(string id, string groupID)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var result = await ConnectAPI.DeleteAuth("api/comments/" + id, Session, User.Identity.Name);
            if (result == null)
            {
                return RedirectToAction("Error_API", "Home");
            }
            if (result.IsSuccessStatusCode)
            {
                return RedirectToAction(nameof(Details), nameof(Group), new { id = groupID });
            }
            else if (result.StatusCode == HttpStatusCode.NotFound)
            {
                return HttpNotFound();
            }
            else
            {
                return RedirectToAction("Error_API", "Home");
            }

        }


        public async Task<ActionResult> CommentsByPostIDPartialView(string postId, int? pageC)
        {
         
           
            int pageComment = (pageC ?? 1);
            int pageCommentSize =12;

            var result = await ConnectAPI.GetAuth("api/posts/" + postId + "?siteC=" + (pageComment - 1)  + "&sizeC=" + pageCommentSize, Session, User.Identity.Name);

            Post content;

            if (result == null)
            {
                return null;
            }

            if (result.IsSuccessStatusCode)
            {
                content = JsonConvert.DeserializeObject<Post>(await result.Content.ReadAsStringAsync());

                ViewBag.Authors = await GetUsersToComm(content.Comments);

                ViewBag.postId = postId;
                ViewBag.isGroupOwner = await isOwner(content.GroupId, (string)Session["UserID"]);
                ViewBag.authorId = (string)Session["UserID"];
                ViewBag.groupID = content.GroupId;
                return PartialView(content.Comments);
            }
            else
            {
                return null;
            }

        }



        private async Task<Dictionary<string,string>> GetUsersToComm(List<Comment> comments)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();

            foreach (var item in comments)
            {
                result.Add(item.Id, await Connect(item.AuthorId));
            }
            return result;
        }

        private async Task<string> Connect(string id)
        {
            var result = await ConnectAPI.GetAuth("api/users/getSimple/"+id, Session, User.Identity.Name);
            SimpleUser content;
            if (result == null)
            {
                return "Not Found";
            }

            if (result.IsSuccessStatusCode)
            {
                content = JsonConvert.DeserializeObject<SimpleUser>(await result.Content.ReadAsStringAsync());
                if (content.Name != null && content.Surname != null)
                {
                    return content.Name + " " + content.Surname.Substring(0, 1);
                }
                return "Anonymous";
            }
            else
            {
                return "Not Found";
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
