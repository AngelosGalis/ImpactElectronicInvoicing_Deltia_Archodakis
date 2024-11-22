using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using CommonLibrary.ExceptionHandling;

namespace ImpactElectronicInvoicing.BusinessLayer
{
    class BoMail
    {
        #region Public Properties
        public string Body { get; set; }
        public string Subject { get; set; }
        #endregion  

        public BoMail()
        { }

        public void SendMail(string _sReceiver)
        {
            try
            {
                int iResult = 0;
                int cnt = 1;
                CommonLibrary.Ini.IniFile ini = new CommonLibrary.Ini.IniFile("C:\\Program Files\\sap\\ImpactElectronicInvoicing\\ConfParams.ini");

                #region InnovPlanet
                //var fromAddress = new MailAddress("vplagianos@digital4u.gr", "Donoupoglou Service");
                string mailAddress = "";
                while (iResult == 0 && cnt < 6)
                {
                    if (cnt > 1)
                    {
                        mailAddress = "noreply" + cnt.ToString() + "@innovplanet.eu";
                    }
                    else
                    {
                        mailAddress = "noreply@innovplanet.eu";
                    }
                    var fromAddress = new MailAddress(mailAddress, "" + ini.IniReadValue("Default", "MAIL_SERVICE_NAME") + " AADE Service");

                    var toAddress = new MailAddress(_sReceiver, _sReceiver);
                    //string fromPassword = "abc123!@#";
                    string fromPassword = "=?UeB(dZ8~vj";

                    //string subject = "eShopIntegrator Automated Mail Process Report" + "/" + this.Parameters.IntegrationParameters.SiteCustomer.CustomerName;

                    iResult = this.Send(fromAddress, toAddress, fromPassword);
                    cnt++;
                }
                #endregion

            }
            catch (Exception ex)
            {
                var a = new Logging("BoMail.SendMail", ex);
            }
        }

        private int Send(System.Net.Mail.MailAddress fromAddress, System.Net.Mail.MailAddress toAddress, string sFromPassword)
        {
            int iRetVal = 0;
            try
            {
                var smtp = new SmtpClient
                {
                    //Host = "mail.mediatel.gr",
                    //Port = 25,
                    Host = "mail.innovplanet.eu",
                    Port = 587,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new System.Net.NetworkCredential(fromAddress.Address, sFromPassword)
                };
                using (var message = new MailMessage(fromAddress, toAddress)
                {
                    Subject = this.Subject,
                    IsBodyHtml = true,
                    Body = this.Body
                })
                {
                    smtp.Send(message);
                }
                iRetVal++;
            }
            catch (Exception ex)
            {
                var a = new Logging("BoMail.Send", ex);
                return 0;
            }
            return iRetVal;
        }
    }
}
