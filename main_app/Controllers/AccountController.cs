using System;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using main_app.Models;
using Newtonsoft.Json;
using main_app.Models.Api;
using System.Net.Http;
using System.Text;
using System.Net;
using main_app.Helper;
using System.Collections.Generic;
using System.IO;

namespace main_app.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private ApplicationSignInManager _signInManager;
        private ApplicationUserManager _userManager;

        //klasa do łączenia z API
        private MyAPI myAPI = new MyAPI();
        private HttpClient httpClient;
        public AccountController()
        {
            httpClient = myAPI.Init();
        }

        public AccountController(ApplicationUserManager userManager, ApplicationSignInManager signInManager)
        {
            UserManager = userManager;
            SignInManager = signInManager;
            httpClient = myAPI.Init();
        }

        public ApplicationSignInManager SignInManager
        {
            get
            {
                return _signInManager ?? HttpContext.GetOwinContext().Get<ApplicationSignInManager>();
            }
            private set 
            { 
                _signInManager = value; 
            }
        }

        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }

        //
        // GET: /Account/Login
        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        //
        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        //[ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginViewModel model, string returnUrl)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.ErorrData = "Invalid data";
                return View(model);
            }

            var content = JsonConvert.SerializeObject(new Login_API
            {
                Password = model.Password,
                Username = model.Email
            });

            Succes_Login result_model = null;

            var httpContent = new StringContent(content, Encoding.UTF8, "application/json");
            
            var response = await httpClient.PostAsync("login", httpContent);

            if (response.IsSuccessStatusCode)
            {

                string temp = await response.Content.ReadAsStringAsync();
                if(temp.Contains("Niepoprawne dane"))
                {
                    ViewBag.ErorrData = "Invalid data";
                    return View(model);
                }


                result_model = JsonConvert.DeserializeObject<Succes_Login>(temp);

                var claims = new List<Claim>();
                claims.Add(new Claim(ClaimTypes.Name, result_model.Username));
                claims.Add(new Claim(ClaimTypes.Email, result_model.Username));
                result_model.Roles.ForEach(r=>
                {
                    claims.Add(new Claim(ClaimTypes.Role,r.Authority));
                });

                var claimsIdentity = new ClaimsIdentity(claims, DefaultAuthenticationTypes.ApplicationCookie);
                AuthenticationManager.SignIn(claimsIdentity);

                Session["My_JWT"] = result_model.Token;
                Session["currentTime"] = result_model.CurrentTime;
                Session["timeEXP"] = result_model.TimeEXP;
                Session["UserID"] = result_model.UserID;
                Session["noooo"] = model.Password;

                return RedirectToLocal(returnUrl);
            }
            else
            {
                return RedirectToAction("Error_API", "Home");
            }
            
        }

        //
        // GET: /Account/VerifyCode
        [AllowAnonymous]
        public async Task<ActionResult> VerifyCode(string provider, string returnUrl, bool rememberMe)
        {
            // Require that the user has already logged in via username/password or external login
            if (!await SignInManager.HasBeenVerifiedAsync())
            {
                return View("Error");
            }
            return View(new VerifyCodeViewModel { Provider = provider, ReturnUrl = returnUrl, RememberMe = rememberMe });
        }

        //
        // POST: /Account/VerifyCode
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> VerifyCode(VerifyCodeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // The following code protects for brute force attacks against the two factor codes. 
            // If a user enters incorrect codes for a specified amount of time then the user account 
            // will be locked out for a specified amount of time. 
            // You can configure the account lockout settings in IdentityConfig
            var result = await SignInManager.TwoFactorSignInAsync(model.Provider, model.Code, isPersistent:  model.RememberMe, rememberBrowser: model.RememberBrowser);
            switch (result)
            {
                case SignInStatus.Success:
                    return RedirectToLocal(model.ReturnUrl);
                case SignInStatus.LockedOut:
                    return View("Lockout");
                case SignInStatus.Failure:
                default:
                    ModelState.AddModelError("", "Invalid code.");
                    return View(model);
            }
        }

        //
        // GET: /Account/Register
        [AllowAnonymous]
        public ActionResult Register()
        {
            return View();
        }

        //
        // POST: /Account/Register
        [HttpPost]
        [AllowAnonymous]
        public async Task<ActionResult> Register(RegisterViewModel model)
        {
            var content = JsonConvert.SerializeObject(new UserRegister
            {
                Email = model.Email,
                ImgSrc = "dummy.png",
                Password = model.Password,
                PhoneNumber = model.PhoneNumber,
                Name = model.Name,
                Surename = model.Surname
            });

            var httpContent = new StringContent(content, Encoding.UTF8, "application/json");
            

            HttpPostedFileBase file = Request.Files["fileWithImg"];
            MemoryStream target = new MemoryStream();
            file.InputStream.CopyTo(target); 
            byte[] data = target.ToArray();

            var contentM = new MultipartFormDataContent();
            contentM.Add(new StreamContent(new MemoryStream(data)), "img", file.FileName);
            contentM.Add(httpContent, "data");

            var response = await httpClient.PostAsync("register?isEducator="+model.isEducator, contentM);
            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Succes", "Home");
            }
            else if (response.StatusCode == HttpStatusCode.Conflict)
            {
                return RedirectToAction("Code_409", "Home");
            }

            return RedirectToAction("Index", "Home");
        }

      
        [AllowAnonymous]
        public ActionResult ResetPassword(string code)
        {
            return code == null ? View("Error") : View();
        }

        //
        // POST: /Account/ResetPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult ResetPassword(ResetPasswordViewModel model)
        {
            /*  if (!ModelState.IsValid)
              {
                  return View(model);
              }
              var user = await UserManager.FindByNameAsync(model.Email);
              if (user == null)
              {
                  // Don't reveal that the user does not exist
                  return RedirectToAction("ResetPasswordConfirmation", "Account");
              }
              var result = await UserManager.ResetPasswordAsync(user.Id, model.Code, model.Password);
              if (result.Succeeded)
              {
                  return RedirectToAction("ResetPasswordConfirmation", "Account");
              }
              AddErrors(result);
              return View();*/
            return null;
        }

        //
        // GET: /Account/ResetPasswordConfirmation
        [AllowAnonymous]
        public ActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        

        //
        // POST: /Account/LogOff
        [HttpPost]
       // [ValidateAntiForgeryToken]
        public ActionResult LogOff()
        {
            AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
            Session["My_JWT"] = null;
            Session["currentTime"] = null;
            Session["timeEXP"] = null;
            Session["noooo"] = null;

            return RedirectToAction("Index", "Home");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_userManager != null)
                {
                    _userManager.Dispose();
                    _userManager = null;
                }

                if (_signInManager != null)
                {
                    _signInManager.Dispose();
                    _signInManager = null;
                }
            }
            httpClient.Dispose();
            base.Dispose(disposing);
        }

        #region Helpers
        // Used for XSRF protection when adding external logins
        private const string XsrfKey = "XsrfId";

        private IAuthenticationManager AuthenticationManager
        {
            get
            {
                return HttpContext.GetOwinContext().Authentication;
            }
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }

        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }

        internal class ChallengeResult : HttpUnauthorizedResult
        {
            public ChallengeResult(string provider, string redirectUri)
                : this(provider, redirectUri, null)
            {
            }

            public ChallengeResult(string provider, string redirectUri, string userId)
            {
                LoginProvider = provider;
                RedirectUri = redirectUri;
                UserId = userId;
            }

            public string LoginProvider { get; set; }
            public string RedirectUri { get; set; }
            public string UserId { get; set; }

            public override void ExecuteResult(ControllerContext context)
            {
                var properties = new AuthenticationProperties { RedirectUri = RedirectUri };
                if (UserId != null)
                {
                    properties.Dictionary[XsrfKey] = UserId;
                }
                context.HttpContext.GetOwinContext().Authentication.Challenge(properties, LoginProvider);
            }
        }
        #endregion
    }
}