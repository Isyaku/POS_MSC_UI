using Microsoft.AspNetCore.Mvc;
using POS_MSC.Data;
using POS_MSC.Models;
using POS_MSC.ViewModels;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using POS_MSC.Helper;
using System.Text;
using Microsoft.EntityFrameworkCore;
using POS_MSC.Services;
using System.Data;
using IOFile = System.IO.File;

namespace POS_MSC.Controllers
{
    [ServiceFilter(typeof(SessionValidationAttribute))]
    public class UploadController : Controller
    {
        private readonly AppDbContext _context;
        private readonly PersonelDbContext _personelContex;
        Utility util = new Utility();

        public UploadController(AppDbContext context, PersonelDbContext personelContext)
        {
            _context = context;
            _personelContex = personelContext;
        }
        private bool SetSessionData()
        {
            var user = HttpContext.Session.GetString("user");
            var role = HttpContext.Session.GetString("mscInitiator");

            if (user == null || role == null)
            {
                return false;
            }

            ViewBag.User = user;
            ViewBag.mscInitiator = role;

            return true;
        }
        public IActionResult UploadCSV()
        {

            if (!SetSessionData())
            {
                return RedirectToAction("Index", "Home");
            }

            return View(new UploadViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> UploadCSV(UploadViewModel model)
        {
            var user = HttpContext.Session.GetString("user");

            if (!SetSessionData())
                return RedirectToAction("Index", "Home");

            if (model?.ExcelFile == null || model.ExcelFile.Length == 0 || !Path.GetExtension(model.ExcelFile.FileName).Equals(".csv", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError("", "Please upload a valid CSV file with .csv extension.");
                return View(new UploadViewModel());
            }

            if (!ModelState.IsValid)
                return View(new UploadViewModel());

            int uploadId = 0;

            try
            {
                var now = DateTime.Now;
                var valueDate = now.ToString("dd/MM/yyyy");
                var batchSessionId = util.GetSessionID();

                var supervisor = util.GetSupervisor(user, _personelContex);

                var upload = new UploadModel
                {
                    StaffID = user,
                    UploadDate = valueDate,
                    Status = "New",
                    CreditAccount = AccountModel.CreditAcct,
                    TransientAccount = AccountModel.TransientAcct,
                    Supervisor = supervisor,
                    TransactionId = batchSessionId,
                    ProgressPercentage = 0
                };

                _context.MSC_Request_Upload.Add(upload);
                await _context.SaveChangesAsync();

                uploadId = upload.ID;

                var originalFolderPath = util.ConfigHelper("appConfiguration:Folders:Incoming");
                var filteredFolderPath = util.ConfigHelper("appConfiguration:Folders:Filtered");

                Directory.CreateDirectory(originalFolderPath);
                Directory.CreateDirectory(filteredFolderPath);

                var originalFile = Path.Combine(originalFolderPath, $"{uploadId}_original.csv");
                var filteredFile = Path.Combine(filteredFolderPath, $"{uploadId}_filtered.csv");

                // Save original file
                using (var stream = new FileStream(originalFile, FileMode.Create))
                {
                    await model.ExcelFile.CopyToAsync(stream);
                }

                var validCategories = new HashSet<string>
                {
                    "POS(FUEL STATION)PURCHASE",
                    "POS(WHOLESALE_ACQUIRER_BORNE)PURCHASE",
                    "POS(WHOLESALE)PURCHASE"
                };

                int filteredCount = 0;

                using (var reader = new StreamReader(originalFile))
                using (var csvReader = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    PrepareHeaderForMatch = args => args.Header.Trim(),
                    MissingFieldFound = null,
                    HeaderValidated = null
                }))
                using (var writer = new StreamWriter(filteredFile))
                using (var csvWriter = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    var records = csvReader.GetRecords<dynamic>();

                    bool headerWritten = false;

                    foreach (var record in records)
                    {
                        string GetValue(string key)
                        {
                            var val = ((IDictionary<string, object>)record).TryGetValue(key, out var v) ? v?.ToString() : "";

                            if (string.IsNullOrWhiteSpace(val))
                                return "";

                            return val.Trim();
                        }

                        var settlementImpactDesc = GetValue("Settlement_Impact_Desc");
                        var trxnCategory = GetValue("trxn_category");

                        var impact = settlementImpactDesc.Trim();
                        var category = trxnCategory.Trim();

                        if (impact == "Acquirer_fee_payable" && validCategories.Contains(category))
                        {
                            if (!headerWritten)
                            {
                                foreach (var key in ((IDictionary<string, object>)record).Keys)
                                {
                                    csvWriter.WriteField(key);
                                }
                                csvWriter.NextRecord();
                                headerWritten = true;
                            }

                            foreach (var value in ((IDictionary<string, object>)record).Values)
                            {
                                csvWriter.WriteField(value);
                            }

                            csvWriter.NextRecord();
                            filteredCount++;
                        }
                    }
                }
                util.LogAction(user, "DOCUMENT UPLOAD", _context);

                TempData["SuccessMessage"] = $"Upload successful. {filteredCount} records staged.";
            }
            catch (Exception ex)
            {
                DeleteUpload(uploadId);

                ModelState.AddModelError("", "Upload failed!");

                var err = $@"Error: {ex.Message} Inner: {ex.InnerException?.Message} Module: UploadCSV >> StackTrace: {ex.StackTrace}";

                util.WriteToLog(err);

                return View(new UploadViewModel());
            }

            return RedirectToAction("UploadList", "Upload");
        }

        [HttpGet]
        public IActionResult UploadList()
        {
            if (!SetSessionData())
            {
                return RedirectToAction("Index", "Home");
            }

            var user = HttpContext.Session.GetString("user");

            List<UploadModel> list = new List<UploadModel>();
            list = _context.MSC_Request_Upload.Where(r => r.StaffID == user && (r.Status == "0" || r.Status == "New")).OrderByDescending(x => x.ID).ToList();
            //util.WriteToLog($"Error Logging in {}::::{}");
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
        public IActionResult RejectedUploadList(int uploadID)
        {
            var user = HttpContext.Session.GetString("user");

            if (!SetSessionData())
            {
                return RedirectToAction("Index", "Home");
            }
            List<UploadModel> list = new List<UploadModel>();
            list = _context.MSC_Request_Upload.Where(r => r.Status == "00" && r.StaffID == user).OrderByDescending(x => x.UploadDate).ToList();
            //util.WriteToLog($"Error Logging in {}::::{}");
            return View(list);
        }
        public IActionResult ApprovedUploadList(int uploadID)
        {
            var user = HttpContext.Session.GetString("user");

            if (!SetSessionData())
            {
                return RedirectToAction("Index", "Home");
            }

            List<UploadModel> list = new List<UploadModel>();
            list = _context.MSC_Request_Upload.Where(r => r.Status == "2" && r.StaffID == user).OrderByDescending(x => x.UploadDate).ToList();
            //util.WriteToLog($"Error Logging in {}::::{}");
            return View(list);
        }
        public IActionResult ProcessedList()
        {
            var user = HttpContext.Session.GetString("user");

            if (!SetSessionData())
            {
                return RedirectToAction("Index", "Home");
            }

            List<UploadModel> list = new List<UploadModel>();
            list = _context.MSC_Request_Upload.Where(r => r.Status == "10" && r.StaffID == user).OrderByDescending(x => x.UploadDate).ToList();
            return View(list);
        }
        public void Delete(int Id)
        {
            var user = HttpContext.Session.GetString("user");

            List<MSC_RequestModel> recordsToRemove = _context.MSC_Request.Where(x => x.MSC_Request_Upload_ID == Id).ToList();
            if (recordsToRemove.Any())
            {
                _context.MSC_Request.RemoveRange(recordsToRemove);
                _context.SaveChanges();
            }

            var uploadToRemove = _context.MSC_Request_Upload.FirstOrDefault(x => x.ID == Id);
            if (uploadToRemove != null)
            {
                _context.MSC_Request_Upload.Remove(uploadToRemove);
                _context.SaveChanges();
            }

            util.LogAction(user, "DELETE UPLOAD", _context);
            //util.WriteToLog($"Error Logging in {}::::{}");
        }
        public void UpdateUploadDetails(int Id)
        {
            var record = _context.MSC_Request_Upload.FirstOrDefault(x => x.ID == Id);

            if (record != null)
            {
                decimal amount = _context.MSC_Request.Where(a => a.MSC_Request_Upload_ID == Id).Sum(a => (decimal?)a.CVAmount) ?? 0;

                record.TotalAmount = amount;
                _context.SaveChanges();
            }
        }

        [HttpGet]
        public async Task<IActionResult> DownloadGenex(int uploadID)
        {
            var user = HttpContext.Session.GetString("user");

            var records = await _context.MSC_Request
                .Where(r => r.MSC_Request_Upload_ID == uploadID)
                .Select(r => new MSC_Request_Download_Model
                {
                    Status = (r.Status == "10") ? "Successful" : "Failed",
                    DebitAcct = r.DebitAcct,
                    CreditAcct = r.CreditAcct,
                    CompanyCode = r.CompanyCode,
                    BranchCode = r.BranchCode,
                    Currency = r.Currency,
                    GLCode = r.GLCode,
                    CIFNO = r.CIFNO,
                    Serial = r.Serial,
                    Void1 = "",
                    Void2 = "",
                    Void3 = "",
                    CVAmount = r.CVAmount ?? 0,
                    FVAmount = r.FCAmount,
                    ValueDate = r.ValueDate,
                    Description1 = r.Description,
                    TRXCode = r.TRXCode,
                    JVType = r.JVType,
                    TrateDate = r.TrateDate,
                    Description2 = r.Description,
                    TransactionId = r.TransactionId,
                    TransCategory = r.TransCategory,
                    SettlementDescription = r.SettlementDescription
                })
                .ToListAsync();

            if (!records.Any())
            {
                return NotFound("No records found.");
            }

            var memory = new MemoryStream();
            using (var writer = new StreamWriter(memory, Encoding.UTF8, leaveOpen: true))
            using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)))
            {
                csv.WriteRecords(records);
            }

