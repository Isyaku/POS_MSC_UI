using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Web;
using POS_MSC.Data;

namespace POS_MSC.Services
{
    public class SessionValidationAttribute : ActionFilterAttribute
    {
        private readonly AppDbContext _db;

        public SessionValidationAttribute(AppDbContext db)
        {
            _db = db;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var httpContext = context.HttpContext;
            var username = httpContext.Session.GetString("user");
            var sessionToken = httpContext.Session.GetString("sessionToken");

            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(sessionToken))
            {
                var userSession = _db.UserSession.FirstOrDefault(u => u.UserID == username);

                if (userSession == null || userSession.SessionToken != sessionToken)
                {
                    // invalidate session
                    httpContext.Session.Clear();
                    context.Result = new RedirectToActionResult("Index", "Home", null);
                }
            }

            base.OnActionExecuting(context);
        }

    }
}
