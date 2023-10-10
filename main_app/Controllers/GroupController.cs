using main_app.Helper;
using main_app.Models.Api;
using main_app.Models.Api.Page;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using PagedList;
using System.Net;
using main_app.Models;
using System.Text;
using System.Net.Http;
using System.IO;
using main_app.Attributes;

namespace main_app.Controllers
{
    [Authorize]
    [AuthorizeSessionAttribute]
    public class GroupController : Controller
    {
        // GET: Group
        public async Task<ActionResult> Index(int? page, string currentFilter, string searchString,bool? me)
        {

            int pageSize = 6;
            int pageNumber = (page ?? 1);
            me = me ?? false;


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
                if (!(bool)me)
                {
                    result = await ConnectAPI.GetAuth("api/groups/?site=" + (pageNumber - 1) + "&size=" + pageSize + "&type=_id", Session, User.Identity.Name);
                }
                else
                {
                    result = await ConnectAPI.GetAuth("api/groups/?owner="+Session["UserID"]+"&site=" + (pageNumber - 1) + "&size=" + pageSize + "&type=_id", Session, User.Identity.Name);
                }
            }
            else
            {
                result = await ConnectAPI.GetAuth("api/groups/query/?query="+searchString+"&site=" + (pageNumber - 1) + "&size=" + pageSize, Session, User.Identity.Name);
            }
           
            Root<Group> content;

            if (result.IsSuccessStatusCode)
            {
                content = JsonConvert.DeserializeObject<Root<Group>>(await result.Content.ReadAsStringAsync());

                List<Group> groups = new List<Group>();
                int counter = 0;

                for (int i = 0; i < content.TotalElements; i++)
                {
                    if ((i >= (pageNumber - 1) * pageSize) && (i < (pageNumber) * pageSize))
                    {
                        groups.Add(content.Content[counter]);
                        counter++;
                        continue;
                    }
                    groups.Add(null);
                }

                ViewBag.showForm = !(bool)me;
                return View(groups.ToPagedList(pageNumber, pageSize));
            }
            return View();
        }

