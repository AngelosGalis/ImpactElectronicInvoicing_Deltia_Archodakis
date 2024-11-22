using CommonLibrary.ExceptionHandling;
using ImpactElectronicInvoicing.Enumerators;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using System.Xml.Linq;

namespace ImpactElectronicInvoicing.BusinessLayer
{
    public class myDataMethods
    {
        #region Public Properties
        public List<BoDocument> ListDocuments { get; set; }
        public SAPbobsCOM.Company CompanyConnection { get; set; }
        public List<BoDocument> ListDocumentsCancel { get; set; }
        public SAPbobsCOM.Company CompanyConnectionCancel { get; set; }

        #endregion

        #region Private Properties
        private RestClient Client { get; set; }
        private RestRequest Request { get; set; }
        #endregion

        public myDataMethods()
        {
            this.ListDocuments = new List<BoDocument>();
        }

        #region Public Methods
        public int LoadnCreate(Enumerators.ot_Object _enType)
        {
            int iRetVal = 0;
            try
            {
                LoadnCreateClass oLoadnCreate = new LoadnCreateClass();
                oLoadnCreate.CompanyConnection = this.CompanyConnection;
                iRetVal = oLoadnCreate.Exec(_enType);

                //if (iRetVal == 1) //έγινε σχόλιο γτ τρέχω ΜΟΝΟ αυτά που είναι πετυχημένα!
                //{
                this.ListDocuments = new List<BoDocument>();
                this.ListDocuments = oLoadnCreate.ListDocuments;
                //}
            }
            catch (Exception ex)
            {
                var a = new Logging("myDataMethods.LoadnCreate", ex);
            }
            return iRetVal;
        }


