using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POS_MSC.Data;
using POS_MSC.Helper;
using POS_MSC.Models;
using POS_MSC.ViewModels;

namespace POS_MSC.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        Utility util = new Utility();
        private readonly AppDbContext _context;
        public HomeController(ILogger<HomeController> logger, AppDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Index(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                try {
                    var userName = util.DecryptTextWithPrivateKey(model.UserName);
                    var userPassword = util.DecryptTextWithPrivateKey(model.Password);

                    string sessionToken = Guid.NewGuid().ToString();

                    //TEST LOGIN
                    if (userName.ToLower() == "im04220" && userPassword.ToLower() == "app")
                    {

                        //APPROVER
                        HttpContext.Session.SetString("user", userName);
                        HttpContext.Session.SetString("mscApprover", "mscApprover");

                        HttpContext.Session.SetString("sessionToken", sessionToken);
                        util.SaveUserSession(userName, sessionToken, _context);

                        return RedirectToAction("Welcome", "Home");
                    }

                    if (userName.ToLower() == "im04220" && userPassword.ToLower() == "ini")
                    {
                        //INITIATOR
                        HttpContext.Session.SetString("user", userName);
                        HttpContext.Session.SetString("mscInitiator", "mscInitiator");

                        HttpContext.Session.SetString("sessionToken", sessionToken);
                        util.SaveUserSession(userName, sessionToken, _context);

                        return RedirectToAction("Welcome", "Home");
                    }

                    var isValidationSuccessful = ValidateUser(userName, userPassword);
                    var isCreator = HttpContext.Session.GetString("mscInitiator");
                    var isApprover = HttpContext.Session.GetString("mscApprover");

                    if (!isValidationSuccessful)
                    {
                        ModelState.AddModelError("InvalidUsernameOrPassword", "The user name or password provided is incorrect.");
                        return View();
                    }
                    else if (isCreator == null && isApprover == null)
                    {
                        ModelState.AddModelError("Unauthorized", "You don't have access previledge");
                        return View();
                    }
                    else
                    {
                        return RedirectToAction("Welcome", "Home");
                    }
                }
                catch (Exception ex)
                {
                    util.WriteToLog($"{ex}, Error Logging in user");
                }
            }

            ModelState.AddModelError("", "Login failed!");
            return View();
        }

        public IActionResult Welcome()
        {
            if (!SetSessionData())
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }
        public IActionResult LogOut()
        {
            HttpContext.Session.Clear();
            HttpContext.Session.Remove("user");
            //util.WriteToLog($"Error Logging in {}::::{}");
            return RedirectToAction("Index", "Home");
        }

        private bool ValidateUser(string userName, string password)
        {
            var userValidation = new JaizAuthService.JaizRoleManagerServiceClient(0);
            var logModel = new JaizAuthService.LogonModel()
            {
                username = userName,
                password = password,
                appID = 80,
                //ipAddress = SystemIpInfo.GetUserIp(System.Web.HttpContext.Current.Request),
                //appIDSpecified = true
            };

            var result = new JaizAuthService.LoginResult();

            try
            {
                result = userValidation.ValidateADUser2FA(logModel);

                if (result.loggedIn)
                {
                    string sessionToken = Guid.NewGuid().ToString();

                    HttpContext.Session.SetString("user", userName);
                    HttpContext.Session.SetString("sessionToken", sessionToken);
                    util.SaveUserSession(userName, sessionToken, _context);

                    if (result.roles[0] == "mscInitiator")
                    {                        
                        HttpContext.Session.SetString("mscInitiator", "mscInitiator");

                        return true;
                    }
                    else if (result.roles[0] == "mscApprover")
                    {                       
                        HttpContext.Session.SetString("mscApprover", "mscApprover");

                        return true;
                    }
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error Aunthenticating User", ex.Message);
                util.WriteToLog($"Error Aunthenticating {userName}::::{ex.Message}");
            }
            return result.loggedIn;
        }
        private bool SetSessionData()
        {
            var user = HttpContext.Session.GetString("user");
            var mscInitiator = HttpContext.Session.GetString("mscInitiator");
            var mscApprover = HttpContext.Session.GetString("mscApprover");   
            

            if (user == null )
            {
                return false;
            }

            ViewBag.User = user;
            ViewBag.mscInitiator = mscInitiator;
            ViewBag.mscApprover = mscApprover;

            return true;
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
