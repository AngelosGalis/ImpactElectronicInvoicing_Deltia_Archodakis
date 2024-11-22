using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using ImpactElectronicInvoicing.BusinessLayer;
//using System.Web.Services.Protocols;
using System.Data.SqlClient;
using System.Threading;
using CommonLibrary.ExceptionHandling;
using System.ServiceProcess;
using System.Collections.ObjectModel;

namespace ImpactElectronicInvoicing.Modules
{
    class MyDataTasks
    {
        #region Private Variables
        private Thread _thread;
        #endregion

        #region Private Properties
        private SAPbobsCOM.Company CompanyConnection { get; set; }
        private List<string> RunningMinutes { get; set; }
        private int StopService { get; set; }

        #endregion

        public MyDataTasks()
        { }

        public void Start()
        {
            //int milliseconds = 30000;
            int milliseconds = 10000;
            Thread.Sleep(milliseconds);

            _thread = new Thread(new ThreadStart(Execute));
            _thread.IsBackground = true;
            _thread.Start();
        }

        public void Stop()
        {
            this.SendMail("Service Stopped", "Service Is Down");
            if (_thread != null)
            {
                //this.SendMail("Service Stopped", "Service Is Down");
                if (CompanyConnection != null)
                {
                    CompanyConnection.Disconnect();
                }

                _thread.Abort();
                _thread.Join();
            }
            //string serviceName = "ImpactElectronicInvoicingWindowsService";
            //ServiceController serviceController = new ServiceController(serviceName);
            //TimeSpan timeout = TimeSpan.FromMilliseconds(1000);
            //serviceController.Stop();
            //serviceController.WaitForStatus(ServiceControllerStatus.Stopped, timeout);
        }

        public void StopMyService()
        {

            CommonLibrary.Ini.IniFile ini = new CommonLibrary.Ini.IniFile("C:\\Program Files\\sap\\ImpactElectronicInvoicingDA\\ConfParams.ini");
            string serviceName = ini.IniReadValue("Default", "MYDATA_WINDOWS_SERVICE_NAME");
            //string serviceName = "ImpactElectronicInvoicingWindowsService";
            if (!string.IsNullOrEmpty(serviceName))
            {
                ServiceController serviceController = new ServiceController(serviceName);
                TimeSpan timeout = TimeSpan.FromMilliseconds(50000);
                serviceController.Stop();
                serviceController.WaitForStatus(ServiceControllerStatus.Stopped, timeout);
                this.SendMail("Service Stopped", "Service Is Down");
            }
        }

        public void Execute()
        {
            try
            {

                //Φόρτωση των Παραμέτρων σε Μεταβλητές
                //AddOnSettings.LoadSettings(this.CompanyConnection);
                int sumOfRows = 1;
                int connectionCnt = 0;
                //int k = 0;
                //DateTime startTime = DateTime.Now.AddSeconds(7500000);
                Logging.WriteToLog("MyDataTasks.ReadIniFile", Logging.LogStatus.START);
                this.ReadIniFile();
                Logging.WriteToLog("MyDataTasks.ReadIniFile", Logging.LogStatus.END);
                while (true)
                {
                    int iResult = 0;
                   

                    int cnt = 0;
                    if (CompanyConnection == null || (CompanyConnection.Connected == false))
                    {
                        connectionCnt++;
                        Logging.WriteToLog("EpsilonSendMethods.ConnectDI", Logging.LogStatus.START);
                        this.ConnectDI();
                        Logging.WriteToLog("EpsilonSendMethods.ConnectDI", Logging.LogStatus.END);
                    }



                    if (CompanyConnection != null && CompanyConnection.Connected == true)
                    {
                        sumOfRows = 0;
                        this.ProcessDocuments();
                        connectionCnt = 0;
                    }

                    if (connectionCnt > 5)
                    {
                        connectionCnt = 0;
                        Logging.WriteToLog("Going to sleep", Logging.LogStatus.START);
                        Thread.Sleep(3600000);
                        Logging.WriteToLog("Waking Up", Logging.LogStatus.END);
                    }
                }

            }
            catch (Exception ex)
            {
                var a = new Logging("MyDataTasks.Execute", ex);
            }
        }

