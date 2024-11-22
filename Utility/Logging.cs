using System;
using System.IO;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using System.Configuration;
//using SAPAddOnFramework;

namespace CommonLibrary.ExceptionHandling
{
    public class Logging : System.Exception
    {

        public enum Purpose
        {
            et_Plain,
            et_Error
        }

        public enum LogStatus
        {
            START,
            END,
            RET_VAL,
            ERROR
        }

        public Logging()
            : base()
        { }
        public Logging(string message)
            : base(message)
        { }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="message">Μήνυμα Λάθους</param>
        /// <param name="inner">To Exception</param>
        public Logging(string message, System.Exception inner)
            : base(message, inner)
        {
            //System.Diagnostics.StackTrace trace = new System.Diagnostics.StackTrace(inner, true);

            //string sMSG = this.GetPCName() + "|" + sUser + "|" + message + "|" + inner.Message.ToString();
            //string sMSG = message + "|" + inner.Message.ToString();
            //string sMSG = "ErrorLine=" + trace.GetFrame(0).GetFileLineNumber() + "|" + message + "|" + inner.Message.ToString();
            string sMSG = "ErrorLine=" + this.GetLineNumber(inner).ToString() + "|" + message + "|" + inner.Message.ToString();

            SendToLog(sMSG, LogStatus.ERROR);
            //SAPAddOnFramework.Globals.SBO_Application.StatusBar.SetText(message, SAPbouiCOM.BoMessageTime.bmt_Short, SAPbouiCOM.BoStatusBarMessageType.smt_Error);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message">Μήνυμα Λάθους</param>
        /// <param name="_sSQL">SQL Query</param>
        /// <param name="inner">To Exception</param>
        public Logging(string _sMessage, string _sSQL, System.Exception oInnerException)
            : base(_sMessage, oInnerException)
        {
            //System.Diagnostics.StackTrace trace = new System.Diagnostics.StackTrace(oInnerException, true);

            //string sMSG = this.GetPCName() + "|" + sUser + "|" + _sMessage + "|" + oInnerException.Message.ToString() + "|" + _sSQL;
            //string sMSG =  _sMessage + "|" + oInnerException.Message.ToString() + "|" + _sSQL;
            //string sMSG = "ErrorLine=" + trace.GetFrame(0).GetFileLineNumber() + "|" + _sMessage + "|" + oInnerException.Message.ToString() + "|" + _sSQL;
            string sMSG = "ErrorLine=" + this.GetLineNumber(oInnerException) + "|" + _sMessage + "|" + oInnerException.Message.ToString() + "|" + _sSQL;

            SendToLog(sMSG, LogStatus.ERROR);
            //SAPAddOnFramework.Globals.SBO_Application.StatusBar.SetText(_sMessage, SAPbouiCOM.BoMessageTime.bmt_Short, SAPbouiCOM.BoStatusBarMessageType.smt_Error);
        }

        // A constructor is needed for serialization when an 
        // exception propagates from a remoting server to the client.  
        protected Logging(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context)
        { }


        /// <summary>
        ///Καταγραφή σε Log Αρχείο
        /// </summary>
        /// <param name="_sText">Το κείμενο που θα αποθηκευτεί</param>
        /// <param name="_oStatus">Τύπος πληροφορίας</param>
        public static void WriteToLog(string _sText, LogStatus _oStatus)
        {
            string sFileLocation = "C:\\Program Files\\sap\\ImpactElectronicInvoicing";

            Ini.IniFile ini = new Ini.IniFile(string.Format("{0}\\{1}", sFileLocation, "ConfParams.ini"));
            int iLogger = int.Parse(ini.IniReadValue("Default", "LOGGER").ToString());

            if (iLogger == 1)
            {
                SendToLog(_sText, _oStatus);
            }
            else
            {
                if (_oStatus == LogStatus.ERROR)
                {
                    SendToLog(_sText, _oStatus);
                }
            }
        }

        public static void ManageLogFile()
        {
            string sErrorLogFile = "ErrorLog.txt";
            string sFileLocation = "C:\\Program Files\\sap\\ImpactElectronicInvoicing\\Logs";

            FileInfo oFInfo = new FileInfo(string.Format("{0}\\{1}", sFileLocation, sErrorLogFile));

            string sTmpNewFileName = "";
            DateTime dt = DateTime.Now;
            //ARCHIVED_LOGS_ODY_2014-09-14
            //sTmpNewFileName = oIniV.ArchiveLogs + oIniV.ArchiveLogsFileName + "_" + dt.ToString("yyyy-MM-dd") + ".lbp";
            //sTmpNewFileName = sFileLocation + "\\" + "ARCHIVED_LOGS " + "_" + dt.ToString("yyyy-MM-dd") + ".lbp";
            sTmpNewFileName = sFileLocation + "\\" + "ARCHIVED_LOGS " + "_" + dt.ToString("yyyyMMddHHmmss") + ".lbp";

            if (oFInfo.Length > 1000000)
            {
                System.IO.File.Move(string.Format("{0}\\{1}", sFileLocation, sErrorLogFile), @sTmpNewFileName);
            }
        }

        private string GetPCName()
        {
            string sRetVal = "";

            try
            {
                //Το όνομα του PC απο το CITRIX
                sRetVal = Environment.GetEnvironmentVariable("CLIENTNAME");
            }
            catch
            {
                sRetVal = System.Environment.MachineName;
            }

            return sRetVal;
        }

        private static string GetComputerName()
        {
            string sRetVal = "";

            try
            {
                //Το όνομα του PC απο το CITRIX
                sRetVal = Environment.GetEnvironmentVariable("CLIENTNAME");
            }
            catch
            {
                sRetVal = System.Environment.MachineName;
            }

            return sRetVal;
        }

        private long GetLineNumber(Exception ex)
        {
            var lineNumber = 0;
            const string lineSearch = ":line ";
            var index = ex.StackTrace.LastIndexOf(lineSearch);
            if (index != -1)
            {
                var lineNumberText = ex.StackTrace.Substring(index + lineSearch.Length);
                if (int.TryParse(lineNumberText, out lineNumber))
                {
                }
            }
            return lineNumber;
        }

        private static void SendToLog(string _sText, LogStatus _oStatus)
        {
            string sErrorLogFile = "ErrorLog.txt";
            string sFileLocation = "C:\\Program Files\\sap\\ImpactElectronicInvoicing\\Logs";
            //string sUser = Globals.Company.UserName;

            StreamWriter oLogFile;

            if (!File.Exists(string.Format("{0}\\{1}", sFileLocation, sErrorLogFile)))
            {
                oLogFile = new StreamWriter(string.Format("{0}\\{1}", sFileLocation, sErrorLogFile));
            }
            else
            {
                oLogFile = File.AppendText(string.Format("{0}\\{1}", sFileLocation, sErrorLogFile));
            }

            //oLogFile.WriteLine(DateTime.Now + "|" + GetComputerName() + "|" + sUser + "|" + _sText + "|" + _oStatus);
            oLogFile.WriteLine(DateTime.Now + "|" + GetComputerName() + "|" + _sText + "|" + _oStatus);

            oLogFile.Close();

            ManageLogFile();
        }
    }
}
