using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POS_MSC.Data;
using POS_MSC.Helper;
using POS_MSC.Models;
using POS_MSC.Services;
using POS_MSC.ViewModels;

namespace POS_MSC.Controllers
{
    [ServiceFilter(typeof(SessionValidationAttribute))]
    public class ApprovalController : Controller
    {
        private readonly AppDbContext _context;
        Utility util = new Utility();

        public ApprovalController(AppDbContext context)
        {
            _context = context;
        }

        private bool SetSessionData()
        {
            var user = HttpContext.Session.GetString("user");
            var role = HttpContext.Session.GetString("mscApprover");

            if (user == null || role == null)
            {
                return false;
            }

            ViewBag.User = user;
            ViewBag.MSCApprover = role;

            return true;
        }
        public IActionResult Index()
        {
            if (!SetSessionData())
            {
                return RedirectToAction("Index", "Home");
            }

            List<UploadModel> list = new List<UploadModel>();
            list = _context.MSC_Request_Upload.Where(a => a.Status == "1").OrderByDescending(x => x.UploadDate).ToList();
            return View(list);
        }

        [HttpGet]
        public IActionResult ViewList(int uploadID)
        {
            if (!SetSessionData())
            {
                return RedirectToAction("Index", "Home");
            }

            ViewListModel model = new ViewListModel();

            model.Settlements = _context.MSC_Request.Where(a => a.MSC_Request_Upload_ID == uploadID).ToList();
            model.Count = _context.MSC_Request.Where(a => a.MSC_Request_Upload_ID == uploadID).Count();
            model.Sum = (decimal)_context.MSC_Request.Where(a => a.MSC_Request_Upload_ID == uploadID).Sum(a => a.CVAmount);

            return View(model);
        }

        [HttpPost]
        public IActionResult HandleApproval(int uploadID, string comment, string action)
        {
            if (!SetSessionData())
            {
                return RedirectToAction("Index", "Home");
            }

            var userComment = util.DecryptTextWithPrivateKey(comment);
            var userAction = util.DecryptTextWithPrivateKey(action);

            if (string.IsNullOrWhiteSpace(comment))
            {
                TempData["ErrorMessage"] = "Please add a comment.";
                return RedirectToAction("Index", "Approval");
            }

            if (userAction == "Approve")
            {
                Approval(uploadID, userComment);
                TempData["SuccessMessage"] = "Approved successfully!";
            }
            else if (userAction == "Reject")
            {
                Rejection(uploadID, userComment);
                TempData["SuccessMessage"] = "Rejected successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Unknown action.";
            }

            return RedirectToAction("Index", "Approval");
        }

        public void Approval(int Id, string comment)
        {
            var user = HttpContext.Session.GetString("user");

            var uploadToApprove = _context.MSC_Request_Upload.FirstOrDefault(x => x.ID == Id);
            if (uploadToApprove != null)
            {
                uploadToApprove.Status = "2";
                uploadToApprove.Comment = comment;
                _context.SaveChanges();

                util.LogAction(user, "APPROVE UPLOAD", _context);
                //util.WriteToLog($"Error Logging in {}::::{}");
            }
        }

        public void Rejection(int Id, string comment)
        {
            var user = HttpContext.Session.GetString("user");

            var uploadToRemove = _context.MSC_Request_Upload.FirstOrDefault(x => x.ID == Id);
            if (uploadToRemove != null)
            {
                uploadToRemove.Status = "00";
                uploadToRemove.Comment = comment;
                _context.SaveChanges();

                util.LogAction(user, "REJECT UPLOAD", _context);
                //util.WriteToLog($"Error Logging in {}::::{}");
            }
        }

        public IActionResult ProcessedList()
        {
            var user = HttpContext.Session.GetString("user");

            if (!SetSessionData())
            {
                return RedirectToAction("Index", "Home");
            }

            List<UploadModel> list = new List<UploadModel>();
            list = _context.MSC_Request_Upload.Where(r => r.Status == "5" || r.Status == "10").OrderByDescending(x => x.UploadDate).ToList();           
            return View(list);
        }
        public IActionResult ViewProcessedList(int uploadID)
        {
            if (!SetSessionData())
            {
                return RedirectToAction("Index", "Home");
            }

            ViewListModel model = new ViewListModel();

            model.Settlements = _context.MSC_Request.Where(a => a.MSC_Request_Upload_ID == uploadID).ToList();
            model.Count = _context.MSC_Request.Where(a => a.MSC_Request_Upload_ID == uploadID).Count();
            model.Sum = (decimal)_context.MSC_Request.Where(a => a.MSC_Request_Upload_ID == uploadID).Sum(a => a.CVAmount);

            return View(model);
        }

        [HttpGet]
        public IActionResult ViewAuditList(int uploadID)
        {
            if (!SetSessionData())
            {
                return RedirectToAction("Index", "Home");
            }

            List<MSC_AuditLogModel> list = new List<MSC_AuditLogModel>();
            list = _context.MSC_AuditLog.OrderByDescending(a => a.Time).ToList();
            return View(list);
        }
    }
}