        // GET: Group/Details/5
        public async Task<ActionResult> Details(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

          

            var result = await ConnectAPI.GetAuth("api/groups/" + id, Session, User.Identity.Name);
            Group group = null;
            if (result == null)
            {
                return RedirectToAction("Error_API", "Home");
            }

            if (result.IsSuccessStatusCode)
            {
                group = JsonConvert.DeserializeObject<Group>(await result.Content.ReadAsStringAsync());

                Member member = await isMember(id, (string)Session["UserID"]);

                if (member == null)
                {
                    return RedirectToAction("Error_API", "Home");
                }

                ViewBag.isJoin = member.IsJoin;
                ViewBag.isMember = member.IsMember;
                ViewBag.isOwner = Session["UserID"].Equals(group.OwnerId);
                return View(group);
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

        // GET: Group/Create
        [Authorize(Roles = "ROLE_ADMIN,ROLE_EDUCATOR")]
        public async Task<ActionResult> Create()
        {
            var result = await ConnectAPI.Get("api/kinds/?site=0&size=1000");

            if (result.IsSuccessStatusCode)
            {
                var content = JsonConvert.DeserializeObject<Root<Kind>>(await result.Content.ReadAsStringAsync());


                ViewBag.KindID = new SelectList(content.Content, "Id", "Name");
                return View();
            }
            else
            {
                return RedirectToAction("Error_API", "Home");
            }


        }

        // POST: Group/Create
        [HttpPost]
        [Authorize(Roles = "ROLE_ADMIN,ROLE_EDUCATOR")]
        public async Task<ActionResult> Create([Bind(Include = "KindID,Name")] GroupeModelView model)
        {
            if (ModelState.IsValid)
            {
                var res = await ConnectAPI.Get("api/kinds/" + model.KindID);
                if (res.IsSuccessStatusCode)
                {
                    var kind = JsonConvert.DeserializeObject<Kind>(await res.Content.ReadAsStringAsync());

                    Group group = new Group
                    {
                        Kind = kind,
                        Name = model.Name,
                        OwnerId = (string)Session["UserID"]
                    };

                    var content = JsonConvert.SerializeObject(group);

                    var httpContent = new StringContent(content, Encoding.UTF8, "application/json");

                    HttpPostedFileBase file = Request.Files["main_img"];

                    var contentM = new MultipartFormDataContent();
                    contentM.Add(new StreamContent(new MemoryStream(FileHelper.Convert(file))), "img", file.FileName);
                    contentM.Add(httpContent, "data");

                    var resGroup = await ConnectAPI.PostAuth("api/groups/", contentM, Session, User.Identity.Name);


                    if (resGroup.IsSuccessStatusCode)
                    {
                        if (resGroup != null)
                            return RedirectToAction("Index");
                        else
                            return RedirectToAction("Error_API", "Home");
                    }
                    else
                        return RedirectToAction("Error_API", "Home");
                }
                else
                {
                    return RedirectToAction("Error_API", "Home");
                }
            }

            #region model not valis
            var result = await ConnectAPI.Get("api/kinds/?site=0&size=1000");
            if (result.IsSuccessStatusCode)
            {
                var content = JsonConvert.DeserializeObject<Root<Kind>>(await result.Content.ReadAsStringAsync());


                ViewBag.KindID = new SelectList(content.Content, "Id", "Name");
                return View(model);
            }
            else
            {
                return RedirectToAction("Error_API", "Home");
            }
            #endregion

        }

        // GET: Group/Edit/5
        [Authorize(Roles = "ROLE_ADMIN,ROLE_EDUCATOR")]
        public async Task<ActionResult> Edit(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var result = await ConnectAPI.GetAuth("api/groups/" + id, Session, User.Identity.Name);
            Group group = null;

            if (result.IsSuccessStatusCode)
            {
                group = JsonConvert.DeserializeObject<Group>(await result.Content.ReadAsStringAsync());
            }

            if (result.StatusCode == HttpStatusCode.NotFound)
            {
                return HttpNotFound();
            }

            result = await ConnectAPI.Get("api/kinds/?site=0&size=1000");
            if (result.IsSuccessStatusCode)
            {
                var content = JsonConvert.DeserializeObject<Root<Kind>>(await result.Content.ReadAsStringAsync());


                ViewBag.KindID = new SelectList(content.Content, "Id", "Name");
                return View(new GroupeModelView { ImgSrc = group.ImgSrc, Name = group.Name, KindID = group.Kind.Id });
            }
            else
            {
                return RedirectToAction("Error_API", "Home");
            }
        }

        // POST: Group/Edit/5
        [HttpPost]
        [Authorize(Roles = "ROLE_ADMIN,ROLE_EDUCATOR")]
        public async Task<ActionResult> Edit(string id, [Bind(Include = "KindID,Name")] GroupeModelView model)
        {
            if (ModelState.IsValid)
            {
                if (id == null)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                }

                #region edytowanie i wysyłanie
                var res = await ConnectAPI.Get("api/kinds/" + model.KindID);
                if (res.IsSuccessStatusCode)
                {
                    var kind = JsonConvert.DeserializeObject<Kind>(await res.Content.ReadAsStringAsync());

                    Group group = new Group
                    {
                        Kind = kind,
                        Name = model.Name,
                        OwnerId = (string)Session["UserID"]
                    };

                    var content = JsonConvert.SerializeObject(group);

                    var httpContent = new StringContent(content, Encoding.UTF8, "application/json");

                    var contentM = new MultipartFormDataContent();
                    contentM.Add(httpContent, "data");

                    HttpPostedFileBase file = Request.Files["main_img"];

                    if (file != null && file.ContentLength > 0)
                    {
                        contentM.Add(new StreamContent(new MemoryStream(FileHelper.Convert(file))), "img", file.FileName);
                    }


                    //wysłanie
                    var resGroup = await ConnectAPI.PutAuth("api/groups/" + id, contentM, Session, User.Identity.Name);


                    if (resGroup.IsSuccessStatusCode)
                    {
                        if (resGroup != null)
                            return RedirectToAction("Index");
                        else
                            return RedirectToAction("Error_API", "Home");
                    }
                    else if (resGroup.StatusCode == HttpStatusCode.NotFound)
                    {
                        return HttpNotFound();
                    }
                    else
                    {
                        return RedirectToAction("Error_API", "Home");
                    }
                }
                else
                {
                    return RedirectToAction("Error_API", "Home");
                }
                #endregion
            }

            #region model not valid
            var result = await ConnectAPI.Get("api/kinds/?site=0&size=1000");
            if (result.IsSuccessStatusCode)
            {
                var content = JsonConvert.DeserializeObject<Root<Kind>>(await result.Content.ReadAsStringAsync());


                ViewBag.KindID = new SelectList(content.Content, "Id", "Name");
                return View(model);
            }
            else
            {
                return RedirectToAction("Error_API", "Home");
            }
            #endregion
        }

        // GET: Group/Delete/5
        [Authorize(Roles = "ROLE_ADMIN,ROLE_EDUCATOR")]
        public async Task<ActionResult> Delete(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var result = await ConnectAPI.GetAuth("api/groups/" + id, Session, User.Identity.Name);
            Group group = null;

            if (result.IsSuccessStatusCode)
            {
                group = JsonConvert.DeserializeObject<Group>(await result.Content.ReadAsStringAsync());
            }

            if (result.StatusCode == HttpStatusCode.NotFound)
            {
                return HttpNotFound();
            }
            return View(new GroupeModelView { ImgSrc = group.ImgSrc, Name = group.Name, KindID = group.Kind.Id });
        }

        // POST: Group/Delete/5
        [HttpPost]
        [Authorize(Roles = "ROLE_ADMIN,ROLE_EDUCATOR")]
        public async Task<ActionResult> Delete(string id, FormCollection collection)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            //CheckTokenTime();
            //var result = await ConnectAPI.DeleteAuth("api/groups/" + id, (string)Session["My_JWT"]);
            var result = await ConnectAPI.DeleteAuth("api/groups/" + id, Session, User.Identity.Name);
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



        private async Task CheckTokenTime()
        {
            long currentTime = (long)DateTime.UtcNow.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;


            if ((currentTime + 100000) >= (long)Session["timeEXP"])
            {
                Console.WriteLine("Refreshing TOKEN");
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
                    result_model = JsonConvert.DeserializeObject<Succes_Login>(await response.Content.ReadAsStringAsync());
                    Session["My_JWT"] = result_model.Token;
                    Session["currentTime"] = result_model.CurrentTime;
                    Session["timeEXP"] = result_model.TimeEXP;
                }
            }
        }