        /// <summary>
        /// Σύνδεση με την Βάση του SAPB1
        /// </summary>
        /// <returns>1 For Success, 0 For Failure</returns>
        private int ConnectDI()
        {
            int iRetVal = 0;
            try
            {
                CommonLibrary.Ini.IniFile ini = new CommonLibrary.Ini.IniFile("C:\\Program Files\\sap\\ImpactElectronicInvoicingDA\\ConfParams.ini");

                string sCompanyName = ini.IniReadValue("Default", "COMPANY_NAME");
                string sServerName = ini.IniReadValue("Default", "SAP_SERVER");
                string sDBUserName = ini.IniReadValue("Default", "DB_USERNAME");
                string sDBPassword = ini.IniReadValue("Default", "DB_PASSWORD");
                string sUserName = ini.IniReadValue("Default", "B1_USERNAME");
                string sPassword = ini.IniReadValue("Default", "B1_PASSWORD");
                string sDBVersion = ini.IniReadValue("Default", "DB_VERSION");
                string sLicenseServer = ini.IniReadValue("Default", "LICENSE_SERVER");

                IConnection oConnection = new IConnection(sCompanyName, sServerName, sDBPassword, sUserName, sPassword, sDBVersion, sDBUserName, sLicenseServer);

                if (oConnection.Connected == SAPbobsCOM.BoYesNoEnum.tYES)
                {
                    CompanyConnection = oConnection.CompanyConnection;
                    Logging.WriteToLog("Connection Established", Logging.LogStatus.RET_VAL);
                    iRetVal++;
                }
                else
                {
                    Logging.WriteToLog("Invalid Connection", Logging.LogStatus.RET_VAL);
                    iRetVal = 0;
                }
            }
            catch (Exception ex)
            {
                iRetVal = 0;
                var a = new Logging("MyDataTasks.ConnectDI", ex);
            }
            finally
            {
            }
            return iRetVal;
        }

        private void ReadIniFile()
        {
            try
            {
                CommonLibrary.Ini.IniFile ini = new CommonLibrary.Ini.IniFile("C:\\Program Files\\sap\\ImpactElectronicInvoicingDA\\ConfParams.ini");
                this.StopService = int.Parse(ini.IniReadValue("Default", "STOP_SERVICE"));
            }
            catch (Exception ex)
            {
                var a = new Logging("MyDataTasks.ReadIniFile", ex);
            }
        }

        private void SendMail(string _sSubject, string _sBody)
        {
            try
            {
                BoMail oMail = new BoMail();
                oMail.Body = _sBody;
                oMail.Subject = _sSubject;
                //oMail.SendMail("vplagianos@gmail.com");
                CommonLibrary.Ini.IniFile ini = new CommonLibrary.Ini.IniFile("C:\\Program Files\\sap\\ImpactElectronicInvoicingDA\\ConfParams.ini");
                string address = ini.IniReadValue("Default", "EMAIL_ADDRESS");
                oMail.SendMail(address);

            }
            catch (Exception ex)
            {
                var a = new Logging("MyDataTasks.SendMail", ex);
            }
        }

        private void ProcessDocuments()
        {
            try
            {
                BusinessLayer.myDataMethods oLoadnCreate = new myDataMethods();
                oLoadnCreate.CompanyConnection = this.CompanyConnection;
                int iProcessResult = oLoadnCreate.LoadnCreate(Enumerators.ot_Object.otSalesDocuments);
                oLoadnCreate.Send(Enumerators.ot_Object.otSalesDocuments);
            }
            catch (Exception ex)
            {
                var a = new Logging("MyDataTasks.ProcessDocuments", ex);
            }
        }

        //private void ProcessCancelDocuments()
        //{
        //    try
        //    {
        //        BusinessLayer.myDataMethods oLoadnCreateCancel = new myDataMethods();
        //        oLoadnCreateCancel.CompanyConnection = this.CompanyConnection;
        //        oLoadnCreateCancel.returnsRows = this.returnRows;
        //        int iProcessResultCancel = oLoadnCreateCancel.LoadnCreateCancel(Enumerators.ot_Object.otSalesDocuments);
        //        this.returnRows = oLoadnCreateCancel.returnsRows;
        //        oLoadnCreateCancel.CancelInvoice();
        //    }
        //    catch (Exception ex)
        //    {
        //        var a = new Logging("MyDataTasks.ProcessCancelDocuments", ex);
        //    }
        //}

       
    }
}