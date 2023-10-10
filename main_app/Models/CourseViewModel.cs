using main_app.Models.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace main_app.Models
{
    public class CourseViewModel
    {
        public string Name { get; set; }

        public string Decription { get; set; }

        public string KindID { get; set; }

        public string GroupID { get; set; }

        public List<Lesson> Lessons { get; set; }
    }
}