        public async Task<ActionResult> MemberInfo(int? page, string groupID,bool? isAccept)
        {
            int pageSize = 10;
            isAccept = isAccept ?? true;
            int pageNumber = (page ?? 1);

            if (groupID == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var result = await ConnectAPI.GetAuth("api/groups/member/getMembers/"+ groupID+ "?isAccept="+isAccept+ "&site="+(pageNumber-1)+"&size="+pageSize, Session, User.Identity.Name);

            if (result == null)
            {
                return RedirectToAction("Error_API", "Home");
            }
            if (result.IsSuccessStatusCode)
            {
                Root<MemberData> rootData = null;

                rootData = JsonConvert.DeserializeObject<Root<MemberData>>(await result.Content.ReadAsStringAsync());


                List<MemberData> members = new List<MemberData>();
                int counter = 0;

                for (int i = 0; i < rootData.TotalElements; i++)
                {
                    if ((i >= (pageNumber - 1) * pageSize) && (i < (pageNumber) * pageSize))
                    {
                        members.Add(rootData.Content[counter]);
                        counter++;
                        continue;
                    }
                    members.Add(null);
                }
                ViewBag.GroupId = groupID;
                ViewBag.isAccept = (bool)isAccept;
                return View(members.ToPagedList(pageNumber, pageSize));
            }
            else
            {
                return RedirectToAction("Error_API", "Home");
            }
        }

        private async Task<Member> isMember(string groupId, string userId)
        {

            var result = await ConnectAPI.GetAuth("api/groups/member/isMember/?aacept=true&groupId=" + groupId + "&user_id=" + userId, Session, User.Identity.Name);
            if (result == null)
            {
                return null;
            }

            if (result.IsSuccessStatusCode)
            {
                Member member = JsonConvert.DeserializeObject<Member>(await result.Content.ReadAsStringAsync());

                return member;
            }

            return null;
        }

        public async Task<ActionResult> Join(string group_id)
        {
            if (group_id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var result = await ConnectAPI.PostAuth("api/groups/member/join/" + group_id,null, Session, User.Identity.Name);
            if (result == null)
            {
                return RedirectToAction("Error_API","Home");
            }

            if (result.IsSuccessStatusCode)
            {
                return Redirect("Details/" + group_id);
            }

            return RedirectToAction("Error_API", "Home");
        }

        public async Task<ActionResult> Leave(string group_id,string userID)
        {
            bool fromGroupDetails = userID == null ? true : false;
            

            userID = userID ?? (string)Session["UserID"];

            if (group_id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var result = await ConnectAPI.DeleteAuth("api/groups/member/left/?groupId=" + group_id+"&userId="+userID, Session, User.Identity.Name);
            if (result == null)
            {
                return RedirectToAction("Error_API", "Home");
            }

            if (result.IsSuccessStatusCode)
            {
                if(fromGroupDetails)
                return Redirect("Details/" + group_id);
                else
                return RedirectToAction("MemberInfo", new { groupID =group_id, isAccept = true });
            }

            return RedirectToAction("Error_API", "Home");
        }

        //educator and admin
        [Authorize(Roles = "ROLE_ADMIN,ROLE_EDUCATOR")]
        public async Task<ActionResult> Accept(string group_id, string userID)
        {
            if (group_id == null || userID==null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var result = await ConnectAPI.PutAuth("api/groups/member/accept/?groupId=" + group_id + "&userId=" + userID, null, Session, User.Identity.Name);
            if (result == null)
            {
                return RedirectToAction("Error_API", "Home");
            }

            if (result.IsSuccessStatusCode)
            {
                    return RedirectToAction("MemberInfo", new { groupID = group_id, isAccept = true });
            }

            return RedirectToAction("Error_API", "Home");
        }

        public async Task<ActionResult> GroupsByMember(int? page)
        {

            int pageSize = 2;
            int pageNumber = (page ?? 1);
            var result = await ConnectAPI.GetAuth("api/groups/member/getGroups/"+Session["UserID"]+"?site=" + (pageNumber - 1) + "&size=" + pageSize, Session, User.Identity.Name);

            if(result == null)
            {
                return RedirectToAction("Error_API", "Home");
            }

            Root<Group> content;

            if (result.IsSuccessStatusCode)
            {
                content = JsonConvert.DeserializeObject<Root<Group>>(await result.Content.ReadAsStringAsync());

                List<Group> groups = new List<Group>();
                int counter = 0;

                for (int i = 0; i < content.TotalElements; i++)
                {
                    if ((i >= (pageNumber - 1) * pageSize) && (i < (pageNumber) * pageSize))
                    {
                        groups.Add(content.Content[counter]);
                        counter++;
                        continue;
                    }
                    groups.Add(null);
                }


                return View(groups.ToPagedList(pageNumber, pageSize));
            }
            else
            {
                return RedirectToAction("Error_API", "Home");
            }
        }

        [Authorize(Roles = "ROLE_ADMIN,ROLE_EDUCATOR")]
        public async Task<ActionResult> AddCourse(string GropuID, string Id)
        {
            if(GropuID == null || Id == null)
            {
                return RedirectToAction("Error_API", "Home");
            }

            var result = await ConnectAPI.PostAuth("api/groups/course/add/"+GropuID+ "?courseId="+Id, null, Session, User.Identity.Name);

            if (result == null)
            {
                return RedirectToAction("Error_API", "Home");
            }

            if (result.IsSuccessStatusCode)
            {
                return RedirectToAction("Details",new {id = GropuID});
            }

            return RedirectToAction("Error_API", "Home");
        }

        [Authorize(Roles = "ROLE_ADMIN,ROLE_EDUCATOR")]
        public async Task<ActionResult> DeleteCourse(string GropuID, string Id)
        {
            if (GropuID == null || Id == null)
            {
                return RedirectToAction("Error_API", "Home");
            }

            var result = await ConnectAPI.DeleteAuth("api/groups/course/remove/" + GropuID + "?courseId=" + Id, Session, User.Identity.Name);

            if (result == null)
            {
                return RedirectToAction("Error_API", "Home");
            }

            if (result.IsSuccessStatusCode)
            {
                return RedirectToAction("Details", new { id = GropuID });
            }

            return RedirectToAction("Error_API", "Home");
        }
    }

}