        public int Send(ot_Object _enType)
        {
            int iRetVal = 0;
            try
            {
                CommonLibrary.Ini.IniFile ini = new CommonLibrary.Ini.IniFile("C:\\Program Files\\sap\\ImpactElectronicInvoicingDA\\ConfParams.ini");
                int updateMark = int.Parse(ini.IniReadValue("Default", "UPDATE_MARK").ToString());
                string[] successArray = { "SUBMITTED", "CONFLICT", "CREATED" };
                foreach (BoDocument oDocument in ListDocuments)
                {
                    if (oDocument.DocumentStatus == DocumentPrepared.p_Success)
                    {

                        int iRes = this.Send2AADE(oDocument);
                        if ((!string.IsNullOrEmpty(oDocument.MARK) || !string.IsNullOrEmpty(oDocument.offlineQR)) /*&& successArray.Contains(ListDocuments[i].StatusCode.ToUpper())*/ && iRes > 0 && updateMark == 1)
                        {
                            int iTempResult = this.UpdateSAPDocuments(oDocument);
                            if (oDocument.Result == Enumerators.SAPResult.sr_Success && iTempResult == 1)
                            {
                                this.UpdateDocumentSETSAPUpdate(oDocument);

                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var a = new Logging("myDataMethods.Send", ex);
            }
            return iRetVal;
        }



        /// <summary>
        /// Ενημέρωση Παραστατικού SAP Business One
        /// </summary>
        /// <param name="_oDocument"> To Παραστατικό που θα Ενημερωθεί</param>
        /// <returns>1 For Success, 0 For Failure</returns>
        private int UpdateSAPDocuments(BoDocument _oDocument)
        {
            int iRetVal = 0;
            string sSQL = "";
            int iResult = 0;
            try
            {
                if (CompanyConnection.DbServerType == SAPbobsCOM.BoDataServerTypes.dst_HANADB)
                {
                    sSQL = "call \"SAP_UPDATE_MARK_IMPACT\"(" +
                   "'" + _oDocument.ObjType + "'," +
                   "'" + _oDocument.DocEntry + "'," +
                   "'" + _oDocument.MARK + "'," +
                   "'" + _oDocument.UID + "'," +
                   "'" + _oDocument.Auth + "'," +
                   "'" + _oDocument.QR + "'," +
                   "'" + _oDocument.offlineQR +
                   "')";
                }
                else
                {
                    sSQL = "exec \"SAP_UPDATE_MARK_IMPACT\"(" +
                   "'" + _oDocument.ObjType + "'," +
                   "'" + _oDocument.DocEntry + "'," +
                   "'" + _oDocument.MARK + "'," +
                   "'" + _oDocument.UID + "'," +
                   "'" + _oDocument.Auth + "'," +
                   "'" + _oDocument.QR + "'," +
                   "'" + _oDocument.offlineQR +
                   "')";

                }

                SAPbobsCOM.Recordset oRS = null;
                oRS = CommonLibrary.Functions.Database.GetRecordSet(sSQL, CompanyConnection);
                if (oRS != null)
                {
                    iResult = UpdateQR(_oDocument);
                    if (iResult == 1)
                    {
                        _oDocument.Result = Enumerators.SAPResult.sr_Success;
                        iRetVal += iResult;
                    }
                }

                //Console.WriteLine("" + sDocumentTypeDsc + " " + _oDocument.DocNum + " Successfully Updated!");
            }
            catch (Exception ex)
            {
                //Console.WriteLine("" + sDocumentTypeDsc + " " + _oDocument.DocNum + " Cannot be Updated!");
                Logging.WriteToLog("sSQL=" + sSQL, Logging.LogStatus.RET_VAL);
                _oDocument.Result = Enumerators.SAPResult.sr_Failure;
                var a = new Logging("myDataMethods.UpdateSAPDocuments", ex);
            }
            return iRetVal;
        }


        private int UpdateQR(BoDocument _oDocument)
        {
            int iRetVal = 0;
            string sSQL = "";
            try
            {
                SAPbobsCOM.Documents oDIDocument = null;

                if (_oDocument.ObjType == "13")
                {
                    oDIDocument = (SAPbobsCOM.Documents)CompanyConnection.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oInvoices);
                }
                else if (_oDocument.ObjType == "14")
                {
                    oDIDocument = (SAPbobsCOM.Documents)CompanyConnection.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oCreditNotes);
                }
                string sDocEntry = _oDocument.DocEntry;//_oDocument.GetDocEntry();
                bool bLoad = oDIDocument.GetByKey(int.Parse(sDocEntry));

                if (bLoad == true)
                {
                    if (!string.IsNullOrEmpty(_oDocument.QR))
                    {
                        oDIDocument.CreateQRCodeFrom = _oDocument.QR;
                    }
                    else
                    {
                        oDIDocument.CreateQRCodeFrom = _oDocument.offlineQR;
                    }

                    int iDIResult = oDIDocument.Update();

                    if (iDIResult == 0)
                    {
                        iRetVal++;
                    }
                    else
                    {
                        int nErr;
                        string sErrMsg;
                        Connection.oCompany.GetLastError(out nErr, out sErrMsg);

                        Console.WriteLine(nErr.ToString() + " / " + sErrMsg);
                        Logging.WriteToLog("DI ERROR on Document with ObjType=" + _oDocument.ObjType + " and DocEntry=" + _oDocument.DocEntry + " | " + nErr.ToString() + " / " + sErrMsg, Logging.LogStatus.RET_VAL);
                    }
                }
                else
                {
                    Console.WriteLine("Δεν ήταν Δυνατή η Φόρτωση του Παραστατικού");
                    Logging.WriteToLog("Δεν ήταν Δυνατή η Φόρτωση του Παραστατικού ObjType=" + _oDocument.ObjType + " and DocEntry=" + _oDocument.DocEntry, Logging.LogStatus.RET_VAL);
                }
            }
            catch (Exception ex)
            {
                Logging.WriteToLog("sSQL=" + sSQL, Logging.LogStatus.RET_VAL);
                var a = new Logging("ElectronicInvoicingMethods.UpdateClass.UpdateSAP", ex);
            }
            return iRetVal;
        }

        public int UpdateDocumentSETSAPUpdate(BusinessLayer.BoDocument _oDocument)
        {
            int iRetVal = 0;
            string sSQL = "";
            try
            {
                if (CompanyConnection.DbServerType == SAPbobsCOM.BoDataServerTypes.dst_HANADB)
                {
                    sSQL = "CALL DOCUMENTS_UPDATE_SET_SAP_UPDATED_IMPACT('" + _oDocument.ObjType + "','" + _oDocument.DocEntry + "')";
                }
                else
                {
                    sSQL = "exec DOCUMENTS_UPDATE_SET_SAP_UPDATED_IMPACT '" + _oDocument.ObjType + "','" + _oDocument.DocEntry + "'";
                }

                SAPbobsCOM.Recordset oRS = CommonLibrary.Functions.Database.GetRecordSet(sSQL, CompanyConnection);
                if (oRS == null)
                {
                    Logging.WriteToLog("Error while updating eliv_documents " + sSQL, Logging.LogStatus.ERROR);
                    Logging.WriteToLog("sSQL=" + sSQL, Logging.LogStatus.ERROR);
                }
                else
                {
                    iRetVal++;
                }
            }
            catch (Exception ex)
            {
                Logging.WriteToLog("sSQL=" + sSQL, Logging.LogStatus.RET_VAL);
                var a = new Logging("BoDAL.UpdateDocumentSETSAPUpdate", ex);
            }
            return iRetVal;
        }
        //public int CancelInvoice()
        //{
        //    int iRetVal = 0;
        //    try
        //    {
        //        int iResult = 0;
        //        int iSuccess = this.ListDocumentsCancel.Count;

        //        for (int i = 0; i < this.ListDocumentsCancel.Count; i++)
        //        {
        //            iResult += this.Cancel(this.ListDocumentsCancel[i]);
        //        }

        //        if (iRetVal == iSuccess)
        //        {
        //            iRetVal++;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        var a = new Logging("myDataMethods.CancelInvoice", ex);
        //    }
        //    return iRetVal;
        //}
        #endregion

        #region Private Methods
        private int Send2AADE(BoDocument _oDocument)
        {
            int iRetVal = 0;
            try
            {
                //////////////// read parameters ///////////////////
                string sEndPoint = "";
                string sUser = "";
                string sSubscription = "";
                string sFileLocation = "C:\\Program Files\\sap\\ImpactElectronicInvoicingDA\\ConfParams.ini";
                CommonLibrary.Ini.IniFile ini = new CommonLibrary.Ini.IniFile(sFileLocation);
                string xmlPath = ini.IniReadValue("Default", "XML_PATH");
                string sProxy = ini.IniReadValue("Default", "PROXY_SERVER");
                sEndPoint = ini.IniReadValue("Default", "ENDPOINT_SEND_INVOICES");
                sSubscription = ini.IniReadValue("Default", "AADE_SUBSCRIPTION_KEY");
                /////////////// create and save JSON /////////////////////
                JsonSerializerOptions json = new JsonSerializerOptions();
                json.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
                string jsonString = System.Text.Json.JsonSerializer.Serialize(_oDocument.ImpactDocument, json);
                //jsonString=jsonString.Replace("400007022", "4000070221985");
                MemoryStream ms = new MemoryStream();
                ms.Position = 0;
                StreamReader SR = new StreamReader(ms);
                string sBody = SR.ReadToEnd();
                string sPath = xmlPath + "\\2AADE\\" + _oDocument.ObjType + "_" + _oDocument.DocEntry + "_" + _oDocument.DocNum + ".json";
                using (StreamWriter sw = File.CreateText(sPath))
                {
                    sw.WriteLine(jsonString);
                }
                this.Client = new RestClient(sEndPoint);
                this.Client.Timeout = -1;
                this.Request = new RestRequest(Method.POST);
                this.Request.AddHeader("APIkey", sSubscription);
                this.Request.AddParameter("application/json", jsonString, ParameterType.RequestBody);
                if (!string.IsNullOrEmpty(sProxy) && !sProxy.Equals(""))
                {
                    WebProxy proxy = new WebProxy(sProxy, true);
                    proxy.UseDefaultCredentials = true;
                    WebRequest.DefaultWebProxy = proxy;
                }
                /////////////////////// read response //////////////////////
                IRestResponse oResponse = this.Client.Execute(this.Request);
                ImpactResponse oReply = new ImpactResponse();
                string sJSON = "";

                this.SaveResponseXML(_oDocument, xmlPath + "\\" + _oDocument.ObjType + "_" + _oDocument.DocEntry + "_" + _oDocument.DocNum + ".json", oResponse.Content, out sJSON);
                Logging.WriteToLog("myDataMethods.AddResponse", Logging.LogStatus.START);
                this.AddResponse(_oDocument, sJSON);
                Logging.WriteToLog("myDataMethods.AddResponse", Logging.LogStatus.END);

                oReply = JsonConvert.DeserializeObject<ImpactResponse>(oResponse.Content);
                switch (oResponse.StatusCode)
                {
                    case HttpStatusCode.Created:
                        iRetVal++;
                        break;
                    case HttpStatusCode.Conflict:
                    case HttpStatusCode.BadRequest:
                    case HttpStatusCode.Unauthorized:
                    case HttpStatusCode.MethodNotAllowed:
                        Logging.WriteToLog("myDataMethods.UpdateDocument", Logging.LogStatus.START);
                        //this.UpdateDocument(_oDocument, oReply);
                        Logging.WriteToLog("myDataMethods.UpdateDocument", Logging.LogStatus.END);
                        break;
                    case HttpStatusCode.RequestTimeout:
                    case HttpStatusCode.InternalServerError:
                        Logging.WriteToLog("myDataMethods.UpdateDocument", Logging.LogStatus.START);
                        this.GetOfflineQR(_oDocument);
                        Logging.WriteToLog("myDataMethods.UpdateDocument", Logging.LogStatus.END);
                        iRetVal++;
                        break;
                }
                Logging.WriteToLog("myDataMethods.UpdateDocument", Logging.LogStatus.START);
                this.UpdateDocument(_oDocument, oReply);
                Logging.WriteToLog("myDataMethods.UpdateDocument", Logging.LogStatus.END);
                _oDocument.StatusCode = oResponse.StatusCode.ToString();
                if (oResponse.StatusCode != HttpStatusCode.Created)
                {
                    Logging.WriteToLog("Processing Document:" + _oDocument.ObjType + " / " + _oDocument.DocNum + "", Logging.LogStatus.RET_VAL);
                    Logging.WriteToLog("Error Contacting EndPoint:" + oResponse.StatusCode + "/" + oResponse.StatusDescription, Logging.LogStatus.ERROR);
                }
            }
            catch (Exception ex)
            {
                var a = new Logging("myDataMethods.Send2AADE", ex);
            }
            return iRetVal;
        }


        private int GetOfflineQR(BoDocument _oDocument)
        {
            int iRetVal = 0;
            try
            {
                //////////////// read parameters ///////////////////
                string sEndPoint = "";
                string sUser = "";
                string sSubscription = "";
                string sFileLocation = "C:\\Program Files\\sap\\ImpactElectronicInvoicingDA\\ConfParams.ini";
                CommonLibrary.Ini.IniFile ini = new CommonLibrary.Ini.IniFile(sFileLocation);
                string xmlPath = ini.IniReadValue("Default", "XML_PATH");
                string sProxy = ini.IniReadValue("Default", "PROXY_SERVER");
                sSubscription = ini.IniReadValue("Default", "AADE_SUBSCRIPTION_KEY");
                /*if (!string.IsNullOrEmpty(sProxy))
                {
                    WebProxy proxy = new WebProxy(sProxy, true);
                    proxy.UseDefaultCredentials = true;
                    WebRequest.DefaultWebProxy = proxy;
                }
                /// get validationSignature
                sEndPoint = ini.IniReadValue("Default", "ENDPOINT_GET_SIGNATURE");
                this.Client = new RestClient(sEndPoint);
                this.Client.Timeout = -1;
                this.Request = new RestRequest(Method.GET);
                this.Request.AddHeader("APIkey", sSubscription);
                this.Request.AddParameter("issuerTin", "EL" + _oDocument.ImpactDocument.Issuer.Vat);
                this.Request.AddParameter("customerTin", _oDocument.CounterPart_LicTradNum);
                this.Request.AddParameter("series", _oDocument.ImpactDocument.series);
                this.Request.AddParameter("number", _oDocument.DocNum);
                this.Request.AddParameter("dateIssued", _oDocument.ImpactDocument.dateIssued.ToString("yyyyMMddHHmmss"));
                this.Request.AddParameter("totalAmount", _oDocument.ImpactDocument.summaries.totalGrossValue.ToString().Replace(",", "."));
                this.Request.AddParameter("internalId", _oDocument.mKey);
                IRestResponse oResponse = this.Client.Execute(this.Request);
                string msignature = oResponse.Content.Replace("\"", "");
                */

                string str = "EL" + _oDocument.ImpactDocument.Issuer.Vat + "-"
                            + _oDocument.CounterPart_LicTradNum + "-"
                            + _oDocument.ImpactDocument.series + "-"
                            + _oDocument.DocNum + "-"
                            + _oDocument.ImpactDocument.dateIssued.ToString("yyyyMMddHHmmss") + "-"
                            + _oDocument.ImpactDocument.summaries.totalGrossValue.ToString().Replace(",", ".") + "-"
                            + _oDocument.mKey
                            + sSubscription;

                var hash = new SHA1Managed().ComputeHash(System.Text.Encoding.UTF8.GetBytes(str));
                string signature = string.Concat(hash.Select(b => b.ToString("x2")));

                /////////////// create and save JSON /////////////////////
                sEndPoint = ini.IniReadValue("Default", "ENDPOINT_GET_OFFLINEQR");
                _oDocument.offlineQR = sEndPoint +
                    "?issuerTin=" + _oDocument.ImpactDocument.Issuer.Vat +
                    "&customerTin=" + _oDocument.CounterPart_LicTradNum +
                    "&customerName=" + _oDocument.CounterPart_name +
                    "&invoiceType=" + _oDocument.ImpactDocument.invoiceTypeCode +
                    "&series=" + _oDocument.ImpactDocument.series +
                    "&number=" + _oDocument.DocNum +
                    "&dateIssued=" + _oDocument.ImpactDocument.dateIssued.ToString("yyyyMMddHHmmss") +
                    "&totalAmount=" + _oDocument.ImpactDocument.summaries.totalGrossValue.ToString().Replace(",", ".") +
                    "&totalNetAmount=" + _oDocument.ImpactDocument.summaries.totalNetAmount.ToString().Replace(",", ".") +
                    "&totalVatAmount=" + _oDocument.ImpactDocument.summaries.totalVATAmount.ToString().Replace(",", ".") +
                    "&internalId=" + _oDocument.mKey +
                    "&Signature=" + signature;

                iRetVal++;

            }
            catch (Exception ex)
            {
                var a = new Logging("myDataMethods.Send2AADE", ex);
            }
            return iRetVal;
        }


        private int SaveResponseXML(BoDocument oDocument, string sPath, string content, out string sJSON)
        {
            int iRetVal = 0;
            sJSON = "";
            try
            {

                using (StreamWriter sw = File.CreateText(sPath))
                {
                    sJSON = content;
                    sJSON = sJSON.Substring(1, sJSON.Length - 1);
                    sJSON = sJSON.Replace("\\r\\n", "");
                    sJSON = sJSON.Replace("</ResponseDoc>\"", "</ResponseDoc>");
                    sJSON = sJSON.Replace("\\", "").Replace("\"", "\"");
                    sw.WriteLine(sJSON);
                }
                iRetVal++;
            }
            catch (Exception ex)
            {
                var a = new Logging("myDataMethods.SaveResponseXML", ex);
            }
            return iRetVal;
        }


        private int AddResponse(BoDocument _oDocument, string _sXML)
        {
            int iRetVal = 0;
            try
            {
                DAL.BoUpdateDB oLog = new DAL.BoUpdateDB();
                oLog.DocumentAA = _oDocument.DocumentAA;
                oLog.DocEntry = _oDocument.DocEntry;
                oLog.DocNum = _oDocument.DocNum;
                oLog.ObjType = _oDocument.ObjType;
                oLog.XMLReply = _sXML;
                oLog.Company = _oDocument.CompanyDB;
                iRetVal = oLog.AddResponse(CompanyConnection);
            }
            catch (Exception ex)
            {
                var a = new Logging("myDataMethods.AddResponse", ex);
            }
            return iRetVal;
        }
        private int UpdateDocument(BoDocument _oDocument, ImpactResponse _oReply)
        {
            int iRetVal = 0;
            try
            {
                string sStatusCode;
                if (!string.IsNullOrEmpty(_oReply.status))
                {
                    sStatusCode = _oReply.status;
                }
                else
                {
                    sStatusCode = "FAILURE";
                }
                string sTableName = "";
                DAL.BoUpdateDB oLog = new DAL.BoUpdateDB();
                oLog.DocumentAA = _oDocument.DocumentAA;
                oLog.DocEntry = _oDocument.DocEntry;
                oLog.ObjType = _oDocument.ObjType;
                oLog.Result = sStatusCode;
                oLog.Company = _oDocument.CompanyDB;
                sTableName = "";
                string[] successArray = { "SUBMITTED", "CONFLICT" };
                if (successArray.Contains(sStatusCode))
                {
                    _oDocument.MARK = _oReply.mark.ToString();
                    _oDocument.UID = _oReply.uid;
                    _oDocument.StatusCode = sStatusCode;
                    _oDocument.Auth = _oReply.authenticationCode;
                    _oDocument.Domain = _oReply.domain;
                    _oDocument.IntegritySignature = _oReply.integritySignature;
                    _oDocument.Signature = _oReply.signature;
                    if (!string.IsNullOrEmpty(_oDocument.Domain) && !string.IsNullOrEmpty(_oDocument.Signature))
                    {
                        _oDocument.QR = _oDocument.Domain + "/V/" + _oDocument.Signature;
                    }
                    else
                    {
                        _oDocument.QR = null;
                    }

                    oLog.MARK = _oReply.mark.ToString();
                    oLog.UID = _oReply.uid;
                    oLog.StatusCode = sStatusCode;
                    oLog.Auth = _oReply.authenticationCode;
                    oLog.Domain = _oReply.domain;
                    oLog.IntegritySignature = _oReply.integritySignature;
                    oLog.Signature = _oReply.signature;
                    oLog.QR = _oDocument.QR;

                }
                else
                {
                    _oDocument.MARK = "IN_PROCESS";
                    _oDocument.UID = "";
                    _oDocument.StatusCode = sStatusCode;
                    if (_oReply.myDataErrors != null)
                    {
                        oLog.ErrorCode = _oReply.myDataErrors[0].key.ToString();
                        oLog.ErrorDescr = _oReply.myDataErrors[0].value.ToString();
                    }
                    else
                    {
                        oLog.ErrorCode = "999";
                        oLog.ErrorDescr = _oReply.message;
                    }
                    oLog.OfflineQR = _oDocument.offlineQR;

                }
                int iResult = oLog.UpdateDocument(sTableName, CompanyConnection);

            }
            catch (Exception ex)
            {
                var a = new Logging("myDataMethods.UpdateDocument", ex);
            }
            return iRetVal;
        }
        #endregion

        #region Nested Classes
        internal class LoadnCreateClass
        {
            #region Public Properties
            public List<BoDocument> ListDocuments { get; set; }
            public SAPbobsCOM.Company CompanyConnection { get; set; }

            #endregion

            #region Private Properties

            #endregion

            public LoadnCreateClass()
            {
                this.ListDocuments = new List<BoDocument>();
            }

            #region Private Methods
            private int LoadDocumentsProcess()
            {
                string sSQL = "";
                int iRetVal = 0;
                int iResult = 0;
                try
                {

                    this.ListDocuments = new List<BoDocument>();
                    BoDocument oDocument = null;

                    if (CompanyConnection.DbServerType == SAPbobsCOM.BoDataServerTypes.dst_HANADB)
                    {
                        sSQL = "SELECT * FROM TKA_V_ELECTRONIC_INVOICES_2_PROCESS_IMPACT WHERE 1=1 ORDER BY DOCDATE DESC";
                    }
                    else
                    {
                        sSQL = "SELECT * FROM TKA_V_ELECTRONIC_INVOICES_2_PROCESS_IMPACT WHERE 1=1 ORDER BY DOCDATE DESC";
                    }

                    SAPbobsCOM.Recordset oRS = CommonLibrary.Functions.Database.GetRecordSet(sSQL, CompanyConnection);

                    while (oRS.EoF == false)
                    {
                        oDocument = new BoDocument();
                        oDocument.ObjType = oRS.Fields.Item("OBJTYPE").Value.ToString();
                        oDocument.DocEntry = oRS.Fields.Item("DOCENTRY").Value.ToString();
                        oDocument.mKey = oDocument.ObjType + "_" + oDocument.DocEntry;
                        oDocument.DocNum = oRS.Fields.Item("DOCNUM").Value.ToString();
                        oDocument.DocDate = DateTime.Parse(oRS.Fields.Item("DOCDATE").Value.ToString());
                        oDocument.QR = oRS.Fields.Item("QR").Value.ToString();
                        oDocument.offlineQR = oRS.Fields.Item("OFFLINE_QR").Value.ToString();
                        oDocument.DocDate = DateTime.Parse(oRS.Fields.Item("DOCDATE").Value.ToString());
                        oDocument.TransId = oRS.Fields.Item("TRANSID").Value.ToString();
                        oDocument.resend = int.Parse(oRS.Fields.Item("RESEND").Value.ToString());
                        oDocument.ErrorCode = int.Parse(oRS.Fields.Item("ERROR_CODE").Value.ToString());

                        oDocument.LoadTotals(this.CompanyConnection);

                        #region B2G

                        //if (this.CompanyConnection.DbServerType == SAPbobsCOM.BoDataServerTypes.dst_HANADB)
                        //{
                        //    sSQL = "SELECT \"B2G\" FROM TKA_V_CHECK_B2G WHERE \"ObjType\"=" + oDocument.ObjType + " and \"DocEntry\"=" + oDocument.DocEntry;
                        //}
                        //else
                        //{
                        //    sSQL = "SELECT B2G FROM TKA_V_CHECK_B2G WHERE ObjType=" + oDocument.ObjType + " and DocEntry=" + oDocument.DocEntry;
                        //}

                        //oDocument.B2G = CommonLibrary.Functions.Database.ReturnDBValues(sSQL, "B2G", CompanyConnection).ToString();
                        oDocument.B2G = "N";

                        #endregion

                        this.ListDocuments.Add(oDocument);

                        if (oDocument.resend == 0)
                        {
                            iResult += this.AddDocumentToTable(CompanyConnection, oDocument);
                        }
                        else
                        {
                            iResult++;
                        }
                        oRS.MoveNext();
                        //iResult+=this.LoadDocuments()
                    }
                    if (iResult == oRS.RecordCount)
                    {
                        iRetVal++;
                    }
                }
                catch (Exception ex)
                {
                    Logging.WriteToLog("_sSQL=" + sSQL, Logging.LogStatus.RET_VAL);
                    var a = new Logging("myDataMethods.LoadnCreateClass.LoadDocumentsProcess", ex);
                }
                return iRetVal;
            }

            private int PrepareDocumentsProcess()
            {
                int iRetVal = 0;
                try
                {
                    int iResult = 0;
                    int iSuccess = this.ListDocuments.Count;

                    for (int i = 0; i < this.ListDocuments.Count; i++)
                    {
                        BoDocument oTemp = new BoDocument();
                        oTemp = this.ListDocuments[i];
                        int iTempResult = this.PrepareDocument(ref oTemp);
                        iResult += iTempResult;
                        if (iTempResult == 1)
                        {
                            //iRetVal++;
                            oTemp.DocumentStatus = DocumentPrepared.p_Success;
                        }
                        else
                        {
                            Logging.WriteToLog("Error Found On Document:" + oTemp.ObjType + " / " + oTemp.DocNum + "", Logging.LogStatus.ERROR);
                            oTemp.DocumentStatus = DocumentPrepared.pFailure;
                            this.SetIgnoreDue2Error(oTemp);
                        }
                        this.ListDocuments[i] = oTemp;
                    }

                    if (iResult == iSuccess)
                    {
                        iRetVal++;
                    }
                }
                catch (Exception ex)
                {
                    var a = new Logging("myDataMethods.LoadnCreateClass.LoadDocuments", ex);
                }
                return iRetVal;
            }

            public int AddDocumentToTable(SAPbobsCOM.Company CompanyConnection, BoDocument oDocument)
            {
                int iRetVal = 0;
                string sSQL = "";
                try
                {
                    string sFileLocation = "C:\\Program Files\\sap\\ImpactElectronicInvoicingDA\\ConfParams.ini";
                    CommonLibrary.Ini.IniFile ini = new CommonLibrary.Ini.IniFile(sFileLocation);
                    string sConnectionString = ini.IniReadValue("Default", "MSSQLConnectionString");
                    if (CompanyConnection.DbServerType == SAPbobsCOM.BoDataServerTypes.dst_HANADB)
                    {
                        sSQL = "call \"DOCUMENTS_INSERT_IMPACT\"(" +
                            "'" + CompanyConnection.CompanyName + "'," +
                            "'" + oDocument.ObjType + "'," +
                            "'" + oDocument.DocEntry + "'," +
                            "'" + oDocument.DocNum + "'," +
                            "'" + oDocument.B2G + "'," +
                            "'" + oDocument.DocDate.ToString("yyyyMMdd") + "'," +
                            "'" + oDocument.TransId + "'," +
                            "'" + oDocument.offlineQR +
                            "')";
                        SAPbobsCOM.Recordset oRS = CommonLibrary.Functions.Database.GetRecordSet(sSQL, CompanyConnection);
                        if (oRS == null)
                        {
                            Logging.WriteToLog("failed to insert to eliv_documents", Logging.LogStatus.ERROR);
                            Logging.WriteToLog("sSQL=" + sSQL, Logging.LogStatus.ERROR);
                        }
                        else
                        {
                            iRetVal++;

                        }
                    }
                    else
                    {
                        using (SqlConnection oConnection = new SqlConnection(sConnectionString))
                        {
                            oConnection.Open();

                            using (SqlCommand oCommand = new SqlCommand("[dbo].RESPONSES_INSERT", oConnection))
                            {
                                oCommand.CommandTimeout = 0;
                                oCommand.Parameters.Add(new SqlParameter("@CompanyName", "" + CompanyConnection.CompanyName + ""));
                                oCommand.Parameters.Add(new SqlParameter("@ObjType", "" + oDocument.ObjType + ""));
                                oCommand.Parameters.Add(new SqlParameter("@DocEntry", "" + oDocument.DocEntry + ""));
                                oCommand.Parameters.Add(new SqlParameter("@DocNum", "" + oDocument.DocNum + ""));
                                oCommand.Parameters.Add(new SqlParameter("@DocNum", "" + oDocument.B2G + ""));
                                oCommand.Parameters.Add(new SqlParameter("@DocDate", "" + oDocument.DocDate.ToString("yyyyMMdd") + ""));
                                oCommand.Parameters.Add(new SqlParameter("@TransId", "" + oDocument.TransId + ""));
                                oCommand.Parameters.Add(new SqlParameter("@offlineQR", "" + oDocument.offlineQR + ""));

                                oCommand.CommandType = CommandType.StoredProcedure;

                                oCommand.ExecuteScalar();
                            }
                            oConnection.Close();
                        }
                        iRetVal++;
                    }

                }
                catch (Exception ex)
                {
                    Logging.WriteToLog("sSQL=" + sSQL, Logging.LogStatus.RET_VAL);
                    var a = new Logging("BoDAL.AddResponse", ex);
                }
                return iRetVal;
            }

            private int PrepareDocument(ref BoDocument _oDocument)
            {
                int iRetVal = 0;
                int iResult = 0;
                string sSQL = "";
                try
                {
                    iResult = 0;
                    iResult = LoadFullDocumentData(ref _oDocument);
                    if (iResult == 1)
                    {
                        iRetVal++;
                    }
                }
                catch (Exception ex)
                {
                    var a = new Logging("myDataMethods.LoadnCreateClass.LoadDocuments", ex);
                }
                return iRetVal;
            }

            private int LoadFullDocumentData(ref BoDocument _oDocument)
            {
                int iResult = 0;
                int iRetVal = 0;
                int iSuccess = 9;
                try
                {
                    int iTempHeader, iTempPayment, iTempIssuer, iTempCounterPart, iTempTaxesTotals, iTempDocumentSummary, iTempDetails, iTempDistrDetails, iTempAddDetails, iTempDestDetails, iTempOriginDetails, iTempB2G, iTempVatAnalysis;
                    iTempHeader = iTempPayment = iTempIssuer = iTempCounterPart = iTempTaxesTotals = iTempDocumentSummary = iTempDetails = iTempDistrDetails = iTempAddDetails = iTempDestDetails = iTempOriginDetails = iTempB2G = iTempVatAnalysis = 0;

                    _oDocument.ImpactDocument = new ImpactDocument();
                    iTempHeader = this.GetInvoiceHeader(ref _oDocument);

                    #region B2G
                    //string sSQL;
                    //if (this.CompanyConnection.DbServerType == SAPbobsCOM.BoDataServerTypes.dst_HANADB)
                    //{
                    //    sSQL = "SELECT \"B2G\" FROM TKA_V_CHECK_B2G WHERE \"ObjType\"=" + _oDocument.ObjType + " and \"DocEntry\"=" + _oDocument.DocEntry;
                    //}
                    //else
                    //{
                    //    sSQL = "SELECT B2G FROM TKA_V_CHECK_B2G WHERE ObjType=" + _oDocument.ObjType + " and DocEntry=" + _oDocument.DocEntry;
                    //}

                    //_oDocument.B2G = CommonLibrary.Functions.Database.ReturnDBValues(sSQL, "B2G", CompanyConnection).ToString();

                    //if (_oDocument.B2G.Equals("Y"))
                    //{
                    //    iTempB2G = this.LoadB2G(_oDocument);
                    //}
                    //else
                    //{
                    //    iTempB2G++;

                    //}

                    #endregion


                    CommonLibrary.Ini.IniFile ini = new CommonLibrary.Ini.IniFile("C:\\Program Files\\sap\\ImpactElectronicInvoicingDA\\ConfParams.ini");
                    string sJEPaymentMethods = ini.IniReadValue("Default", "PAYMENT_METHODS");
                    List<string> ListJEPaymentMethods = new List<string>();
                    ListJEPaymentMethods = sJEPaymentMethods.Split(',').ToList();


                    if (_oDocument.ObjType != "30" || (_oDocument.ObjType == "30" && ListJEPaymentMethods.Contains(_oDocument.ImpactDocument.invoiceTypeCode.ToString()) == true))
                    {

                        _oDocument.ImpactDocument.paymentDetails = this.GetPaymentMethods(_oDocument, out iTempPayment);


                        string sNoCounterPart = ini.IniReadValue("Default", "NO_COUNTERPART");
                        List<string> ListNoCounterpart = new List<string>();
                        ListNoCounterpart = sNoCounterPart.Split(',').ToList();

                        if (ListNoCounterpart.Contains(_oDocument.ImpactDocument.invoiceTypeCode.ToString()) == false)
                        {
                            _oDocument.ImpactDocument.counterParty = this.GetCounterPart(_oDocument, out iTempCounterPart, _oDocument.ImpactDocument.invoiceTypeCode.ToString());
                        }
                        else
                        {
                            iTempCounterPart++;
                        }
                    }
                    else
                    {
                        iTempPayment++;
                        iTempCounterPart++;
                    }

                    _oDocument.ImpactDocument.Issuer = this.GetIssuer(out iTempIssuer, _oDocument);

                    _oDocument.ImpactDocument.DistributionDetails = this.GetDistributionDetails(_oDocument, out iTempDistrDetails);

                    _oDocument.ImpactDocument.additionalDetails = this.GetAdditionalDetails(_oDocument, out iTempAddDetails);
                    //_oDocument.ImpactDocument.deliveryDestinationDetails = this.GetDestinationDetails(_oDocument, out iTempDestDetails);
                    //_oDocument.ImpactDocument.deliveryOriginDetails = this.GetOriginDetails(_oDocument, out iTempOriginDetails);


                    //if (_oDocument.TotalTaxesAmount > 0)
                    //{
                    //    //List<Τaxes> ListRet;
                    //    //ListRet = this.GetTaxesTotals(ref _oDocument, out iTempTaxesTotals);
                    //    //_oDocument.ImpactDocument.Τaxes = ListRet.ToArray();
                    //}
                    //else
                    //{
                    //    iTempTaxesTotals = 1;
                    //}

                    List<Detail> ListDetail;
                    ListDetail = this.GetDetails(_oDocument, _oDocument.ImpactDocument.invoiceTypeCode.ToString(), out iTempDetails);
                    _oDocument.ImpactDocument.details = ListDetail.ToArray();

                    _oDocument.ImpactDocument.summaries = this.GetInvoiceSummary(_oDocument, out iTempDocumentSummary, _oDocument.ImpactDocument);


                    iTempVatAnalysis = LoadVatAnalysis(_oDocument);

                    iResult = iTempHeader + iTempPayment + iTempIssuer + iTempCounterPart + iTempDocumentSummary + iTempDetails + iTempAddDetails + iTempDistrDetails + iTempVatAnalysis;

                    if (iResult == iSuccess)
                    {
                        iRetVal++;
                        _oDocument.DocumentStatus = DocumentPrepared.p_Success;
                    }
                }
                catch (Exception ex)
                {
                    var a = new Logging("myDataMethods.LoadnCreateClass.LoadDocuments", ex);
                }
                return iRetVal;
            }

            /// <summary>
            /// Δημιουργία Γραμμών Παραστατικού
            /// </summary>
            /// <param name="_oDocument">Το Αντικείμενο του Παραστατικού</param>
            /// <param name="_iResult">1 For Success, 0 For Failure</param>
            /// <returns>Το Αντικείμενο της ΑΑΔΕ για τις γραμμές του Παραστατικού</returns>
            private List<Detail> GetDetails(BoDocument _oDocument, string invoiceType, out int _iResult)
            {
                _iResult = 0;
                List<Detail> oRet = new List<Detail>();
                string sSQL = "";
                try
                {
                    Detail oRow = null;
                    if (this.CompanyConnection.DbServerType == SAPbobsCOM.BoDataServerTypes.dst_HANADB)
                    {
                        sSQL = "SELECT * FROM TKA_V_ELECTRONIC_INVOICES_DETAILS_IMPACT_WRAPPER WHERE 1=1 AND \"ObjType\" = '" + _oDocument.ObjType + "' AND \"DocEntry\" = '" + _oDocument.DocEntry + "'";
                    }
                    else
                    {
                        sSQL = "SELECT * FROM TKA_V_ELECTRONIC_INVOICES_DETAILS_IMPACT_WRAPPER WHERE 1=1 AND ObjType = '" + _oDocument.ObjType + "' AND DocEntry = '" + _oDocument.DocEntry + "'";
                    }

                    SAPbobsCOM.Recordset oRS = CommonLibrary.Functions.Database.GetRecordSet(sSQL, this.CompanyConnection);

                    int iRow = 0;
                    while (oRS.EoF == false)
                    {
                        iRow++;
                        oRow = new Detail();

                        #region Required
                        oRow.lineNo = iRow;
                        oRow.classificationLineNo = iRow;
                        oRow.code = oRS.Fields.Item("ItemCode").Value.ToString();
                        oRow.descriptions = new string[1];
                        oRow.descriptions[0] = oRS.Fields.Item("ItemName").Value.ToString();
                        oRow.quantity = decimal.Parse(oRS.Fields.Item("quantity").Value.ToString());
                        oRow.UnitPrice = decimal.Parse(oRS.Fields.Item("UnitPrice").Value.ToString());
                        string mUnit = oRS.Fields.Item("measurementUnit").Value.ToString();
                        string mUnitCode = oRS.Fields.Item("measurementUnitCode").Value.ToString();
                        if (!string.IsNullOrEmpty(mUnit) && !string.IsNullOrEmpty(mUnitCode) && !mUnit.Equals("-112") && !mUnitCode.Equals("-112"))
                        {
                            oRow.measurementUnit = mUnit;
                            oRow.measurementUnitCode = int.Parse(mUnitCode);
                            if (mUnitCode.Equals("7"))
                            {
                                oRow.otherMeasurementUnitTitle = oRS.Fields.Item("measurementUnit").Value.ToString();
                                oRow.otherMeasurementUnitQuantity = decimal.Parse(oRS.Fields.Item("quantity").Value.ToString());
                            }

                        }
                        //oRow.unitPriceAC = Math.Round(decimal.Parse(oRS.Fields.Item("unitPriceAC").Value.ToString()), 2);
                        //oRow.totalAC = Math.Round((decimal.Parse(oRS.Fields.Item("totalAC").Value.ToString())), 2);
                        //oRow.netTotalAC = Math.Round((decimal.Parse(oRS.Fields.Item("netTotalAC").Value.ToString())), 2);
                        oRow.chargesTotal = Math.Round((decimal.Parse(oRS.Fields.Item("chargesTotal").Value.ToString())), 2);
                        oRow.Total = Math.Round((decimal.Parse(oRS.Fields.Item("Total").Value.ToString())), 2);


                        oRow.totalNetValueBeforeTotalDiscount = Math.Round((decimal.Parse(oRS.Fields.Item("totalNetValueBeforeTotalDiscount").Value.ToString())), 2);
                        oRow.netTotal = Math.Round((decimal.Parse(oRS.Fields.Item("netValue").Value.ToString())), 2);
                        oRow.vatTotal = Math.Round((decimal.Parse(oRS.Fields.Item("vatAmount").Value.ToString())), 2);
                        oRow.total = oRow.netTotal + oRow.vatTotal;
                        oRow.allowancesTotal = Math.Round((decimal.Parse(oRS.Fields.Item("allowancesTotal").Value.ToString())), 2);

                        oRow.allowancesCharges = new Allowancescharge[3];
                        Allowancescharge allowancescharge = new Allowancescharge();
                        allowancescharge.amount = Math.Round((decimal.Parse(oRS.Fields.Item("amount1").Value.ToString())), 2);
                        allowancescharge.percentage = oRS.Fields.Item("percentage1").Value.ToString();
                        allowancescharge.underlyingValue = Math.Round((decimal.Parse(oRS.Fields.Item("underlyingValue1").Value.ToString())), 2);
                        allowancescharge.descriptions = oRS.Fields.Item("descriptions1").Value.ToString();
                        allowancescharge.code = oRS.Fields.Item("code1").Value.ToString();
                        allowancescharge.Type = int.Parse(oRS.Fields.Item("Type1").Value.ToString());
                        oRow.allowancesCharges[0] = allowancescharge;

                        Allowancescharge allowancescharge1 = new Allowancescharge();
                        allowancescharge1.amount = Math.Round((decimal.Parse(oRS.Fields.Item("amount2").Value.ToString())), 2);
                        allowancescharge1.percentage = oRS.Fields.Item("percentage2").Value.ToString();
                        allowancescharge1.underlyingValue = Math.Round((decimal.Parse(oRS.Fields.Item("underlyingValue2").Value.ToString())), 2);
                        allowancescharge1.descriptions = oRS.Fields.Item("descriptions2").Value.ToString();
                        allowancescharge1.code = oRS.Fields.Item("code2").Value.ToString();
                        allowancescharge1.Type = int.Parse(oRS.Fields.Item("Type2").Value.ToString());
                        oRow.allowancesCharges[1] = allowancescharge1;

                        Allowancescharge allowancescharge2 = new Allowancescharge();
                        allowancescharge2.amount = Math.Round((decimal.Parse(oRS.Fields.Item("amount3").Value.ToString())), 2);
                        allowancescharge2.percentage = oRS.Fields.Item("percentage3").Value.ToString();
                        allowancescharge2.underlyingValue = Math.Round((decimal.Parse(oRS.Fields.Item("underlyingValue3").Value.ToString())), 2);
                        allowancescharge2.descriptions = oRS.Fields.Item("descriptions3").Value.ToString();
                        allowancescharge2.code = oRS.Fields.Item("code3").Value.ToString();
                        allowancescharge2.Type = int.Parse(oRS.Fields.Item("Type3").Value.ToString());
                        oRow.allowancesCharges[2] = allowancescharge2;

                        string vatCat = oRS.Fields.Item("vatCategory").Value.ToString();
                        oRow.vatCategoryCode = int.Parse(vatCat);
                        if (oRow.vatCategoryCode == 7 || oRow.vatCategoryCode == 8)
                        {
                            oRow.vatExemptionCategoryCode = int.Parse(oRS.Fields.Item("vatExemptionCategory").Value.ToString());
                        }
                        if (!string.IsNullOrEmpty(vatCat))
                        {
                            oRow.vatCategory = GetVatCategory(vatCat) + "%";

                        }

                        string taxCat = oRS.Fields.Item("otherTaxesPercentCategory").Value.ToString();
                        if (!string.IsNullOrEmpty(taxCat) && !taxCat.Equals("-112"))
                        {
                            oRow.otherTaxesPercentCategoryCode = int.Parse(taxCat);
                            oRow.otherTaxesPercentCategory = "ΕΦΚ";
                        }


                        string recType = oRS.Fields.Item("recordTypeCode").Value.ToString();
                        if (!string.IsNullOrEmpty(recType) && !recType.Equals("0"))
                        {
                            oRow.recordTypeCode = int.Parse(recType);
                        }

                        int isInformative = int.Parse(oRS.Fields.Item("isInformative").Value.ToString());
                        int isHidden = int.Parse(oRS.Fields.Item("isHidden").Value.ToString());
                        if (isInformative == 1)
                        {
                            oRow.isInformative = true;
                        }
                        else
                        {
                            oRow.isInformative = false;
                        }
                        if (isHidden == 1)
                        {
                            oRow.isHidden = true;
                        }
                        else
                        {
                            oRow.isHidden = false;
                        }

                        CommonLibrary.Ini.IniFile ini = new CommonLibrary.Ini.IniFile("C:\\Program Files\\sap\\ImpactElectronicInvoicingDA\\ConfParams.ini");
                        string sNoClassRecType = ini.IniReadValue("Default", "NO_CLASSIFICATION_RECTYPE");
                        List<string> ListNoClassRecType = new List<string>();
                        ListNoClassRecType = sNoClassRecType.Split(',').ToList();

                        string sNoClassDelivery = ini.IniReadValue("Default", "NO_CLASSIFICATION_DELIVERY");
                        List<string> ListNoClassDelivery = new List<string>();
                        ListNoClassDelivery = sNoClassDelivery.Split(',').ToList();

                        if (ListNoClassRecType.Contains(invoiceType) == false && ListNoClassDelivery.Contains(invoiceType) == false)
                        {

                            //if (recType.Equals("0") && !invoiceType.Equals("9.3"))
                            //{
                            oRow.incomeClassification = new Incomeclassification();

                            oRow.incomeClassification.classificationCategoryCode = oRS.Fields.Item("classificationCategory").Value.ToString();
                            string classificationType = oRS.Fields.Item("classificationType").Value.ToString();
                            if (!string.IsNullOrEmpty(classificationType) && !classificationType.Equals("-112"))
                            {
                                oRow.incomeClassification.classificationTypeCode = classificationType;
                            }
                        }
                        if (_oDocument.B2G.Equals("Y"))
                        {
                            oRow.CpvCode = _oDocument.CpvCode;
                            oRow.measurementUnitCodeEN = _oDocument.measurementUnitCodeEN;
                        }

                        #endregion

                        oRet.Add(oRow);

                        oRS.MoveNext();
                    }


                    _iResult++;
                }
                catch (Exception ex)
                {
                    Logging.WriteToLog("sSQL=" + sSQL, Logging.LogStatus.RET_VAL);
                    var a = new Logging("myDataMethods.LoadnCreateClass.GetDetails", ex);
                }
                return oRet;
            }


            private string GetVatCategory(string vatCat)
            {
                string retVal = "";
                switch (vatCat)
                {
                    case "1":
                        retVal = "24";
                        break;
                    case "2":
                        retVal = "13";
                        break;
                    case "3":
                        retVal = "6";
                        break;
                    case "4":
                        retVal = "17";
                        break;
                    case "5":
                        retVal = "9";
                        break;
                    case "6":
                        retVal = "4";
                        break;
                    case "7":
                        retVal = "0";
                        break;
                    case "8":
                        retVal = "-";
                        break;
                }
                return retVal;
            }


            //private List<Τaxes> GetTaxesTotals(ref BoDocument _oDocument, out int _iRetVal)
            //{
            //    _iRetVal = 0;
            //    List<Τaxes> oRet = new List<Τaxes>();
            //    string sSQL = "";
            //    try
            //    {
            //        Τaxes oType = null;

            //        if (this.CompanyConnection.DbServerType == SAPbobsCOM.BoDataServerTypes.dst_HANADB)
            //        {
            //            sSQL = "SELECT * FROM TKA_V_ELECTRONIC_INVOICES_TAXES_TOTALS_IMPACT_WRAPPER WHERE 1=1 AND \"ObjType\" = '" + _oDocument.ObjType + "' AND \"DocEntry\" = '" + _oDocument.DocEntry + "'";
            //        }
            //        else
            //        {
            //            sSQL = "SELECT * FROM TKA_V_ELECTRONIC_INVOICES_TAXES_TOTALS_IMPACT_WRAPPER WHERE 1=1 AND ObjType = '" + _oDocument.ObjType + "' AND DocEntry = '" + _oDocument.DocEntry + "'";
            //        }

            //        SAPbobsCOM.Recordset oRS = CommonLibrary.Functions.Database.GetRecordSet(sSQL, this.CompanyConnection);

            //        while (oRS.EoF == false)
            //        {
            //            oType = new Τaxes();
            //            oType.ΤaxAmount = Math.Round((decimal.Parse(oRS.Fields.Item("TAX_AMOUNT").Value.ToString())), 2);
            //            oType.TaxType = oRS.Fields.Item("TAX_CODE").Value.ToString();
            //            oType.ΤaxTypeCode = int.Parse(oRS.Fields.Item("TAX_CODE").Value.ToString());
            //            oType.ΤaxCategory = oRS.Fields.Item("TAX_CATEGORY").Value.ToString();
            //            oType.ΤaxCategoryCode = int.Parse(oRS.Fields.Item("TAX_CATEGORY").Value.ToString());
            //            //oType.underlyingValue = Math.Round((decimal.Parse(oRS.Fields.Item("TAX_BASE_AMOUNT").Value.ToString())), 2);   //not used

            //            oRet.Add(oType);

            //            oRS.MoveNext();
            //        }
            //        _iRetVal++;
            //    }
            //    catch (Exception ex)
            //    {
            //        var a = new Logging("myDataMethods.LoadnCreateClass.GetInvoiceSummary", ex);
            //    }
            //    return oRet;
            //}

            /// <summary>
            /// Δημιουργία Totals Classifications
            /// </summary>
            /// <param name="_oIncomeClassification">Λίστα classifications εσόδων</param>
            /// <param name="_oExpensesClassification">Λίστα classifications εξόδων</param>
            /// <returns>1 for success, 0 for failure</returns>
            private int GetInvoiceTotalsClassifications(BoDocument _oDocument, out decimal dTotal, out Summaries oRet)
            {
                int iRetVal = 0;
                string sSQL = "";
                oRet = new Summaries();
                dTotal = 0;
                try
                {
                    if (this.CompanyConnection.DbServerType == SAPbobsCOM.BoDataServerTypes.dst_HANADB)
                    {
                        sSQL = "SELECT * FROM TKA_V_ELECTRONIC_INVOICES_TOTALS_IMPACT_WRAPPER WHERE 1=1 AND \"ObjType\" = '" + _oDocument.ObjType + "' AND \"DocEntry\" = '" + _oDocument.DocEntry + "'";
                    }
                    else
                    {
                        sSQL = "SELECT * FROM TKA_V_ELECTRONIC_INVOICES_TOTALS_IMPACT_WRAPPER WHERE 1=1 AND ObjType = '" + _oDocument.ObjType + "' AND DocEntry = '" + _oDocument.DocEntry + "'";
                    }

                    SAPbobsCOM.Recordset oRS = CommonLibrary.Functions.Database.GetRecordSet(sSQL, this.CompanyConnection);

                    while (oRS.EoF == false)
                    {
                        dTotal += decimal.Parse(oRS.Fields.Item("Amount").Value.ToString());
                        oRet.totalCatalogNetAmount = Math.Round(decimal.Parse(oRS.Fields.Item("totalCatalogNetAmount").Value.ToString()), 2);
                        //oRet.totalCharges = Math.Round(decimal.Parse(oRS.Fields.Item("totalCharges").Value.ToString()), 2);
                        //oRet.totalNetAmountAC = Math.Round(decimal.Parse(oRS.Fields.Item("totalNetAmountAC").Value.ToString()), 2);
                        oRet.totalSpecialCharges = Math.Round(decimal.Parse(oRS.Fields.Item("totalSpecialCharges").Value.ToString()), 2);
                        //oRet.totalSpecialChargesAC = Math.Round(decimal.Parse(oRS.Fields.Item("totalSpecialChargesAC").Value.ToString()), 2);
                        oRet.totalPayableAmount = Math.Round(decimal.Parse(oRS.Fields.Item("totalPayableAmount").Value.ToString()), 2);
                        //oRet.totalVATAmountAC = Math.Round(decimal.Parse(oRS.Fields.Item("totalVATAmountAC").Value.ToString()), 2);
                        oRS.MoveNext();
                    }
                    dTotal = Math.Round(dTotal, 2);
                    iRetVal++;
                }
                catch (Exception ex)
                {
                    var a = new Logging("myDataMethods.LoadnCreateClass.GetInvoiceSummary", ex);
                }
                return iRetVal;
            }

            private Summaries GetInvoiceSummary(BoDocument _oDocument, out int _iResult, ImpactDocument oImpactDocument)
            {
                _iResult = 0;
                Summaries oRet = null;// = new Summaries();
                List<taxes> taxes = new List<taxes>();
                string sSQL = "";
                try
                {
                    decimal _dTotal = 0;
                    int iResult = this.GetInvoiceTotalsClassifications(_oDocument, out _dTotal, out oRet);
                    decimal dTotal = _dTotal;

                    //////////////////////////////////////////////////////////////////

                    if (this.CompanyConnection.DbServerType == SAPbobsCOM.BoDataServerTypes.dst_HANADB)
                    {
                        sSQL = "SELECT SUM(TAX_AMOUNT) AS \"Result\"," + Environment.NewLine +
                            " TAX_CODE," + Environment.NewLine +
                            " TAX_CATEGORY" + Environment.NewLine +
                            " FROM TKA_V_ELECTRONIC_INVOICES_TAXES_TOTALS_IMPACT_WRAPPER" + Environment.NewLine +
                            " WHERE 1 = 1" + Environment.NewLine +
                            " AND \"ObjType\" = '" + _oDocument.ObjType + "'" + Environment.NewLine +
                            " AND \"DocEntry\" = '" + _oDocument.DocEntry + "'" + Environment.NewLine +
                            " GROUP BY TAX_CODE,TAX_CATEGORY";
                    }
                    else
                    {
                        sSQL = "SELECT SUM(TAX_AMOUNT) AS Result," + Environment.NewLine +
                            " TAX_CODE," + Environment.NewLine +
                            " TAX_CATEGORY" + Environment.NewLine +
                            " FROM TKA_V_ELECTRONIC_INVOICES_TAXES_TOTALS_IMPACT_WRAPPER" + Environment.NewLine +
                            " WHERE 1 = 1" + Environment.NewLine +
                            " AND ObjType = '" + _oDocument.ObjType + "'" + Environment.NewLine +
                            " AND DocEntry = '" + _oDocument.DocEntry + "'" + Environment.NewLine +
                            " GROUP BY TAX_CODE,TAX_CATEGORY";
                    }
                    SAPbobsCOM.Recordset oRS = CommonLibrary.Functions.Database.GetRecordSet(sSQL, this.CompanyConnection);
                    oRS = CommonLibrary.Functions.Database.GetRecordSet(sSQL, this.CompanyConnection);
                    decimal dTotalFees, dTotalStamp, dTotalDeductions, dTotalOtherTaxes, dTotalWithheldTaxes, dTotalAllownaces;
                    dTotalFees = dTotalStamp = dTotalDeductions = dTotalOtherTaxes = dTotalWithheldTaxes = dTotalAllownaces = 0;

                    while (oRS.EoF == false)
                    {
                        switch ((string)oRS.Fields.Item("TAX_CODE").Value.ToString())
                        {
                            case "1":
                                taxes tax1 = new taxes();
                                tax1.TaxAmount = decimal.Parse(oRS.Fields.Item("Result").Value.ToString());
                                tax1.TaxTypeCode = int.Parse(oRS.Fields.Item("TAX_CODE").Value.ToString());
                                tax1.taxCategoryCode = int.Parse(oRS.Fields.Item("TAX_CATEGORY").Value.ToString());
                                tax1.taxType = "ΠΑΡΑΚΡΑΤΗΣΗ";
                                tax1.taxCategory = "ΠΑΡΑΚΡΑΤΗΣΗ";
                                taxes.Add(tax1);
                                dTotalWithheldTaxes = decimal.Parse(oRS.Fields.Item("Result").Value.ToString());
                                break;
                            case "2":
                                taxes tax2 = new taxes();
                                tax2.TaxAmount = decimal.Parse(oRS.Fields.Item("Result").Value.ToString());
                                tax2.TaxTypeCode = int.Parse(oRS.Fields.Item("TAX_CODE").Value.ToString());
                                tax2.taxCategoryCode = int.Parse(oRS.Fields.Item("TAX_CATEGORY").Value.ToString());
                                tax2.taxType = "ΤΕΛΗ";
                                tax2.taxCategory = "ΤΕΛΗ";
                                taxes.Add(tax2);
                                dTotalFees = decimal.Parse(oRS.Fields.Item("Result").Value.ToString());
                                break;
                            case "3":
                                taxes tax3 = new taxes();
                                tax3.TaxAmount = decimal.Parse(oRS.Fields.Item("Result").Value.ToString());
                                tax3.TaxTypeCode = int.Parse(oRS.Fields.Item("TAX_CODE").Value.ToString());
                                tax3.taxCategoryCode = int.Parse(oRS.Fields.Item("TAX_CATEGORY").Value.ToString());
                                tax3.taxType = "Λοιποί Φόροι";
                                tax3.taxCategory = "Λοιποί Φόροι";
                                taxes.Add(tax3);
                                dTotalOtherTaxes = decimal.Parse(oRS.Fields.Item("Result").Value.ToString());
                                break;
                            case "4":
                                taxes tax4 = new taxes();
                                tax4.TaxAmount = decimal.Parse(oRS.Fields.Item("Result").Value.ToString());
                                tax4.TaxTypeCode = int.Parse(oRS.Fields.Item("TAX_CODE").Value.ToString());
                                tax4.taxCategoryCode = int.Parse(oRS.Fields.Item("TAX_CATEGORY").Value.ToString());
                                tax4.taxType = "ΧΑΡΤΟΣΗΜΟ";
                                tax4.taxCategory = "ΧΑΡΤΟΣΗΜΟ";
                                taxes.Add(tax4);
                                dTotalStamp = decimal.Parse(oRS.Fields.Item("Result").Value.ToString());
                                break;
                            case "5":
                                taxes tax5 = new taxes();
                                tax5.TaxAmount = decimal.Parse(oRS.Fields.Item("Result").Value.ToString());
                                tax5.TaxTypeCode = int.Parse(oRS.Fields.Item("TAX_CODE").Value.ToString());
                                tax5.taxCategoryCode = int.Parse(oRS.Fields.Item("TAX_CATEGORY").Value.ToString());
                                tax5.taxType = "Κρατήσεις";
                                tax5.taxCategory = "Κρατήσεις";
                                taxes.Add(tax5);
                                dTotalDeductions = decimal.Parse(oRS.Fields.Item("Result").Value.ToString());
                                break;
                            case "6":
                                taxes tax6 = new taxes();
                                tax6.TaxAmount = decimal.Parse(oRS.Fields.Item("Result").Value.ToString());
                                tax6.TaxTypeCode = int.Parse(oRS.Fields.Item("TAX_CODE").Value.ToString());
                                tax6.taxCategoryCode = int.Parse(oRS.Fields.Item("TAX_CATEGORY").Value.ToString());
                                tax6.taxType = "";
                                tax6.taxCategory = "";
                                taxes.Add(tax6);
                                dTotalAllownaces = decimal.Parse(oRS.Fields.Item("Result").Value.ToString());
                                break;
                        }
                        oRS.MoveNext();
                    }

                    //***NOTE*** ALL FIELDS ARE REQUIRED!!!!

                    oImpactDocument.taxes = taxes.ToArray();

                    decimal totalDeductionsAmount = Math.Round(dTotalDeductions, 2);
                    decimal totalFeesAmount = Math.Round(dTotalFees, 2);
                    oRet.totalNetAmount = Math.Round(dTotal, 2);
                    decimal totalOtherTaxesAmount = Math.Round(dTotalOtherTaxes, 2);
                    decimal totalStampDutyAmount = Math.Round(dTotalStamp, 2);
                    oRet.totalStampDutyAmount = totalStampDutyAmount;
                    oRet.totalVATAmount = Math.Round(_oDocument.TotalVATAmount, 2);
                    decimal totalWithheldAmount = Math.Round(dTotalWithheldTaxes, 2);
                    oRet.totalAllowances = Math.Round(dTotalAllownaces, 2);
                    oRet.totalGrossValue = Math.Round(dTotal + _oDocument.TotalVATAmount - dTotalDeductions - dTotalFees + dTotalStamp - dTotalOtherTaxes - dTotalWithheldTaxes, 2);//Net + taxes (Το Taxes περιλαμβάνει όλους τους επιπλέον φόρους βλ. View Φόρων)


                    _iResult++;
                }
                catch (Exception ex)
                {
                    var a = new Logging("myDataMethods.LoadnCreateClass.GetInvoiceSummary", ex);
                }
                return oRet;
            }


            /// <summary>
            /// Δημιουργία Αντικειμένου για την Εταιρεία που ανεβάζει
            /// </summary>
            /// <param name="_iResult">1 For Success, 0 For Failure</param>
            /// <returns>Το Αντικείμενο της ΑΑΔΕ για την Εταιρεία που Ανεβάζει</returns>
            //private int LoadB2G(BoDocument _oDocument)
            //{
            //    int iResult = 0;
            //    int iRetVal = 0;
            //    string sSQL = "";
            //    try
            //    {
            //        #region details
            //        if (this.CompanyConnection.DbServerType == SAPbobsCOM.BoDataServerTypes.dst_HANADB)
            //        {
            //            sSQL = "SELECT * FROM TKA_V_ELECTRONIC_INVOICES_B2G_DETAILS WHERE \"ObjType\"=" + _oDocument.ObjType + " and \"DocEntry\"=" + _oDocument.DocEntry;
            //        }
            //        else
            //        {
            //            sSQL = "SELECT * FROM TKA_V_ELECTRONIC_INVOICES_B2G_DETAILS WHERE ObjType=" + _oDocument.ObjType + " and DocEntry=" + _oDocument.DocEntry;
            //        }

            //        SAPbobsCOM.Recordset oRS = CommonLibrary.Functions.Database.GetRecordSet(sSQL, this.CompanyConnection);

            //        _oDocument.ImpactDocument.B2GDetails = new B2GDetails();

            //        while (oRS.EoF == false)
            //        {
            //            _oDocument.ImpactDocument.B2GDetails.ContractingAuthority = oRS.Fields.Item("ContractingAuthority").Value.ToString();
            //            _oDocument.ImpactDocument.B2GDetails.ContractingAuthorityCode = oRS.Fields.Item("ContractingAuthorityCode").Value.ToString();
            //            _oDocument.ImpactDocument.B2GDetails.ContractTypeCode = oRS.Fields.Item("ContractTypeCode").Value.ToString();
            //            _oDocument.ImpactDocument.B2GDetails.ContractReferenceCode = oRS.Fields.Item("ContractReferenceCode").Value.ToString();

            //            _oDocument.CpvCode = oRS.Fields.Item("CpvCode").Value.ToString();
            //            _oDocument.measurementUnitCodeEN = oRS.Fields.Item("measurementUnitCodeEN").Value.ToString();
            //            oRS.MoveNext();
            //        }

            //        iResult++;
            //        #endregion

            //        #region recepient
            //        if (this.CompanyConnection.DbServerType == SAPbobsCOM.BoDataServerTypes.dst_HANADB)
            //        {
            //            sSQL = "SELECT * FROM TKA_V_ELECTRONIC_INVOICES_B2G_RECIPIENTS WHERE \"ObjType\"=" + _oDocument.ObjType + " and \"DocEntry\"=" + _oDocument.DocEntry;
            //        }
            //        else
            //        {
            //            sSQL = "SELECT * FROM TKA_V_ELECTRONIC_INVOICES_B2G_RECIPIENTS WHERE ObjType=" + _oDocument.ObjType + " and DocEntry=" + _oDocument.DocEntry;
            //        }

            //        SAPbobsCOM.Recordset oRSRecipients = CommonLibrary.Functions.Database.GetRecordSet(sSQL, this.CompanyConnection);

            //        _oDocument.ImpactDocument.Recipient = new Recipient();
            //        _oDocument.ImpactDocument.Recipient.Address = new RecipientAddress();

            //        while (oRSRecipients.EoF == false)
            //        {
            //            _oDocument.ImpactDocument.Recipient.RegisteredName = oRSRecipients.Fields.Item("RegisteredName").Value.ToString();
            //            _oDocument.ImpactDocument.Recipient.Vat = oRSRecipients.Fields.Item("Vat").Value.ToString();
            //            _oDocument.ImpactDocument.Recipient.Address.CountryCode = oRSRecipients.Fields.Item("CountryCode").Value.ToString();
            //            _oDocument.ImpactDocument.Recipient.Address.City = oRSRecipients.Fields.Item("City").Value.ToString();
            //            _oDocument.ImpactDocument.Recipient.Address.Street = oRSRecipients.Fields.Item("Street").Value.ToString();
            //            _oDocument.ImpactDocument.Recipient.Address.Postal = oRSRecipients.Fields.Item("Postal").Value.ToString();
            //            oRSRecipients.MoveNext();
            //        }

            //        iResult++;
            //        #endregion

            //        #region vatanalysis
            //        if (this.CompanyConnection.DbServerType == SAPbobsCOM.BoDataServerTypes.dst_HANADB)
            //        {
            //            sSQL = "SELECT * FROM TKA_V_ELECTRONIC_INVOICES_B2G_VATANALYSIS WHERE \"ObjType\"=" + _oDocument.ObjType + " and \"DocEntry\"=" + _oDocument.DocEntry;
            //        }
            //        else
            //        {
            //            sSQL = "SELECT * FROM TKA_V_ELECTRONIC_INVOICES_B2G_VATANALYSIS WHERE ObjType=" + _oDocument.ObjType + " and DocEntry=" + _oDocument.DocEntry;
            //        }

            //        SAPbobsCOM.Recordset oRSVat = CommonLibrary.Functions.Database.GetRecordSet(sSQL, this.CompanyConnection);


            //        //TODO να διαβάζω πρώτα λίστ και να τα μεταφέρω μετά σε πίνακα για να μην μου μείνει πίνακας με άδειες θέσεις
            //        List<VatAnalysis> VatAnalysisList = new List<VatAnalysis>();
            //        while (oRSVat.EoF == false)
            //        {
            //            VatAnalysis obj = new VatAnalysis();
            //            obj.Name = (oRSVat.Fields.Item("Name").Value.ToString());
            //            obj.Percentage = Math.Round(decimal.Parse(oRSVat.Fields.Item("Percentage").Value.ToString()), 2);
            //            obj.VatAmount = Math.Round(decimal.Parse(oRSVat.Fields.Item("VatAmount").Value.ToString()), 2);
            //            obj.UnderlyingValue = Math.Round(decimal.Parse(oRSVat.Fields.Item("UnderlyingValue").Value.ToString()), 2);
            //            VatAnalysisList.Add(obj);
            //            oRSVat.MoveNext();
            //        }

            //        _oDocument.ImpactDocument.vatAnalysis = new VatAnalysis[VatAnalysisList.Count];
            //        int i = 0;
            //        foreach (VatAnalysis obj in VatAnalysisList)
            //        {
            //            _oDocument.ImpactDocument.vatAnalysis[i] = new VatAnalysis();
            //            _oDocument.ImpactDocument.vatAnalysis[i].Name = obj.Name;
            //            _oDocument.ImpactDocument.vatAnalysis[i].Percentage = obj.Percentage;
            //            _oDocument.ImpactDocument.vatAnalysis[i].VatAmount = obj.VatAmount;
            //            _oDocument.ImpactDocument.vatAnalysis[i].UnderlyingValue = obj.UnderlyingValue;
            //            i++;
            //        }
            //        iResult++;
            //        #endregion
            //        if (iResult == 2)
            //        {
            //            iRetVal++;
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        var a = new Logging("myDataMethods.LoadnCreateClass.GetIssuer", ex);
            //    }
            //    return iRetVal;
            //}

            private int LoadVatAnalysis(BoDocument _oDocument)
            {
                int iResult = 0;
                int iRetVal = 0;
                string sSQL = "";
                try
                {

                    #region vatanalysis
                    if (this.CompanyConnection.DbServerType == SAPbobsCOM.BoDataServerTypes.dst_HANADB)
                    {
                        sSQL = "SELECT * FROM TKA_V_ELECTRONIC_INVOICES_B2G_VATANALYSIS_IMPACT WHERE \"ObjType\"=" + _oDocument.ObjType + " and \"DocEntry\"=" + _oDocument.DocEntry;
                    }
                    else
                    {
                        sSQL = "SELECT * FROM TKA_V_ELECTRONIC_INVOICES_B2G_VATANALYSIS_IMPACT WHERE ObjType=" + _oDocument.ObjType + " and DocEntry=" + _oDocument.DocEntry;
                    }

                    SAPbobsCOM.Recordset oRSVat = CommonLibrary.Functions.Database.GetRecordSet(sSQL, this.CompanyConnection);


                    //TODO να διαβάζω πρώτα λίστ και να τα μεταφέρω μετά σε πίνακα για να μην μου μείνει πίνακας με άδειες θέσεις
                    List<VatAnalysis> VatAnalysisList = new List<VatAnalysis>();
                    while (oRSVat.EoF == false)
                    {
                        VatAnalysis obj = new VatAnalysis();
                        string vatCat = GetVatCategory(oRSVat.Fields.Item("vatCategory").Value.ToString());
                        if (vatCat.Equals("-"))
                        {
                            obj.Name = vatCat;
                            obj.Percentage = 0;
                        }
                        else
                        {
                            obj.Name = vatCat + ".00";
                            obj.Percentage = Math.Round(decimal.Parse(vatCat), 2);
                        }
                        obj.VatAmount = Math.Round(decimal.Parse(oRSVat.Fields.Item("VatAmount").Value.ToString()), 2);
                        obj.UnderlyingValue = Math.Round(decimal.Parse(oRSVat.Fields.Item("UnderlyingValue").Value.ToString()), 2);
                        VatAnalysisList.Add(obj);
                        oRSVat.MoveNext();
                    }

                    _oDocument.ImpactDocument.vatAnalysis = new VatAnalysis[VatAnalysisList.Count];
                    int i = 0;
                    foreach (VatAnalysis obj in VatAnalysisList)
                    {
                        _oDocument.ImpactDocument.vatAnalysis[i] = new VatAnalysis();
                        _oDocument.ImpactDocument.vatAnalysis[i].Name = obj.Name;
                        _oDocument.ImpactDocument.vatAnalysis[i].Percentage = obj.Percentage;
                        _oDocument.ImpactDocument.vatAnalysis[i].VatAmount = obj.VatAmount;
                        _oDocument.ImpactDocument.vatAnalysis[i].UnderlyingValue = obj.UnderlyingValue;
                        i++;
                    }
                    iResult++;
                    #endregion
                    if (iResult == 1)
                    {
                        iRetVal++;
                    }
                }
                catch (Exception ex)
                {
                    var a = new Logging("myDataMethods.LoadnCreateClass.LoadVatAnalysis", ex);
                }
                return iRetVal;
            }


            /// <summary>
            /// Δημιουργία Αντικειμένου για την Εταιρεία που ανεβάζει
            /// </summary>
            /// <param name="_iResult">1 For Success, 0 For Failure</param>
            /// <returns>Το Αντικείμενο της ΑΑΔΕ για την Εταιρεία που Ανεβάζει</returns>
            private Issuer GetIssuer(out int _iResult, BoDocument _oDocument)
            {
                _iResult = 0;
                string sSQL = "";
                Issuer oRet = null;
                try
                {

                    if (this.CompanyConnection.DbServerType == SAPbobsCOM.BoDataServerTypes.dst_HANADB)
                    {
                        sSQL = "SELECT * FROM TKA_V_ELECTRONIC_INVOICES_ISSUER_IMPACT_WRAPPER WHERE 1=1";
                    }
                    else
                    {
                        sSQL = "SELECT * FROM TKA_V_ELECTRONIC_INVOICES_ISSUER_IMPACT_WRAPPER WHERE 1=1";
                    }

                    SAPbobsCOM.Recordset oRS = CommonLibrary.Functions.Database.GetRecordSet(sSQL, this.CompanyConnection);

                    oRet = new Issuer();
                    #region B2G
                    oRet.RegisteredName = oRS.Fields.Item("RegisteredName").Value.ToString();
                    //oRet.RegisteredName = "ΦΑΡΜΑΣΕΡΒ - ΛΙΛΛΥ DEMO";
                    #endregion
                    oRet.ΒrandName = oRS.Fields.Item("ΒrandName").Value.ToString();
                    oRet.Vat = oRS.Fields.Item("Vat").Value.ToString();
                    oRet.taxOfficeCode = oRS.Fields.Item("TaxOffice").Value.ToString();
                    oRet.GeneralCommercialRegistryNumber = oRS.Fields.Item("GeneralCommercialRegistryNumber").Value.ToString();
                    oRet.RegistrationNumber = oRS.Fields.Item("RegistrationNumber").Value.ToString();
                    oRet.otherInfo = oRS.Fields.Item("otherInfo").Value.ToString();
                    oRet.Url = oRS.Fields.Item("Url").Value.ToString();
                    string activities = oRS.Fields.Item("activities").Value.ToString();
                    string phones = oRS.Fields.Item("phones").Value.ToString();
                    string faxes = oRS.Fields.Item("faxes").Value.ToString();
                    string emails = oRS.Fields.Item("emails").Value.ToString();

                    List<string> ListActivities = new List<string>();
                    if (!string.IsNullOrEmpty(activities))
                    {
                        ListActivities = activities.Split(';').ToList();
                        oRet.Activities = ListActivities.ToArray();
                    }

                    List<string> ListPhones = new List<string>();
                    if (!string.IsNullOrEmpty(phones))
                    {
                        ListPhones = phones.Split(';').ToList();
                        oRet.Phones = ListPhones.ToArray();
                    }

                    List<string> ListFaxes = new List<string>();
                    if (!string.IsNullOrEmpty(faxes))
                    {
                        ListFaxes = faxes.Split(';').ToList();
                        oRet.faxes = ListFaxes.ToArray();
                    }

                    List<string> ListEmails = new List<string>();
                    if (!string.IsNullOrEmpty(emails))
                    {
                        ListFaxes = emails.Split(';').ToList();
                        oRet.emails = ListEmails.ToArray();
                    }

                    oRet.Address = new Address();
                    oRet.Address.Street = oRS.Fields.Item("Street").Value.ToString();
                    oRet.Address.City = oRS.Fields.Item("City").Value.ToString();
                    oRet.Address.Postal = oRS.Fields.Item("Postal").Value.ToString();
                    oRet.Address.CountryCode = oRS.Fields.Item("CountryCode").Value.ToString();
                    oRet.Address.Country = oRS.Fields.Item("Country").Value.ToString();
                    oRet.Address.Number = oRS.Fields.Item("Number").Value.ToString();

                    _iResult++;
                }
                catch (Exception ex)
                {
                    var a = new Logging("myDataMethods.LoadnCreateClass.GetIssuer", ex);
                }
                return oRet;
            }


            /// <summary>
            /// Δημιουργία Αντικειμένου για την Εταιρεία που ανεβάζει
            /// </summary>
            /// <param name="_iResult">1 For Success, 0 For Failure</param>
            /// <returns>Το Αντικείμενο της ΑΑΔΕ για την Εταιρεία που Ανεβάζει</returns>
            private Distributiondetails GetDistributionDetails(BoDocument _oDocument, out int _iResult)
            {
                _iResult = 0;
                string sSQL = "";
                Distributiondetails oRet = null;
                try
                {
                    oRet = new Distributiondetails();
                    //todo
                    oRet.InternalDocumentId = _oDocument.mKey;
                    //oRet.RelativeDocuments = null;
                    oRet.deliveryDate = _oDocument.deliveryDate;
                    oRet.billOfLading = _oDocument.billOfLading;
                    oRet.movePurpose = _oDocument.movePurpose;
                    oRet.movePurposeCode = _oDocument.movePurposeCode;
                    oRet.shippingMethod = _oDocument.shippingMethod;
                    oRet.vehileNumber = _oDocument.vehileNumber;
                    oRet.totalQuantity = _oDocument.totalQuantity;
                    oRet.deliveryDestinationDetails = new deliveryDestinationDetails();
                    oRet.deliveryDestinationDetails.address = new deliveryDestinationAddress();
                    oRet.deliveryDestinationDetails.address.countryCode = _oDocument.deliveryCountryCode;
                    oRet.deliveryDestinationDetails.address.city = _oDocument.deliveryCity;
                    oRet.deliveryDestinationDetails.address.Street = _oDocument.deliveryStreet;
                    oRet.deliveryDestinationDetails.address.postal = _oDocument.deliveryPostal;
                    oRet.deliveryDestinationDetails.address.number = _oDocument.deliveryNumber;

                    oRet.deliveryOriginDetails = new deliveryOriginDetails();
                    oRet.deliveryOriginDetails.address = new deliveryDestinationAddress();
                    oRet.deliveryOriginDetails.address.countryCode = _oDocument.originCountryCode;
                    oRet.deliveryOriginDetails.address.city = _oDocument.originCity;
                    oRet.deliveryOriginDetails.address.Street = _oDocument.originStreet;
                    oRet.deliveryOriginDetails.address.postal = _oDocument.originPostal;
                    oRet.deliveryOriginDetails.address.number = _oDocument.originNumber;

                    _iResult++;
                }
                catch (Exception ex)
                {
                    var a = new Logging("myDataMethods.LoadnCreateClass.GetDistributionDetails", ex);
                }
                return oRet;
            }

            /// <summary>
            /// Δημιουργία Αντικειμένου για την Εταιρεία που ανεβάζει
            /// </summary>
            /// <param name="_iResult">1 For Success, 0 For Failure</param>
            /// <returns>Το Αντικείμενο της ΑΑΔΕ για την Εταιρεία που Ανεβάζει</returns>
            private Additionaldetails GetAdditionalDetails(BoDocument _oDocument, out int _iResult)
            {
                _iResult = 0;
                Additionaldetails oRet = null;
                try
                {
                    oRet = new Additionaldetails();
                    oRet.transmissionMethod = _oDocument.transmissionMethod;
                    if (_oDocument.avoidEmailGrouping.Equals("true"))
                    {
                        oRet.avoidEmailGrouping = true;
                    }
                    else
                    {
                        oRet.avoidEmailGrouping = false;
                    }
                    List<string> accountingDepartmentEmails = new List<string>();
                    if (!string.IsNullOrEmpty(_oDocument.accountingDepartmentEmails))
                    {
                        accountingDepartmentEmails = _oDocument.accountingDepartmentEmails.Split(';').ToList();
                        oRet.accountingDepartmentEmails = new string[accountingDepartmentEmails.Count()];
                        oRet.accountingDepartmentEmails = accountingDepartmentEmails.ToArray();
                    }
                    _iResult++;
                }
                catch (Exception ex)
                {
                    var a = new Logging("myDataMethods.LoadnCreateClass.GetAdditionalDetails", ex);
                }
                return oRet;
            }

            /// <summary>
            /// Δημιουργία Αντικειμένου για την Εταιρεία που ανεβάζει
            /// </summary>
            /// <param name="_iResult">1 For Success, 0 For Failure</param>
            /// <returns>Το Αντικείμενο της ΑΑΔΕ για την Εταιρεία που Ανεβάζει</returns>
            //private Deliverydestinationdetails GetDestinationDetails(BoDocument _oDocument, out int _iResult)
            //{
            //    _iResult = 0;
            //    Deliverydestinationdetails oRet = null;
            //    try
            //    {
            //        oRet = new Deliverydestinationdetails();
            //        oRet.remarks = _oDocument.DestinationRemarks;
            //        oRet.Address = new Address3();
            //        oRet.Address.City = _oDocument.DestinationCity;
            //        oRet.Address.Street = _oDocument.DestinationStreet;

            //        _iResult++;
            //    }
            //    catch (Exception ex)
            //    {
            //        var a = new Logging("myDataMethods.LoadnCreateClass.GetDestinationDetails", ex);
            //    }
            //    return oRet;
            //}


            /// <summary>
            /// Δημιουργία Αντικειμένου για την Εταιρεία που ανεβάζει
            /// </summary>
            /// <param name="_iResult">1 For Success, 0 For Failure</param>
            /// <returns>Το Αντικείμενο της ΑΑΔΕ για την Εταιρεία που Ανεβάζει</returns>
            //private Deliveryorigindetails GetOriginDetails(BoDocument _oDocument, out int _iResult)
            //{
            //    _iResult = 0;
            //    Deliveryorigindetails oRet = null;
            //    try
            //    {
            //        oRet = new Deliveryorigindetails();
            //        oRet.movePurposeCode = "ΠΩΛΗΣΗ";
            //        oRet.Address = new Address2();
            //        oRet.Address.City = "testCity";
            //        oRet.Address.Street = "testStreet";
            //        oRet.Phones = new string[1];
            //        oRet.Phones[0] = "2310521010";

            //        _iResult++;
            //    }
            //    catch (Exception ex)
            //    {
            //        var a = new Logging("myDataMethods.LoadnCreateClass.GetDestinationDetails", ex);
            //    }
            //    return oRet;
            //}
            /// <summary>
            /// Δημιουργία Αντικειμένου για τον Συν/μένο
            /// </summary>
            /// <param name="_iResult">1 For Success, 0 For Failure</param>
            /// <param name="_oDocument">To Αντικείμενο του Παραστατικού</param>
            /// <returns>Το Αντικείμενο της ΑΑΔΕ για τον Συν/μένο</returns>
            private Counterparty GetCounterPart(BoDocument _oDocument, out int _iResult, string _oInvoiceType)
            {
                Counterparty oRet = new Counterparty();
                _iResult = 0;
                try
                {
                    oRet.Code = _oDocument.CounterPart_code;
                    oRet.registeredName = _oDocument.CounterPart_name;
                    oRet.Vat = _oDocument.CounterPart_vatNumber;
                    oRet.TaxOffice = _oDocument.CounterPart_taxOffice;

                    #region test b2g
                    //oRet.Code = "3000008080";
                    //oRet.registeredName ="ΔΗΜΟΣ ΑΘΗΝΑΙΩΝ";
                    //oRet.Vat = "090025537";
                    //oRet.TaxOffice = "Δ.ΑΘΗΝΩΝ";
                    #endregion

                    List<string> ListActivities = new List<string>();
                    if (!string.IsNullOrEmpty(_oDocument.CounterPart_activities))
                    {
                        ListActivities = _oDocument.CounterPart_activities.Split(';').ToList();
                        oRet.Activities = ListActivities.ToArray();
                    }

                    List<string> ListPhones = new List<string>();
                    if (!string.IsNullOrEmpty(_oDocument.CounterPart_phones))
                    {
                        ListPhones = _oDocument.CounterPart_phones.Split(';').ToList();
                        oRet.Phones = ListPhones.ToArray();
                    }

                    List<string> ListFaxes = new List<string>();
                    if (!string.IsNullOrEmpty(_oDocument.CounterPart_faxes))
                    {
                        ListFaxes = _oDocument.CounterPart_faxes.Split(';').ToList();
                        oRet.faxes = ListFaxes.ToArray();
                    }
                    oRet.address = new Address1();
                    oRet.address.Street = _oDocument.CounterPart_address_street;
                    oRet.address.city = _oDocument.CounterPart_address_city;
                    oRet.address.postal = _oDocument.CounterPart_address_postalCode;
                    oRet.address.countryCode = _oDocument.CounterPart_country;
                    oRet.address.number = _oDocument.CounterPart_address_number;


                    #region old code where i needed to filter the results
                    //switch (_oDocument.CounterPart_Define_Area)
                    //{
                    //    case "GR":
                    //        CommonLibrary.Ini.IniFile ini = new CommonLibrary.Ini.IniFile("C:\\Program Files\\sap\\ImpactElectronicInvoicingDA\\ConfParams.ini");

                    //        string sNoAddress = ini.IniReadValue("Default", "GR_COUNTERPART_WITHOUT_ADDRESS");
                    //        List<string> ListNoAddress = new List<string>();
                    //        ListNoAddress = sNoAddress.Split(',').ToList();

                    //        string sNoName = ini.IniReadValue("Default", "GR_COUNTERPART_WITHOUT_NAME");
                    //        List<string> ListNoName = new List<string>();
                    //        ListNoName = sNoName.Split(',').ToList();

                    //        if (ListNoAddress.Contains(_oInvoiceType.ToString()) == true)
                    //        {
                    //            oRet.vatNumber = _oDocument.CounterPart_vatNumber;
                    //            oRet.country = (CountryType)Enum.Parse(typeof(CountryType), _oDocument.CounterPart_country);
                    //            oRet.branch = int.Parse(_oDocument.CounterPart_branch);
                    //        }
                    //        else
                    //        {
                    //            if (ListNoName.Contains(_oInvoiceType.ToString()) == false)
                    //            {
                    //                //if (_oInvoiceType != InvoiceType.Item71)
                    //                //{
                    //                oRet.name = _oDocument.CounterPart_name;
                    //            }
                    //            oRet.country = (CountryType)Enum.Parse(typeof(CountryType), _oDocument.CounterPart_country);
                    //            oRet.vatNumber = _oDocument.CounterPart_vatNumber;
                    //            oRet.branch = int.Parse(_oDocument.CounterPart_branch);
                    //            oRet.address = new AddressType();
                    //            oRet.address.city = _oDocument.CounterPart_country;
                    //            oRet.address.street = _oDocument.CounterPart_address_street;
                    //            oRet.address.postalCode = _oDocument.CounterPart_address_postalCode;
                    //        }
                    //        break;
                    //    case "EU":
                    //        oRet.name = _oDocument.CounterPart_name;
                    //        oRet.country = (CountryType)Enum.Parse(typeof(CountryType), _oDocument.CounterPart_country);
                    //        oRet.vatNumber = _oDocument.CounterPart_vatNumber;
                    //        oRet.branch = int.Parse(_oDocument.CounterPart_branch);
                    //        oRet.address = new AddressType();
                    //        oRet.address.city = _oDocument.CounterPart_country;
                    //        oRet.address.street = _oDocument.CounterPart_address_street;
                    //        oRet.address.postalCode = _oDocument.CounterPart_address_postalCode;
                    //        break;
                    //    case "TX":
                    //        oRet.name = _oDocument.CounterPart_name;
                    //        oRet.country = (CountryType)Enum.Parse(typeof(CountryType), _oDocument.CounterPart_country);
                    //        oRet.vatNumber = _oDocument.CounterPart_vatNumber;
                    //        oRet.branch = int.Parse(_oDocument.CounterPart_branch);
                    //        oRet.address = new AddressType();
                    //        oRet.address.city = _oDocument.CounterPart_address_city;
                    //        oRet.address.street = _oDocument.CounterPart_address_street;
                    //        oRet.address.postalCode = _oDocument.CounterPart_address_postalCode;
                    //        break;
                    //}
                    #endregion
                    _iResult++;
                }
                catch (Exception ex)
                {
                    var a = new Logging("myDataMethods.LoadnCreateClass.GetCounterPart", ex);
                }
                return oRet;
            }

            /// <summary>
            /// Δημιουργία Αντικειμένου ΑΑΔΕ για τους Όρους Πληρωμής
            /// </summary>
            /// <param name="_oDocument">To Αντικείμενο του Παραστατικού</param>
            /// <param name="_iResult">1 For Success, 0 For Failure</param>
            /// <returns>Το Αντικείμενο της ΑΑΔΕ για τους Όρους Πληρωμής</returns>
            private Paymentdetails GetPaymentMethods(BoDocument _oDocument, out int _iResult)
            {
                _iResult = 0;
                string sSQL = "";
                Paymentdetails oRet = new Paymentdetails();
                List<Paymentmethod> paymentMethods = new List<Paymentmethod>();
                try
                {
                    if (this.CompanyConnection.DbServerType == SAPbobsCOM.BoDataServerTypes.dst_HANADB)
                    {
                        sSQL = "SELECT * FROM TKA_V_ELECTRONIC_INVOICES_PAYMENT_TOTALS_IMPACT_WRAPPER WHERE 1=1 AND \"ObjType\" = '" + _oDocument.ObjType + "' AND \"DocEntry\" = '" + _oDocument.DocEntry + "'";
                    }
                    else
                    {
                        sSQL = "SELECT * FROM TKA_V_ELECTRONIC_INVOICES_PAYMENT_TOTALS_IMPACT_WRAPPER WHERE 1=1 AND ObjType = '" + _oDocument.ObjType + "' AND DocEntry = '" + _oDocument.DocEntry + "'";
                    }

                    SAPbobsCOM.Recordset oRS = CommonLibrary.Functions.Database.GetRecordSet(sSQL, this.CompanyConnection);
                    oRet.previousBalance = Math.Round(decimal.Parse(oRS.Fields.Item("previousBalance").Value.ToString()), 2);
                    oRet.newBalance = Math.Round(decimal.Parse(oRS.Fields.Item("newBalance").Value.ToString()), 2);
                    oRet.electronicPaymentCode = oRS.Fields.Item("electronicPaymentCode").Value.ToString();
                    oRet.otherPaymentDetails = oRS.Fields.Item("otherPaymentDetails").Value.ToString();
                    oRet.paymentReferenceID = oRS.Fields.Item("paymentReferenceID").Value.ToString();

                    while (oRS.EoF == false)
                    {
                        Paymentmethod oPayment = null;
                        oPayment = new Paymentmethod();
                        oPayment.amount = Math.Round(decimal.Parse(oRS.Fields.Item("amount").Value.ToString()), 2);
                        oPayment.remarks = oRS.Fields.Item("remarks").Value.ToString();
                        oPayment.paymentDate = oRS.Fields.Item("paymentDate").Value.ToString();
                        oPayment.paymentMethodTypeCode = int.Parse(oRS.Fields.Item("paymentMethodTypeCode").Value.ToString());
                        oPayment.paymentMethodType = oRS.Fields.Item("paymentMethodType").Value.ToString();

                        paymentMethods.Add(oPayment);

                        oRS.MoveNext();
                    }
                    oRet.paymentMethods = paymentMethods.ToArray();
                    _iResult++;
                }
                catch (Exception ex)
                {
                    Logging.WriteToLog("sSQL=" + sSQL, Logging.LogStatus.RET_VAL);
                    var a = new Logging("myDataMethods.LoadnCreateClass.GetPaymentMethods", ex);
                }
                return oRet;
            }

            /// <summary>
            /// Δημιουργία Header Δεδομένων ΑΑΔΕ Παραστατικού
            /// </summary>
            /// <param name="_oDocument">To Business Object</param>
            /// <param name="_iResult">1 For Success, 0 For Failure</param>
            /// <returns>Τον Header του Παραστατικού</returns>
            private int GetInvoiceHeader(ref BoDocument _oDocument)
            {
                int _iResult = 0;
                //InvoiceHeaderType oRet = new InvoiceHeaderType();
                string sSQL = "";
                try
                {
                    DateTime dtRefDate = DateTime.Now;

                    string sFileLocation = "C:\\Program Files\\sap\\ImpactElectronicInvoicingDA\\ConfParams.ini";
                    CommonLibrary.Ini.IniFile ini = new CommonLibrary.Ini.IniFile(sFileLocation);

                    //ΠΡΕΠΕΙ ΝΑ ΣΚΕΦΤΩ ΚΑΤΙ ΔΗΜΙΟΥΡΓΙΚΟ ΓΙΑ ΝΑ ΣΥΝΔΕΟΜΑΙ ΣΕ ΔΙΑΦΟΡΕΤΙΚΗ ΒΑΣΗ ΑΝΑΛΟΓΑ ΜΕ ΤΟ DBNAME (HANA EDITION)
                    string sConnectionString = ini.IniReadValue("Default", "MSSQLConnectionString");
                    sConnectionString = sConnectionString.Replace("#DB_NAME", _oDocument.CompanyDB);

                    if (this.CompanyConnection.DbServerType == SAPbobsCOM.BoDataServerTypes.dst_HANADB)
                    {
                        sSQL = "SELECT * FROM TKA_V_ELECTRONIC_INVOICES_HEADER_IMPACT_WRAPPER WHERE 1=1 AND \"ObjType\" = '" + _oDocument.ObjType + "' AND \"DocEntry\" = '" + _oDocument.DocEntry + "'";
                    }
                    else
                    {
                        sSQL = "SELECT * FROM TKA_V_ELECTRONIC_INVOICES_HEADER_IMPACT_WRAPPER WHERE 1=1 AND ObjType = '" + _oDocument.ObjType + "' AND DocEntry = '" + _oDocument.DocEntry + "'";
                    }

                    SAPbobsCOM.Recordset oRS = CommonLibrary.Functions.Database.GetRecordSet(sSQL, this.CompanyConnection);

                    while (oRS.EoF == false)
                    {
                        #region CounterPart Data
                        _oDocument.CounterPart_address_city = oRS.Fields.Item("CounterPart_address_city").Value.ToString();
                        _oDocument.CounterPart_address_postalCode = oRS.Fields.Item("CounterPart_address_postalCode").Value.ToString();
                        _oDocument.CounterPart_address_street = oRS.Fields.Item("CounterPart_address_street").Value.ToString();
                        _oDocument.CounterPart_branch = oRS.Fields.Item("CounterPart_branch").Value.ToString();
                        _oDocument.CounterPart_country = oRS.Fields.Item("CounterPart_country").Value.ToString();
                        _oDocument.CounterPart_address_number = oRS.Fields.Item("CounterPart_address_number").Value.ToString();
                        _oDocument.CounterPart_name = oRS.Fields.Item("CounterPart_name").Value.ToString();
                        _oDocument.CounterPart_code = oRS.Fields.Item("CounterPart_code").Value.ToString();
                        _oDocument.CounterPart_taxOffice = oRS.Fields.Item("CounterPart_taxOffice").Value.ToString();
                        _oDocument.CounterPart_vatNumber = oRS.Fields.Item("CounterPart_vatNumber").Value.ToString();
                        _oDocument.CounterPart_Define_Area = oRS.Fields.Item("CounterPart_Define_Area").Value.ToString();
                        _oDocument.CounterPart_activities = oRS.Fields.Item("CounterPart_activities").Value.ToString();
                        _oDocument.CounterPart_phones = oRS.Fields.Item("CounterPart_phones").Value.ToString();
                        _oDocument.CounterPart_LicTradNum = oRS.Fields.Item("LicTradNum").Value.ToString();

                        _oDocument.CounterPart_faxes = oRS.Fields.Item("CounterPart_faxes").Value.ToString();
                        _oDocument.DestinationCity = oRS.Fields.Item("DestinationCity").Value.ToString();
                        _oDocument.DestinationStreet = oRS.Fields.Item("DestinationStreet").Value.ToString();
                        _oDocument.DestinationRemarks = oRS.Fields.Item("DestinationRemarks").Value.ToString();



                        #endregion

                        #region Required
                        _oDocument.ImpactDocument.number = oRS.Fields.Item("aa").Value.ToString();
                        _oDocument.ImpactDocument.series = oRS.Fields.Item("series").Value.ToString();
                        _oDocument.ImpactDocument.dateIssued = DateTime.Parse(oRS.Fields.Item("issueDate").Value.ToString());
                        string invoiceType = oRS.Fields.Item("invoiceType").Value.ToString();
                        _oDocument.ImpactDocument.invoiceTypeCode = invoiceType;
                        string dDate = oRS.Fields.Item("dispatchDate").Value.ToString();
                        if (!string.IsNullOrEmpty(dDate) && !dDate.Equals("-112"))
                        {
                            _oDocument.dispatchDate = DateTime.Parse(oRS.Fields.Item("dispatchDate").Value.ToString());
                        }
                        _oDocument.deliveryDate = DateTime.Parse(oRS.Fields.Item("deliveryDate").Value.ToString());
                        string isDeliveryNote = oRS.Fields.Item("isDeliveryNote").Value.ToString();
                        if (isDeliveryNote.Equals("true"))
                        {
                            _oDocument.ImpactDocument.isDeliveryNote = true;
                        }
                        else
                        {
                            _oDocument.ImpactDocument.isDeliveryNote = false;
                        }


                        #endregion


                        #region Distribution Details
                        _oDocument.ImpactDocument.currencyCode = oRS.Fields.Item("currency").Value.ToString();
                        _oDocument.ImpactDocument.category = oRS.Fields.Item("category").Value.ToString();
                        _oDocument.movePurpose = oRS.Fields.Item("movePurpose").Value.ToString();
                        _oDocument.movePurposeCode = int.Parse(oRS.Fields.Item("movePurposeCode").Value.ToString());
                        _oDocument.shippingMethod = oRS.Fields.Item("shippingMethod").Value.ToString();
                        _oDocument.vehileNumber = oRS.Fields.Item("vehileNumber").Value.ToString();
                        _oDocument.billOfLading = oRS.Fields.Item("billOfLading").Value.ToString();
                        _oDocument.totalQuantity = double.Parse(oRS.Fields.Item("totalQuantity").Value.ToString());
                        _oDocument.deliveryCountryCode = oRS.Fields.Item("deliveryCountryCode").Value.ToString();
                        _oDocument.deliveryCity = oRS.Fields.Item("deliveryCity").Value.ToString();
                        _oDocument.deliveryStreet = oRS.Fields.Item("deliveryStreet").Value.ToString();
                        _oDocument.deliveryPostal = oRS.Fields.Item("deliveryPostal").Value.ToString();
                        _oDocument.deliveryNumber = oRS.Fields.Item("deliveryNumber").Value.ToString();
                        _oDocument.originCountryCode = oRS.Fields.Item("originCountryCode").Value.ToString();
                        _oDocument.originCity = oRS.Fields.Item("originCity").Value.ToString();
                        _oDocument.originStreet = oRS.Fields.Item("originStreet").Value.ToString();
                        _oDocument.originPostal = oRS.Fields.Item("originPostal").Value.ToString();
                        _oDocument.originNumber = oRS.Fields.Item("originNumber").Value.ToString();

                        #endregion

                        if (_oDocument.ImpactDocument.dateIssued == DateTime.Today)
                        {
                            _oDocument.ImpactDocument.isDelayedCode = 0;
                        }
                        else
                        {
                            if (_oDocument.ErrorCode == 500 || _oDocument.ErrorCode == 409)
                            {
                                _oDocument.ImpactDocument.isDelayedCode = 2;
                            }
                            else
                            {
                                _oDocument.ImpactDocument.isDelayedCode = 1;
                            }
                        }

                        _oDocument.ImpactDocument.orderNumber = oRS.Fields.Item("orderNumber").Value.ToString();
                        _oDocument.ImpactDocument.remarks = oRS.Fields.Item("remarks").Value.ToString();
                        if (!string.IsNullOrEmpty(oRS.Fields.Item("invoiceVariationType").Value.ToString()) && !oRS.Fields.Item("invoiceVariationType").Value.ToString().Equals("-112"))
                        {
                            _oDocument.ImpactDocument.variationType = int.Parse(oRS.Fields.Item("invoiceVariationType").Value.ToString());
                        }
                        _oDocument.ImpactDocument.invoiceType = oRS.Fields.Item("invoiceTypeDscr").Value.ToString();
                        if (_oDocument.B2G.Equals("Y"))
                        {
                            _oDocument.ImpactDocument.documentTypeCode = "ELB2G";
                        }
                        else
                        {
                            _oDocument.ImpactDocument.documentTypeCode = oRS.Fields.Item("documentTypeCode").Value.ToString();

                        }
                        _oDocument.ImpactDocument.specialInvoiceCategory = int.Parse(oRS.Fields.Item("specialInvoiceCategory").Value.ToString());

                        string relativeDocumentsStr = oRS.Fields.Item("relativeDocuments").Value.ToString();
                        List<string> relativeDocuments = new List<string>();
                        if (!string.IsNullOrEmpty(relativeDocumentsStr))
                        {
                            relativeDocuments = relativeDocumentsStr.Split(';').ToList();
                            _oDocument.ImpactDocument.RelativeDocuments = new string[relativeDocuments.Count()];
                            _oDocument.ImpactDocument.RelativeDocuments = relativeDocuments.ToArray();
                        }

                        #region Branch
                        _oDocument.ImpactDocument.branchCode = oRS.Fields.Item("branchCode").Value.ToString();
                        _oDocument.ImpactDocument.branchId = oRS.Fields.Item("branchId").Value.ToString();
                        _oDocument.ImpactDocument.branchAddress = new branchAddress();
                        _oDocument.ImpactDocument.branchAddress.city = oRS.Fields.Item("branchCity").Value.ToString();
                        _oDocument.ImpactDocument.branchAddress.street = oRS.Fields.Item("branchStreet").Value.ToString();
                        _oDocument.ImpactDocument.branchAddress.postal = oRS.Fields.Item("branchPostal").Value.ToString();
                        _oDocument.ImpactDocument.branchAddress.countryCode = oRS.Fields.Item("branchCountryCode").Value.ToString();

                        string branchPhonesStr = oRS.Fields.Item("branchPhones").Value.ToString();
                        List<string> branchPhones = new List<string>();
                        if (!string.IsNullOrEmpty(branchPhonesStr))
                        {
                            relativeDocuments = branchPhonesStr.Split(';').ToList();
                            _oDocument.ImpactDocument.branchPhones = new string[branchPhones.Count()];
                            _oDocument.ImpactDocument.branchPhones = branchPhones.ToArray();
                        }
                        string branchFaxesStr = oRS.Fields.Item("branchFaxes").Value.ToString();
                        List<string> branchFaxes = new List<string>();
                        if (!string.IsNullOrEmpty(branchFaxesStr))
                        {
                            relativeDocuments = branchFaxesStr.Split(';').ToList();
                            _oDocument.ImpactDocument.branchFaxes = new string[branchFaxes.Count()];
                            _oDocument.ImpactDocument.branchFaxes = branchFaxes.ToArray();
                        }
                        #endregion

                        #region Additional Details
                        _oDocument.accountingDepartmentEmails = oRS.Fields.Item("accountingDepartmentEmails").Value.ToString();
                        _oDocument.transmissionMethod = oRS.Fields.Item("transmissionMethod").Value.ToString();
                        _oDocument.avoidEmailGrouping = oRS.Fields.Item("avoidEmailGrouping").Value.ToString();

                        #endregion


                        #region MiscellaneousData
                        _oDocument.ImpactDocument.MiscellaneousData = new Miscellaneousdata();
                        _oDocument.ImpactDocument.MiscellaneousData.MoreInformation1 = oRS.Fields.Item("MoreInformation1").Value.ToString();
                        _oDocument.ImpactDocument.MiscellaneousData.MoreInformation2 = oRS.Fields.Item("MoreInformation2").Value.ToString();
                        _oDocument.ImpactDocument.MiscellaneousData.MoreInformation3 = oRS.Fields.Item("MoreInformation3").Value.ToString();

                        #endregion


                        #region banks
                        _oDocument.ImpactDocument.banks = new banks[5];
                        _oDocument.ImpactDocument.banks[0] = new banks();
                        _oDocument.ImpactDocument.banks[0].name = oRS.Fields.Item("bank1").Value.ToString();
                        _oDocument.ImpactDocument.banks[0].iban = oRS.Fields.Item("iban1").Value.ToString();
                        _oDocument.ImpactDocument.banks[1] = new banks();
                        _oDocument.ImpactDocument.banks[1].name = oRS.Fields.Item("bank2").Value.ToString();
                        _oDocument.ImpactDocument.banks[1].iban = oRS.Fields.Item("iban2").Value.ToString();
                        _oDocument.ImpactDocument.banks[2] = new banks();
                        _oDocument.ImpactDocument.banks[2].name = oRS.Fields.Item("bank3").Value.ToString();
                        _oDocument.ImpactDocument.banks[2].iban = oRS.Fields.Item("iban3").Value.ToString();
                        _oDocument.ImpactDocument.banks[3] = new banks();
                        _oDocument.ImpactDocument.banks[3].name = oRS.Fields.Item("bank4").Value.ToString();
                        _oDocument.ImpactDocument.banks[3].iban = oRS.Fields.Item("iban4").Value.ToString();
                        _oDocument.ImpactDocument.banks[4] = new banks();
                        _oDocument.ImpactDocument.banks[4].name = oRS.Fields.Item("bank5").Value.ToString();
                        _oDocument.ImpactDocument.banks[4].iban = oRS.Fields.Item("iban5").Value.ToString();

                        #endregion

                        #region NotRequired


                        //if (string.IsNullOrEmpty(oRS.Fields.Item("currency").Value.ToString()) || oRS.Fields.Item("currency").Value.ToString().Equals("-112"))
                        //{
                        //    //oRet.currencySpecified = false;
                        //}
                        //else
                        //{
                        //    CurrencyType enCur = (CurrencyType)Enum.Parse(typeof(CurrencyType), oRS.Fields.Item("currency").Value.ToString());
                        //    oRet.currencySpecified = true;
                        //    oRet.currency = enCur;
                        //}

                        //if (string.IsNullOrEmpty(oRS.Fields.Item("vatPaymentSuspension").Value.ToString()) || oRS.Fields.Item("vatPaymentSuspension").Value.ToString().Equals("-112"))
                        //{
                        //    oRet.vatPaymentSuspensionSpecified = false;
                        //}
                        //else
                        //{
                        //    oRet.vatPaymentSuspensionSpecified = true;
                        //    oRet.vatPaymentSuspension = oRS.Fields.Item("vatPaymentSuspension").Value.ToString();
                        //}

                        //if (string.IsNullOrEmpty(oRS.Fields.Item("exchangeRate").Value.ToString()) || decimal.Parse(oRS.Fields.Item("exchangeRate").Value.ToString()) == -775)
                        //{
                        //    oRet.exchangeRateSpecified = false;
                        //}
                        //else
                        //{
                        //    oRet.exchangeRateSpecified = true;
                        //    //oRet.exchangeRate = decimal.Parse(dtRow["exchangeRate"].ToString());
                        //    oRet.exchangeRate = Math.Round((decimal.Parse(oRS.Fields.Item("exchangeRate").Value.ToString())), 2);
                        //}
                        //if (string.IsNullOrEmpty(oRS.Fields.Item("selfPricing").Value.ToString()) || oRS.Fields.Item("selfPricing").Value.ToString().Equals("-112"))
                        //{
                        //    oRet.selfPricingSpecified = false;
                        //}
                        //else
                        //{
                        //    oRet.selfPricingSpecified = true;
                        //    oRet.selfPricing = oRS.Fields.Item("selfPricing").Value.ToString() == "0" ? false : true;
                        //}
                        //if (string.IsNullOrEmpty(oRS.Fields.Item("dispatchDate").Value.ToString()) || oRS.Fields.Item("dispatchDate").Value.ToString().Equals("-112"))
                        //{
                        //    oRet.dispatchDateSpecified = false;
                        //}
                        //else
                        //{
                        //    oRet.dispatchDateSpecified = true;
                        //    oRet.dispatchDate = DateTime.Parse(oRS.Fields.Item("dispatchDate").Value.ToString());
                        //}
                        //if (string.IsNullOrEmpty(oRS.Fields.Item("dispatchTime").Value.ToString()) || oRS.Fields.Item("dispatchTime").Value.ToString().Equals("-112"))
                        //{
                        //    oRet.dispatchTimeSpecified = false;
                        //}
                        //else
                        //{
                        //    oRet.dispatchTimeSpecified = true;
                        //    oRet.dispatchTime = DateTime.Parse(oRS.Fields.Item("dispatchTime").Value.ToString());
                        //}
                        //if (!string.IsNullOrEmpty(oRS.Fields.Item("vehicleNumber").Value.ToString()) || oRS.Fields.Item("vehicleNumber").Value.ToString().Equals("-112"))
                        //{
                        //    oRet.vehicleNumber = oRS.Fields.Item("vehicleNumber").Value.ToString();
                        //}

                        //string sNoMovePurpose = ini.IniReadValue("Default", "NO_MOVE_PURPOSE");
                        //List<string> ListNoMovePurpose = new List<string>();
                        //ListNoMovePurpose = sNoMovePurpose.Split(',').ToList();

                        ////if (ListNoMovePurpose.Contains(_oInvoiceType.ToString()) == false)
                        ////{
                        //if (ListNoMovePurpose.Contains(oRet.invoiceType.ToString()) == true || string.IsNullOrEmpty(oRS.Fields.Item("movePurpose").Value.ToString()) || int.Parse(oRS.Fields.Item("movePurpose").Value.ToString()) == -112)
                        //{
                        //    oRet.movePurposeSpecified = false;
                        //}
                        //else
                        //{
                        //    oRet.movePurposeSpecified = true;
                        //    oRet.movePurpose = int.Parse(oRS.Fields.Item("movePurpose").Value.ToString());
                        //}

                        //if (!string.IsNullOrEmpty(oRS.Fields.Item("invoiceVariationType").Value.ToString()) && int.Parse(oRS.Fields.Item("invoiceVariationType").Value.ToString()) != -112)
                        //{
                        //    oRet.invoiceVariationType = int.Parse(oRS.Fields.Item("invoiceVariationType").Value.ToString());
                        //    oRet.invoiceVariationTypeSpecified = true;
                        //}
                        //else
                        //{
                        //    oRet.invoiceVariationTypeSpecified = false;
                        //}
                        //}
                        #endregion
                        //TODO
                        //List<long> correlatedInvoicesField;
                        oRS.MoveNext();
                    }

                    _iResult++;
                }
                catch (Exception ex)
                {
                    Logging.WriteToLog("sSQL=" + sSQL, Logging.LogStatus.RET_VAL);
                    var a = new Logging("myDataMethods.LoadnCreateClass.GetInvoiceHeader", ex);
                }
                return _iResult;
            }

            private void SetIgnoreDue2Error(BoDocument _oDocument)
            {
                try
                {
                    DAL.BoUpdateDB oLog = new DAL.BoUpdateDB();
                    oLog.DocumentAA = _oDocument.DocumentAA;
                    oLog.Company = _oDocument.CompanyDB;
                    oLog.ObjType = _oDocument.ObjType;
                    oLog.DocEntry = _oDocument.DocEntry;
                    oLog.DocNum = _oDocument.DocNum;
                    //oLog.isExpense = _oDocument.isExpense;
                    int iResult = oLog.UpdateDocumentSETIgnore(CompanyConnection);
                }
                catch (Exception ex)
                {
                    var a = new Logging("myDataMethods.SetIgnoreDue2Error", ex);
                }
            }
            #endregion

            #region Public Methods
            public int Exec(Enumerators.ot_Object _enType)
            {
                int iRetVal = 0;
                try
                {
                    int iSuccess = 2;
                    int iResult = 0;

                    iResult += this.LoadDocumentsProcess();
                    if (iResult == 1)
                    {
                        iResult += this.PrepareDocumentsProcess();
                    }

                    if (iResult == iSuccess)
                    {
                        iRetVal++;
                    }
                }
                catch (Exception ex)
                {
                    var a = new Logging("myDataMethods.LoadnCreateClass.Exec", ex);
                }
                return iRetVal;
            }
            #endregion
        }


        //internal class LoadnCreateClassCancel
        //{
        //    public List<BoDocument> ListDocumentsCancel { get; set; }
        //    public SAPbobsCOM.Company CompanyConnectionCancel { get; set; }

        //    public int returnsRows { get; set; }

        //    public LoadnCreateClassCancel()
        //    {
        //        this.ListDocumentsCancel = new List<BoDocument>();
        //    }

        //    #region Public Methods
        //    public int Exec(Enumerators.ot_Object _enType)
        //    {
        //        int iRetVal = 0;
        //        try
        //        {
        //            int iSuccess = 1;
        //            int iResult = 0;

        //            //Logging.WriteToLog("myDataMethods.LoadnCreateClass.LoadDocumentsProcess", Logging.LogStatus.START);
        //            iResult += this.LoadDocumentsCancelProcess();
        //            //Logging.WriteToLog("myDataMethods.LoadnCreateClass.LoadDocumentsProcess", Logging.LogStatus.END);

        //            if (iResult == iSuccess)
        //            {
        //                iRetVal++;
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            var a = new Logging("myDataMethods.LoadnCreateClassCancel.Exec", ex);
        //        }
        //        return iRetVal;
        //    }
        //    #endregion

        //    #region Private Methods
        //    private int LoadDocumentsCancelProcess()
        //    {
        //        string sSQL = "";
        //        int iRetVal = 0;
        //        try
        //        {
        //            this.ListDocumentsCancel = new List<BoDocument>();
        //            BoDocument oDocument = null;

        //            if (CompanyConnectionCancel.DbServerType == SAPbobsCOM.BoDataServerTypes.dst_HANADB)
        //            {
        //                sSQL = "SELECT * FROM TKA_V_SELECT_DOCUMENTS_2_CANCEL WHERE 1=1 ORDER BY AA DESC";
        //            }
        //            else
        //            {
        //                sSQL = "SELECT * FROM TKA_V_SELECT_DOCUMENTS_2_CANCEL WHERE 1=1 ORDER BY AA DESC";
        //            }

        //            SAPbobsCOM.Recordset oRS = CommonLibrary.Functions.Database.GetRecordSet(sSQL, this.CompanyConnectionCancel);

        //            while (oRS.EoF == false)
        //            {
        //                this.returnsRows = oRS.RecordCount;
        //                oDocument = new BoDocument();
        //                oDocument.DocumentAA = oRS.Fields.Item("AA").Value.ToString();
        //                oDocument.CompanyDB = oRS.Fields.Item("COMPANY_DB").Value.ToString();
        //                oDocument.ObjType = oRS.Fields.Item("OBJTYPE").Value.ToString();
        //                oDocument.DocEntry = oRS.Fields.Item("DOCENTRY").Value.ToString();
        //                oDocument.DocNum = oRS.Fields.Item("DOCNUM").Value.ToString();
        //                oDocument.MARK = oRS.Fields.Item("MARK").Value.ToString();
        //                oDocument.isExpense = int.Parse(oRS.Fields.Item("ISEXPENSE").Value.ToString());
        //                if (oDocument.isExpense == 1)
        //                {
        //                    oDocument.DocumentType = Enumerators.DocumentType.p_EU_TX;
        //                }
        //                else
        //                {
        //                    oDocument.DocumentType = Enumerators.DocumentType.p_Income;
        //                }

        //                this.ListDocumentsCancel.Add(oDocument);
        //                oRS.MoveNext();
        //                //iResult+=this.LoadDocuments()
        //            }

        //            iRetVal++;
        //        }
        //        catch (Exception ex)
        //        {
        //            Logging.WriteToLog("_sSQL=" + sSQL, Logging.LogStatus.RET_VAL);
        //            var a = new Logging("myDataMethods.LoadnCreateClassCancel.LoadDocumentsCancelProcess", ex);
        //        }
        //        return iRetVal;
        //    }
        //    #endregion

        //}

        #endregion
    }
}
