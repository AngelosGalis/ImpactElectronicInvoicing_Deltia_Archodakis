using System;
using System.Collections.Generic;
using System.Text;
using SAPbobsCOM;
//using SAPAddOnFramework;
using CommonLibrary.ExceptionHandling;

namespace ImpactElectronicInvoicing.BusinessLayer
{
    class IConnection
    {
        #region Private Variables
        private SAPbobsCOM.Company oCompany;
        private string sCompanyName = "";
        private bool bSuccess = false;
        #endregion

        #region Private Properties
        private string CompanyName
        {
            get
            {
                return this.sCompanyName;
            }
            set
            {
                this.sCompanyName = value;
            }
        }
        #endregion

        #region Public Properties
        public SAPbobsCOM.BoYesNoEnum Connected { get; set; }
        public SAPbobsCOM.Company CompanyConnection
        {
            get
            {
                return this.oCompany;
            }
            set
            {
                this.oCompany = value;
            }
        }
        #endregion

        /// <summary>
        /// Manual Διασύνδεση με Άλλη Βάση SAPB1
        /// </summary>
        /// <param name="_sCompanyName">Όνομα Εταιρίας</param>
        /// <param name="_sServer">Server IP</param>
        /// <param name="_sDBPassword">Database Password</param>
        /// <param name="_sUserName">SAPB1 UserName</param>
        /// <param name="_sPassword">SAPB1 Password</param>
        /// <param name="_sDBVersion">Database Version</param>
        /// <param name="_sDBUserName">DB UserName</param>
        /// <param name="_sLicenseServer">License Server</param>
        public IConnection(string _sCompanyName, string _sServer, string _sDBPassword, string _sUserName, string _sPassword, string _sDBVersion, string _sDBUserName, string _sLicenseServer)
        {
            oCompany = new SAPbobsCOM.Company();
            //oCompany.Server = "10.0.1.105:30015";
            oCompany.Server = _sServer;
            //oCompany.DbServerType = BoDataServerTypes.dst_HANADB; 
            oCompany.DbServerType = (BoDataServerTypes)Enum.Parse(typeof(BoDataServerTypes), _sDBVersion);
            oCompany.UseTrusted = false;
            oCompany.DbUserName = _sDBUserName;
            oCompany.DbPassword = _sDBPassword;
            oCompany.CompanyDB = _sCompanyName;
            oCompany.UserName = _sUserName;
            oCompany.Password = _sPassword;
            oCompany.LicenseServer = _sLicenseServer;
            oCompany.SLDServer = _sLicenseServer;

            if (oCompany.Connect() == 0)
            {
                Console.WriteLine("Connected successfully to DB: " + oCompany.CompanyDB);
                Connected = BoYesNoEnum.tYES;
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

        private void SendMail(string _sErrorDscr)
        {
            try
            {
                BoMail oMail = new BoMail();
                oMail.Body = "Περιγραφή Μηνύματος=" + _sErrorDscr;

                oMail.Subject = "DI Connection Error";

                oMail.SendMail("vplagianos@gmail.com");
            }
            catch (Exception ex)
            {
                var a = new Logging("IConnection.SendMail", ex);
            }
        }
    }
}



//SAPbobsCOM.Company oCompany = new SAPbobsCOM.Company();

//oCompany.Server = "127.0.0.1";
//oCompany.language = SAPbobsCOM.BoSuppLangs.ln_English;

//// Use Windows authentication for database server.
//// True for NT server authentication,
//// False for database server authentication.
//oCompany.UseTrusted = true;
////oCompany.CompanyDB = "DLP";




////oCompany.CompanyDB = "IMPORTEXPORT_TOOL";
//oCompany.CompanyDB="EYDAP_19_07_2014";

//oCompany.UserName = "manager";
//oCompany.Password = "mngr";
//oCompany.DbServerType = SAPbobsCOM.BoDataServerTypes.dst_MSSQL2008;
//int i = oCompany.Connect();

//if (i != 0)
//{
//    int temp_int = 0;
//    string temp_string = "";
//    oCompany.GetLastError(out temp_int, out temp_string);

//}
//else
//{

//    SAPbobsCOM.JournalEntries JournalDocument = (SAPbobsCOM.JournalEntries)oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oJournalEntries);
//    JournalDocument.ReferenceDate = DateTime.Parse("12/12/2014");
//    JournalDocument.DueDate = JournalDocument.ReferenceDate;
//    JournalDocument.TaxDate = JournalDocument.ReferenceDate;
//    JournalDocument.Reference = "riona2";

//    //JournalDocument.Lines.ShortName = "20.00.00.00";
//    JournalDocument.Lines.ShortName = "50.05.00.000078";


//    JournalDocument.Lines.Debit = 5;
//    JournalDocument.Lines.Credit = 0;
//    JournalDocument.Lines.FCDebit = 4;
//    JournalDocument.Lines.Add();

//    //JournalDocument.Lines.ShortName = "20.01.00.00";
//    JournalDocument.Lines.ShortName = "50.05.00.000101";

//    JournalDocument.Lines.Credit = 5;
//    JournalDocument.Lines.FCCredit = 4;
//    JournalDocument.Lines.Debit = 0;

//    JournalDocument.Lines.Add();


//    if (JournalDocument.Add() != 0)
//    {
//        int nErr;
//        string sErrMsg;
//        oCompany.GetLastError(out nErr, out sErrMsg);
//    }
//    else
//    {
//        string sTranResu = oCompany.GetNewObjectKey();
//    }

//}