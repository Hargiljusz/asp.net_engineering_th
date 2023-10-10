using main_app.Attributes;
using main_app.Helper;
using main_app.Models.Api;
using main_app.Models.Api.Page;
using Newtonsoft.Json;
using PagedList;
using System;
using System.Collections;
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
    public class CourseController : Controller
    {
        // GET: Course
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
                    result = await ConnectAPI.GetAuth("api/courses/?site=" + (pageNumber - 1) + "&size=" + pageSize + "&published=1", Session, User.Identity.Name);
                }
                else
                {
                    result = await ConnectAPI.GetAuth("api/courses/?authorID="+Session["UserID"]+"&site=" + (pageNumber - 1) + "&size=" + pageSize, Session, User.Identity.Name);
                }
            }
            else
            {
                 result = await ConnectAPI.GetAuth("api/courses/query?query="+ searchString + "&site=" + (pageNumber - 1) + "&size=" + pageSize, Session, User.Identity.Name);
            }

            Root<Course> rootCourses = null;

            if (result == null)
            {
                return RedirectToAction("Error_API", "Home");
            }
            if (result.IsSuccessStatusCode)
            {
                rootCourses = JsonConvert.DeserializeObject<Root<Course>>(await result.Content.ReadAsStringAsync());

                List<Course> courses = new List<Course>();
                int counter = 0;

                for (int i = 0; i < rootCourses.TotalElements; i++)
                {
                    if ((i >= (pageNumber - 1) * pageSize) && (i < (pageNumber) * pageSize))
                    {
                        courses.Add(rootCourses.Content[counter]);
                        counter++;
                        continue;
                    }
                    courses.Add(null);
                }

                ViewBag.showForm = !(bool)me;
                return View(courses.ToPagedList(pageNumber, pageSize));
            }
            else
            {
                return RedirectToAction("Error_API", "Home");
            }
        }

        // GET: Course/Details/5
        public async Task<ActionResult> Details(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var result = await ConnectAPI.GetAuth("api/courses/" + id, Session, User.Identity.Name);


            if (result == null )
            {
                return RedirectToAction("Error_API", "Home");
            }
            if (result.IsSuccessStatusCode )
            {
                Course course = JsonConvert.DeserializeObject<Course>(await result.Content.ReadAsStringAsync());

                course.Lessons.ForEach(l =>
                {
                    string vl = l.VideoLink;
                    if (vl != null && vl != "" && vl.Contains("youtube.com"))
                    {
                        vl = vl.Replace("watch?v=", "embed/");
                    }
                    l.VideoLink = vl;
                });

                if (User.IsInRole("ROLE_EDUCATOR"))
                {
                    ViewBag.GropuID = new SelectList(await getGroupsByOwnerID((string)Session["UserID"],id), "Id", "Name");
                }
                return View(course);
            }
            else
            {
                return HttpNotFound();
            }

        }


        // GET: Course/Create
        [Authorize(Roles = "ROLE_ADMIN,ROLE_EDUCATOR")]
        public async Task<ActionResult> Create()
        {
            var result = await ConnectAPI.Get("api/kinds/?site=0&size=1000");
            var resultG = await ConnectAPI.GetAuth("api/groups/?site=0&size=1000&owner=" + Session["UserID"], Session, User.Identity.Name);

            if (result == null || resultG == null)
            {
                return RedirectToAction("Error_API", "Home");
            }

            if (result.IsSuccessStatusCode && resultG.IsSuccessStatusCode)
            {
                var content = JsonConvert.DeserializeObject<Root<Kind>>(await result.Content.ReadAsStringAsync());

                var contentG = JsonConvert.DeserializeObject<Root<Group>>(await resultG.Content.ReadAsStringAsync());

                List<Group> groups = contentG.Content;

                if (groups == null)
                {
                    groups = new List<Group>();
                }

                groups.Insert(0, new Group { Name = "Do not assign to", Id = "-1000" });

                ViewBag.GropuID = new SelectList(groups, "Id", "Name");
                ViewBag.KindID = new SelectList(content.Content, "Id", "Name");
                return View();
            }
            else
            {
                return RedirectToAction("Error_API", "Home");
            }
        }

        // POST: Course/Create
        [HttpPost]
        [Authorize(Roles = "ROLE_ADMIN,ROLE_EDUCATOR")]
        public async Task<ActionResult> Create(FormCollection collection)
        {
            if (ModelState.IsValid)
            {

                #region pobieranie danych z formularza
                //Course
                var name = collection.GetValues("Name")[0];
                var decription = collection.GetValues("Decription")[0];
                var kindID = collection.GetValues("KindID")[0];
                var gropuID = collection.GetValues("GropuID")[0];

                //Lessons
                var subject = collection.GetValues("Subject");
                var conntent = collection.GetValues("Conntent");
                var homework = collection.GetValues("Homework");
                var videolink = collection.GetValues("VideoLink");
                var number = collection.GetValues("number");
                #endregion

                #region tworzenie lekcji
                List<Lesson> lessons = new List<Lesson>();
                for (int i = 0; i < number.Length; i++)
                {
                    List<string> fileList = new List<string>();
                    lessons.Add(new Lesson()
                    {
                        Subject = subject[i],
                        Conntent = conntent[i],
                        Homework = homework[i],
                        VideoLink = videolink[i],
                        Number = Int32.Parse(number[i]),
                        FileSrc = fileList
                    });
                }
                #endregion

                #region tworzenie kursu
                Course course = new Course()
                {
                    Name = name,
                    Decription = decription,
                    Lessons = lessons,
                    PublishedDate = DateTime.Now,
                    AuthorId = (string)Session["UserID"]
                };

                if (gropuID.Equals("-1000"))
                {
                    course.Published = true;
                }
                else
                {
                    course.GroupeId = gropuID;
                    course.Published = false;
                }

                #region ustawienie obiektu kind
                var res = await ConnectAPI.Get("api/kinds/" +kindID);
                if(res == null)
                {
                    return RedirectToAction("Error_API", "Home");
                }
                if (res.IsSuccessStatusCode)
                {
                    course.Kind = JsonConvert.DeserializeObject<Kind>(await res.Content.ReadAsStringAsync());
                }
                else
                {
                    return RedirectToAction("Error_API", "Home");
                }
                #endregion

                #endregion

                #region tworznie HttpContent'u do wysłania
                var contentM = new MultipartFormDataContent();
                foreach (var lessonId in number)
                {
                    string attrName = "filesLesson" + lessonId;

                    IList<HttpPostedFileBase> files = Request.Files.GetMultiple(attrName);

                    foreach (var file in files)
                    {
                        if (file.ContentLength > 0)
                        {
                            string fName = "lesson" + lessonId + "," + file.FileName;
                            contentM.Add(new StreamContent(new MemoryStream(FileHelper.Convert(file))), "files", fName);
                        }
                    }
                }

                var content = JsonConvert.SerializeObject(course);

                var httpContent = new StringContent(content, Encoding.UTF8, "application/json");
                contentM.Add(httpContent, "data");
                #endregion


                var resultAPI = await ConnectAPI.PostAuth("api/courses/", contentM, Session, User.Identity.Name);

                if (resultAPI.IsSuccessStatusCode)
                {
                    return RedirectToAction("Index");
                }
                else if(resultAPI.StatusCode== HttpStatusCode.NotFound)
                {
                    return HttpNotFound();
                }
            }

            return RedirectToAction("Error_API", "Home");
        }

        // GET: Course/Edit/5
        [Authorize(Roles = "ROLE_ADMIN,ROLE_EDUCATOR")]
        public async Task<ActionResult> Edit(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var result = await ConnectAPI.Get("api/kinds/?site=0&size=1000");

            if (result == null)
            {
                return RedirectToAction("Error_API", "Home");
            }

            if (result.IsSuccessStatusCode)
            {
                var content = JsonConvert.DeserializeObject<Root<Kind>>(await result.Content.ReadAsStringAsync());

                var courseJson =  await ConnectAPI.GetAuth("api/courses/" + id, Session, User.Identity.Name);

                if (courseJson == null)
                {
                    return RedirectToAction("Error_API", "Home");
                }
                if (courseJson.IsSuccessStatusCode)
                {
                    Course course = JsonConvert.DeserializeObject<Course>(await courseJson.Content.ReadAsStringAsync());

                    ViewBag.KindID = new SelectList(content.Content, "Id", "Name");
                    ViewBag.Current_Counter = course.Lessons[course.Lessons.Count - 1].Number;
                    return View(course);
                }
                else
                {
                    return HttpNotFound();
                }

            }
            else
            {
                return RedirectToAction("Error_API", "Home");
            }
        }

        // POST: Course/Edit/5
        [HttpPost]
        [Authorize(Roles = "ROLE_ADMIN,ROLE_EDUCATOR")]
        public async Task<ActionResult> Edit(string id, FormCollection collection)
        {
            #region pobieranie danych z formularza
            //Course
            var name = collection.GetValues("Name")[0];
            var decription = collection.GetValues("Decription")[0];
            var kindID = collection.GetValues("KindID")[0];
            var published = collection.GetValues("Published")[0];

            //Lessons
            var subject = collection.GetValues("Subject");
            var conntent = collection.GetValues("Conntent");
            var homework = collection.GetValues("Homework");
            var videolink = collection.GetValues("VideoLink");
            var number = collection.GetValues("number");

            #endregion

            #region tworzenie lekcji
            List<Lesson> lessons = new List<Lesson>();
            for (int i = 0; i < number.Length; i++)
            {
                List<string> fileList = new List<string>();
                lessons.Add(new Lesson()
                {
                    Subject = subject[i],
                    Conntent = conntent[i],
                    Homework = homework[i],
                    VideoLink = videolink[i],
                    Number = Int32.Parse(number[i]),
                    FileSrc = fileList
                });
            }
            #endregion

            #region tworzenie kursu
            Course course = new Course()
            {
                Name = name,
                Decription = decription,
                Published = published == "true" ? true : false,
                Lessons = lessons,
                PublishedDate = DateTime.Now
            };
            #region ustawienie obiektu kind
            var res = await ConnectAPI.Get("api/kinds/" + kindID);
            if (res == null)
            {
                return RedirectToAction("Error_API", "Home");
            }
            if (res.IsSuccessStatusCode)
            {
                course.Kind = JsonConvert.DeserializeObject<Kind>(await res.Content.ReadAsStringAsync());
            }
            else
            {
                return RedirectToAction("Error_API", "Home");
            }
            #endregion
            #endregion

            #region tworzenie contentu
            var contentM = new MultipartFormDataContent();
            foreach (var lessonId in number)
            {
                string attrName = "filesLesson" + lessonId;

                IList<HttpPostedFileBase> files = Request.Files.GetMultiple(attrName);

                foreach (var file in files)
                {
                    if (file.ContentLength > 0)
                    {
                        string fName = "lesson" + lessonId + "," + file.FileName;
                        contentM.Add(new StreamContent(new MemoryStream(FileHelper.Convert(file))), "files", fName);
                    }
                }
            }
            var content = JsonConvert.SerializeObject(course);

            var httpContent = new StringContent(content, Encoding.UTF8, "application/json");
            contentM.Add(httpContent, "data");
            #endregion

            #region wysyłanie
            var resultAPI = await ConnectAPI.PutAuth("api/courses/"+id, contentM, Session, User.Identity.Name);

            if (resultAPI.IsSuccessStatusCode)
            {
                return RedirectToAction("Index", "Home");
            }
            else if(resultAPI.StatusCode == HttpStatusCode.NotFound)
            {
                return HttpNotFound();
            }

            return RedirectToAction("Error_API", "Home");
            #endregion
        }

        // GET: Course/Delete/5
        [Authorize(Roles = "ROLE_ADMIN,ROLE_EDUCATOR")]
        public async Task<ActionResult> Delete(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var result = await ConnectAPI.GetAuth("api/courses/" + id, Session, User.Identity.Name);
            if (result == null)
            {
                return RedirectToAction("Error_API", "Home");
            }
            if (result.IsSuccessStatusCode)
            {
                Course course = JsonConvert.DeserializeObject<Course>(await result.Content.ReadAsStringAsync());
                return View(course);
            }
            else
            {
                return HttpNotFound();
            }

        }

        // POST: Course/Delete/5
        [HttpPost]
        [Authorize(Roles = "ROLE_ADMIN,ROLE_EDUCATOR")]
        public async Task<ActionResult> Delete(String id, FormCollection collection)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var result = await ConnectAPI.DeleteAuth("api/courses/" + id, Session, User.Identity.Name);
            if (result == null)
            {
                return RedirectToAction("Error_API", "Home");
            }
            if (result.IsSuccessStatusCode)
            {
              
                return RedirectToAction("Index");
            }
            else
            {
                return HttpNotFound();
            }
        }

        [Authorize(Roles = "ROLE_ADMIN,ROLE_EDUCATOR")]
        public async Task<ActionResult> CoursesByGroup(int? page, string groupID,string userID)
        {
            int pageSize =9;
            int pageNumber = (page ?? 1);
            var result = await ConnectAPI.GetAuth("api/groups/course/"+groupID+"?site=" + (pageNumber - 1) + "&size=" + pageSize, Session, User.Identity.Name);

            Root<Course> rootCourses = null;
            if (result == null)
            {
                return RedirectToAction("Error_API", "Home");
            }
            if (result.IsSuccessStatusCode)
            {
                rootCourses = JsonConvert.DeserializeObject<Root<Course>>(await result.Content.ReadAsStringAsync());

                List<Course> courses = new List<Course>();
                int counter = 0;

                for (int i = 0; i < rootCourses.TotalElements; i++)
                {
                    if ((i >= (pageNumber - 1) * pageSize) && (i < (pageNumber) * pageSize))
                    {
                        courses.Add(rootCourses.Content[counter]);
                        counter++;
                        continue;
                    }
                    courses.Add(null);
                }

                ViewBag.GroupID = groupID;

                return View(courses.ToPagedList(pageNumber, pageSize));
            }
            else
            {
                return RedirectToAction("Error_API", "Home");
            }
        }

        private List<string> EditFileHelper(int number, FormCollection collection)
        {
            return collection.GetValues("fileNameForLesson" + number)[0].Split(',').ToList<string>();
        }
        private async Task<List<Group>> getGroupsByOwnerID(string v,string cId)
        {

            var resultG = await ConnectAPI.GetAuth("api/groups/?site=0&size=1000&owner=" + v, Session, User.Identity.Name);

            if (resultG == null)
            {
                return null;
            }
            if (resultG.IsSuccessStatusCode)
            {
                var contentG = JsonConvert.DeserializeObject<Root<Group>>(await resultG.Content.ReadAsStringAsync());

                List<Group> groups = contentG.Content;
                var temp = new List<Group>();

                foreach (var item in groups)
                {
                    var test = await Check(item.Id, cId);
                    if (!test)
                    {
                        temp.Add(item);
                    }
                }


                return temp;
            }
            else
            {
                return null;
            }

        }
        private async Task<bool>Check(string groupId, string courseId)
        {
            var result = await ConnectAPI.GetAuth("api/groups/check/"+groupId+ "?courseId=" + courseId, Session, User.Identity.Name);
            if (result.IsSuccessStatusCode)
            {
                var value = JsonConvert.DeserializeObject<bool>(await result.Content.ReadAsStringAsync());
                return value;
            }
            return true;
        }
    }
}
