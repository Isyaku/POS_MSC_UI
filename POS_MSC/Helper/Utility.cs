using JaizAuthService;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Oracle.ManagedDataAccess.Client;
using POS_MSC.Data;
using POS_MSC.Models;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace POS_MSC.Helper
{
    public class Utility
    {
        public string ConfigHelper(string key)
        {
            var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appSettings.json", optional: true, reloadOnChange: true)
            .Build();

            // Read the configuration value
            string result = configuration[$"{key}"];
            return result;
        }

        public void WriteToLog(string content)
        {
            var errorpath = ConfigHelper("appConfiguration:LogPath");

            if (!Directory.Exists(errorpath))
            {
                Directory.CreateDirectory(errorpath);
            }

            string today = DateTime.Now.ToString("dd-MM-yyyy");

            string path = errorpath + today + ".txt";
            using (StreamWriter sw = File.AppendText(path))
            {
                sw.WriteLine(DateTime.Now + "--------------------" + content);
            }
        }

        public string DecryptTextWithPrivateKey(string encryptedText)
        {
            try
            {
                var xml = "<RSAKeyValue><Modulus>zX0HSeWFFywVOdnoOF3MC+uL8YOvLoA+vLEPBmZ2U7AWuZKer0gzk49mk2v87tBchEzDgfr7z5icgAStf9MVPhR3FckZ/WGRY0ifTH4bkPFtpMmonH755rcMSsszgrguVDyMjeizoEhFvkyUFR30LyXOSqynLYkZBfi2Z8/O01M=</Modulus><Exponent>AQAB</Exponent><P>6i86nisKd3z/A+D+2mWAa1AdXaDVL7qJ78DhHOOEFGbGCKnK16rKbb+R+L8WcsmKVgqhhczEWGbCFymLUynTDQ==</P><Q>4KF2Yjxv/hXFhe6r289gyMFHkWFm9gpSIb8Vah9aruwuB6EOtAL7BFhfdnaTQuo2XEs6v4+OijFV4oADwd0n3w==</Q><DP>WUEa5EGfQZ9ASqgsOezJnxzvtEmiNwivndMzeSE1q9jnzVF5X+1WLbH/3oBl++XYdaajnS1IADFZ9B3/Xfjo2Q==</DP><DQ>qhI1Qm1F0ZcERMIOhk79lSGZIP4g6TmpM3msKfvxOa0BsK8FJc9347NRG6ztE+WmILyojy6Omhx+TQ3lSls5+w==</DQ><InverseQ>B0tAXbxa10gEGokheNErVaBJm/lkZIc9M4B6sFgoSVjXCwtSyIBkRZXH8py4YAQmu6eMmGoAtaaWf2oKgAeOGQ==</InverseQ><D>pC3DBw20uoDkLKan3XFDuDpoQ3zdGKAqgARPZuOyosbMQVSeKJnda4ZlhGABZKVhZesXQeDQFFtwnvAd10VFcDfAFEIuLmBzvU3jAvV+oOcAr5v41n4v3OPMLP+WhUj7hemWTY8NQH1jdo1gBRz0bcsga8Vnjy79UCo5j2gciBE=</D></RSAKeyValue>";

                byte[] encryptedBytes = Convert.FromBase64String(encryptedText);

                using (var rsa = new RSACryptoServiceProvider())
                {
                    rsa.FromXmlString(xml);

                    byte[] decryptedBytes = rsa.Decrypt(encryptedBytes, RSAEncryptionPadding.Pkcs1);

                    return Encoding.UTF8.GetString(decryptedBytes);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred during decryption: " + ex.Message);
                return null;
            }
        }

        public string SendNotificationEmail(string emailaddress, string message)
        {
            string templatePath = $"{Directory.GetCurrentDirectory()}\\wwwroot\\emailTemplate\\NotificationMail.htm";
            string strMessage = File.ReadAllText(templatePath);
            string mailBody = string.Empty;
            mailBody = strMessage;
            mailBody = mailBody.Replace("#Message#", message);
            try
            {
                JaizServiceReference.JaizHelperClient service = new JaizServiceReference.JaizHelperClient();
                JaizServiceReference.EmailObject obj = new JaizServiceReference.EmailObject
                {
                    Attachment = null,
                    EmailAddress = emailaddress,
                    EmailContent = mailBody,
                    FromAddress = "platform@jaizbankplc.com",
                    HasAttachment = 0,
                    SenderId = "SRVMGT",
                    Subject = "POS_MSC Notification"
                };

                return service.SendEmailViaHelper(obj).ToString();
            }
            catch (Exception ex)
            {
                WriteToLog($"{ex}, Unable to send email notification");
                return null;
            }
        }

        public void LogAction(string user, string action, AppDbContext db)
        {
            try
            {
                var time = DateTime.Now;
                var log = new MSC_AuditLogModel
                {
                    Staff = user,
                    Action = action,
                    Time = time
                };

                db.MSC_AuditLog.Add(log);
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                WriteToLog($"{ex}, Unable to log user action");
            }

        }

        public void SaveUserSession(string username, string sessionToken, AppDbContext db)
        {
            try {
                var existing = db.UserSession.FirstOrDefault(u => u.UserID == username);

                if (existing != null)
                {
                    // Update existing session
                    existing.SessionToken = sessionToken;
                    existing.LastUpdated = DateTime.UtcNow;
                    db.UserSession.Update(existing);
                }
                else
                {
                    // Insert new session
                    db.UserSession.Add(new UserSessionModel
                    {
                        UserID = username,
                        SessionToken = sessionToken,
                        LastUpdated = DateTime.UtcNow
                    });
                }

                db.SaveChanges();
            } catch (Exception ex) {
                WriteToLog($"{ex}, Unable to log user action");
            }
            
        }
        public string? GetSupervisor(string staffID, PersonelDbContext _context)
        {
            string result = "";
            try {
                var sql = "SELECT personnel_id, supervisor FROM staff_personnel WHERE personnel_id = {0}";

                var personel = _context.staff_personnel
                    .FromSqlRaw(sql, staffID)
                    .AsEnumerable()
                    .FirstOrDefault();

                result = personel?.Supervisor;
            } catch(Exception ex)
            {
                WriteToLog($"{ex}, Unable to get supervisor");
            }
            return result;
        }

        public string GetSessionID()
        {
            string result = "";

            try
            {

                JaizInternalFTRef.processmessageSoapClient client = new JaizInternalFTRef.processmessageSoapClient(0);
                string res = client.getSessionID();
                result = "000006" + res;
                //return "000006";
            }

            catch (Exception ex)
            {
                WriteToLog($"Error Getting session Id: {ex.Message}");
            }
            return result;
        }
    }
}
