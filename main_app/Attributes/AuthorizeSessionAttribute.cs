using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace main_app.Attributes
{
    public class AuthorizeSessionAttribute : AuthorizeAttribute
    {
        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            var userId = httpContext.Session["UserID"];

            if (userId == null)
            {
                var auth = httpContext.GetOwinContext().Authentication;
                auth.SignOut(DefaultAuthenticationTypes.ApplicationCookie);

                return false;
            }

            return true;
        }
    }
}