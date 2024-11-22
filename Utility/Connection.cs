using System;
using SAPbobsCOM;
using CommonLibrary.ExceptionHandling;
using System.Configuration;

namespace ImpactElectronicInvoicing
{
    public class Connection
    {
        #region Public Properties
        public static SAPbobsCOM.Company oCompany { get; set; }
        public static SAPbobsCOM.BoYesNoEnum Connected { get; set; }
        #endregion

        public Connection()
        {
            Logging.WriteToLog("Connection.Connect", Logging.LogStatus.START);
            this.Connect();
            Logging.WriteToLog("Connection.Connect", Logging.LogStatus.END);
        }
        public void SapConnection()
        {
            Logging.WriteToLog("SapConnection.Connect", Logging.LogStatus.START);
            this.Connect();
            Logging.WriteToLog("SapConnection.Connect", Logging.LogStatus.END);
        }

        #region Private Methods
        private int Connect()
        {
            int iRetVal = 0;
            try
            {
                oCompany = new SAPbobsCOM.Company();
                //oCompany.Server = "10.0.1.105:30015";
                oCompany.Server = ConfigurationManager.AppSettings["ServerIP"];
                oCompany.DbServerType = BoDataServerTypes.dst_HANADB;
                oCompany.UseTrusted = false;
                oCompany.DbUserName = ConfigurationManager.AppSettings["DbUserName"];
                oCompany.DbPassword = ConfigurationManager.AppSettings["DbPassword"];
                oCompany.CompanyDB = ConfigurationManager.AppSettings["Database"];
                oCompany.UserName = ConfigurationManager.AppSettings["B1UserName"];
                oCompany.Password = ConfigurationManager.AppSettings["B1Password"];
                oCompany.LicenseServer = ConfigurationManager.AppSettings["LicenseServer"];

                if (oCompany.Connect() == 0)
                {
                    Console.WriteLine("Connected successfully to DB: " + oCompany.CompanyDB);
                    Connected = BoYesNoEnum.tYES;
                    iRetVal++;
                }
                else
                {
                    Connected = BoYesNoEnum.tNO;
                    Console.WriteLine("Connection Failed because: " + oCompany.GetLastErrorDescription());
                    int nErr;
                    string sErrMsg;
                    oCompany.GetLastError(out nErr, out sErrMsg);

                    Logging.WriteToLog("Invalid Connection", Logging.LogStatus.RET_VAL);
                    Logging.WriteToLog("DI Error: " + nErr.ToString() + " / " + sErrMsg, Logging.LogStatus.RET_VAL);
                }
            }
            catch (Exception ex)
            {
                var a = new Logging("SapConnection.Connect", ex);
            }
            return iRetVal;


        }
        #endregion
    }
}