            util.LogAction(user, "DOWNLOAD GENEX FILE", _context);
            //util.WriteToLog($"Error Logging in {}::::{}");
            memory.Position = 0;
            return File(memory, "text/csv", $"MSC_Request_{uploadID}.csv");
        }
        public IActionResult DeleteUpload(int uploadID)
        {
            if (!SetSessionData())
            {
                return RedirectToAction("Index", "Home");
            }

            Delete(uploadID);

            TempData["SuccessMessage"] = "Upload deleted!";
            return RedirectToAction("UploadList", "Upload");
        }
        public IActionResult DeleteRejectedUpload(int uploadID)
        {
            if (!SetSessionData())
            {
                return RedirectToAction("Index", "Home");
            }

            Delete(uploadID);
            TempData["SuccessMessage"] = "Upload deleted!";
            return RedirectToAction("RejectedUploadList", "Upload");
        }
        public IActionResult RequestApproval(int uploadID)
        {
            if (!SetSessionData())
            {
                return RedirectToAction("Index", "Home");
            }

            Approval(uploadID);
            TempData["SuccessMessage"] = "Request logged!";
            return RedirectToAction("UploadList", "Upload");
        }
        public void Approval(int Id)
        {
            var user = HttpContext.Session.GetString("user");

            var upload = _context.MSC_Request_Upload.FirstOrDefault(x => x.ID == Id);

            if (upload != null)
            {
                upload.Status = "1";
                _context.SaveChanges();

                //TODO
                //SEND APPROVAL NOTIFICATION

                //util.SendNotificationEmail("im04220@jaizbankplc.com", "POS merchant charge settlement is awaiting your approval.");
                util.SendNotificationEmail($"{upload.Supervisor}@jaizbankplc.com", "POS merchant charge settlement is awaiting your approval.");
            }

            util.LogAction(user, "REQUEST FOR APPROVAL", _context);
        }
    }
}
