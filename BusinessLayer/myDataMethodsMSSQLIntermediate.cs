using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using CommonLibrary.ExceptionHandling;
using RestSharp;
using System.Web.Script.Serialization;
using Newtonsoft.Json;
using System.IO;
using Response;
using System.Text.RegularExpressions;
using System.Web;
using SAPmyDataService.Enumerators;
using System.Data.SqlClient;
using System.Data;

namespace SAPmyDataService.BusinessLayer
{
    public class myDataMethods
    {
        #region Public Properties
        public List<BoDocument> ListDocuments { get; set; }
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
                for (int i = 0; i < this.ListDocuments.Count; i++)
                {
                    if (ListDocuments[i].DocumentStatus == DocumentPrepared.p_Success)
                    {
                        this.Send2AADE(this.ListDocuments[i]);
                    }
                }
            }
            catch (Exception ex)
            {
                var a = new Logging("myDataMethods.Send", ex);
            }
            return iRetVal;
        }
        public int CancelInvoice()
        {
            int iRetVal = 0;
            try
            {
                for (int i = 0; i < this.ListDocuments.Count; i++)
                {
                    this.Cancel(this.ListDocuments[i]);
                }
            }
            catch (Exception ex)
            {
                var a = new Logging("myDataMethods.CancelInvoice", ex);
            }
            return iRetVal;
        }
        #endregion

        #region Private Methods
        private int Send2AADE(BoDocument _oDocument)
        {
            int iRetVal = 0;
            try
            {
                string sFileLocation = "C:\\Program Files\\sap\\SAPmyDataService\\ConfParams.ini";
                CommonLibrary.Ini.IniFile ini = new CommonLibrary.Ini.IniFile(sFileLocation);

                string sEndPoint = ini.IniReadValue("Default", "ENDPOINT_SEND_INVOICES");
                string sUser = ini.IniReadValue("Default", "AADE_USER_ID");
                string sSubscription = ini.IniReadValue("Default", "AADE_SUBSCRIPTION_KEY");

                //string sEndPoint = "https://mydata-dev.azure-api.net/SendInvoices";

                object q = _oDocument.AADEDocument;

                this.Client = new RestClient(sEndPoint);
                this.Client.Timeout = -1;
                this.Request = new RestRequest(Method.POST);
                //this.Request.AddHeader("aade-user-id", "vplanet01");
                //this.Request.AddHeader("ocp-apim-subscription-key", "df217e0ebb2f4b2caeba6ce4764514e2");

                //this.Request.AddHeader("aade-user-id", "VPLAGIANOS");
                //this.Request.AddHeader("ocp-apim-subscription-key", "cca62e7f48114109ad9197e3ec719a47");

                this.Request.AddHeader("aade-user-id", sUser);
                this.Request.AddHeader("ocp-apim-subscription-key", sSubscription);

                //VPLAGIANOS
                //VPLAGIANOS
                //cca62e7f48114109ad9197e3ec719a47

                //this.Request.AddHeader("aade-user-id", "vplanet01");
                //this.Request.AddHeader("ocp-apim-subscription-key", "df217e0ebb2f4b2caeba6ce4764514e2");
                //VPLANET
                //VPLAGIANOS
                //064b84b769f9458aa0facb734f9b8b66











                //this.Request.AddHeader("aade-user-id", "DAESLONDON");
                //this.Request.AddHeader("ocp-apim-subscription-key", "80ce8766428d44a0b35b3b2f7be4d170");

                System.Xml.Serialization.XmlSerializer oXML = null;
                MemoryStream ms = new MemoryStream();
                oXML = new System.Xml.Serialization.XmlSerializer(typeof(InvoicesDoc));
                oXML.Serialize(ms, _oDocument.AADEDocument);
                ms.Position = 0;

                StreamReader SR = new StreamReader(ms);
                string sBody = SR.ReadToEnd();

                this.Request.AddParameter("application/text", sBody, ParameterType.RequestBody);

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                string sPath = "C:\\XML\\2AADE\\" + _oDocument.ObjType + "_" + _oDocument.DocEntry + "_" + _oDocument.DocNum + ".xml";

                using (StreamWriter sw = File.CreateText(sPath))
                {
                    sw.WriteLine(sBody);
                }

                IRestResponse oResponse = this.Client.Execute(this.Request);

                if (oResponse.StatusCode == HttpStatusCode.OK)
                {
                    sPath = "C:\\XML\\" + _oDocument.ObjType + "_" + _oDocument.DocEntry + "_" + _oDocument.DocNum + ".xml";
                    string sXML = "";

                    using (StreamWriter sw = File.CreateText(sPath))
                    {
                        sXML = oResponse.Content;
                        sXML = sXML.Substring(1, sXML.Length - 1);
                        sXML = sXML.Replace("\\r\\n", "");
                        sXML = sXML.Replace("</ResponseDoc>\"", "</ResponseDoc>");
                        sXML = sXML.Replace("\\", "").Replace("\"", "\"");
                        sw.WriteLine(sXML);
                    }


                    Logging.WriteToLog("myDataMethods.AddResponse", Logging.LogStatus.START);
                    this.AddResponse(_oDocument, sXML);
                    Logging.WriteToLog("myDataMethods.AddResponse", Logging.LogStatus.END);

                    System.Xml.Serialization.XmlSerializer ser = new System.Xml.Serialization.XmlSerializer(typeof(ResponseDoc));

                    using (StreamReader sr = new StreamReader(sPath))
                    {
                        ResponseDoc oReply = (ResponseDoc)ser.Deserialize(sr);

                        Logging.WriteToLog("myDataMethods.UpdateDocument", Logging.LogStatus.START);
                        this.UpdateDocument(_oDocument, oReply, false);
                        Logging.WriteToLog("myDataMethods.UpdateDocument", Logging.LogStatus.END);
                    }
                }
                else
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
        private int Cancel(BoDocument _oDocument)
        {
            int iRetVal = 0;
            try
            {
                string sFileLocation = "C:\\Program Files\\sap\\SAPmyDataService\\ConfParams.ini";
                CommonLibrary.Ini.IniFile ini = new CommonLibrary.Ini.IniFile(sFileLocation);

                string sEndPoint = ini.IniReadValue("Default", "ENDPOINT_CANCEL_INVOICE");
                string sUser = ini.IniReadValue("Default", "AADE_USER_ID");
                string sSubscription = ini.IniReadValue("Default", "AADE_SUBSCRIPTION_KEY");

                var queryString = HttpUtility.ParseQueryString(string.Empty);
                queryString["mark"] = "" + _oDocument.MARK + "";

                //sEndPoint = sEndPoint.Replace("#MARK", _oDocument.MARK);

                sEndPoint = sEndPoint + queryString;

                this.Client = new RestClient(sEndPoint);
                this.Client.Timeout = -1;
                this.Request = new RestRequest(Method.POST);
                this.Request.AddHeader("aade-user-id", sUser);
                this.Request.AddHeader("ocp-apim-subscription-key", sSubscription);
                //this.Request.AddParameter("mark", _oDocument.MARK);

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                IRestResponse oResponse = this.Client.Execute(this.Request);

                if (oResponse.StatusCode == HttpStatusCode.OK)
                {
                    string sPath = "C:\\XML\\" + _oDocument.MARK + ".xml";
                    string sXML = "";

                    using (StreamWriter sw = File.CreateText(sPath))
                    {
                        sXML = oResponse.Content;
                        sXML = sXML.Substring(1, sXML.Length - 1);
                        sXML = sXML.Replace("\\r\\n", "");
                        sXML = sXML.Replace("</ResponseDoc>\"", "</ResponseDoc>");
                        sXML = sXML.Replace("\\", "").Replace("\"", "\"");
                        sw.WriteLine(sXML);
                    }

                    Logging.WriteToLog("myDataMethods.AddResponse", Logging.LogStatus.START);
                    this.AddResponse(_oDocument, sXML);
                    Logging.WriteToLog("myDataMethods.AddResponse", Logging.LogStatus.END);

                    System.Xml.Serialization.XmlSerializer ser = new System.Xml.Serialization.XmlSerializer(typeof(ResponseDoc));

                    using (StreamReader sr = new StreamReader(sPath))
                    {
                        ResponseDoc oReply = (ResponseDoc)ser.Deserialize(sr);

                        Logging.WriteToLog("myDataMethods.UpdateDocument", Logging.LogStatus.START);
                        this.UpdateDocument(_oDocument, oReply, true);
                        Logging.WriteToLog("myDataMethods.UpdateDocument", Logging.LogStatus.END);
                    }
                }
            }
            catch (Exception ex)
            {
                var a = new Logging("myDataMethods.Cancel", ex);
            }
            return iRetVal;
        }
        private int AddResponse(BoDocument _oDocument, string _sXML)
        {
            int iRetVal = 0;
            try
            {
                DAL.BoUpdateDB oLog = new DAL.BoUpdateDB();
                oLog.DocEntry = _oDocument.DocEntry;
                oLog.DocNum = _oDocument.DocNum;
                oLog.ObjType = _oDocument.ObjType;
                oLog.XMLReply = _sXML;
                oLog.Company = _oDocument.CompanyDB;
                iRetVal = oLog.AddResponse();
            }
            catch (Exception ex)
            {
                var a = new Logging("myDataMethods.AddResponse", ex);
            }
            return iRetVal;
        }
        private int UpdateDocument(BoDocument _oDocument, ResponseDoc _oReply, bool _bCancel)
        {
            int iRetVal = 0;
            try
            {
                //Success
                string sStatusCode = _oReply.response[0].statusCode;

                DAL.BoUpdateDB oLog = new DAL.BoUpdateDB();
                oLog.DocEntry = _oDocument.DocEntry;
                oLog.ObjType = _oDocument.ObjType;
                oLog.Result = sStatusCode;
                oLog.Company = _oDocument.CompanyDB;

                if (sStatusCode == "Success")
                {
                    oLog.MARK = _oReply.response[0].Items[1].ToString();
                    oLog.UID = _oReply.response[0].Items[0].ToString();
                    oLog.UpdateDocument();
                }
                else
                {
                    if (_oReply.response[0].ItemsElementName[0] == ItemsChoiceType.errors)
                    {
                        ResponseTypeErrors oError = (ResponseTypeErrors)_oReply.response[0].Items[0];
                        oLog.ErrorCode = oError.error[0].code.ToString();
                        oLog.ErrorDescr = oError.error[0].message.ToString();
                    }

                    if (_bCancel == true) //Αυτό γτ Δεν θέλω να χαθεί το MARK & το UID που είχε πάρει
                    {
                        oLog.MARK = _oDocument.MARK;
                        oLog.UID = _oDocument.UID;
                    }
                    else
                    {
                        oLog.MARK = "";
                        oLog.UID = "";
                    }

                    oLog.UpdateDocument();
                }
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
                try
                {
                    this.ListDocuments = new List<BoDocument>();
                    BoDocument oDocument = null;

                    string sFileLocation = "C:\\Program Files\\sap\\SAPmyDataService\\ConfParams.ini";
                    CommonLibrary.Ini.IniFile ini = new CommonLibrary.Ini.IniFile(sFileLocation);

                    string sConnectionString = ini.IniReadValue("Default", "MSSQLConnectionString");

                    sSQL = "SELECT * FROM TKA_V_ELECTRONIC_INVOICES_2_PROCESS WHERE 1=1 ORDER BY AA DESC";

                    using (SqlConnection oConnection = new SqlConnection(sConnectionString))
                    {
                        oConnection.Open();
                        DataTable dtDocs = new DataTable();
                        using (SqlDataAdapter oSQLAdapter = new SqlDataAdapter(sSQL, oConnection))
                        {
                            //oSQLAdapter.SelectCommand.Parameters.AddWithValue("@USERNAME", "manager");
                            oSQLAdapter.SelectCommand.CommandTimeout = 0;
                            //SQLAdapter.SelectCommand.CommandType = CommandType.;
                            oSQLAdapter.Fill(dtDocs);
                        }

                        foreach (DataRow dtRow in dtDocs.Rows)
                        {
                            oDocument = new BoDocument();
                            oDocument.CompanyDB = dtRow["COMPANY_DB"].ToString();
                            oDocument.ObjType = dtRow["ObjType"].ToString();
                            oDocument.DocEntry = dtRow["DocEntry"].ToString();
                            oDocument.DocNum = dtRow["DocNum"].ToString();

                            oDocument.LoadTotals();

                            this.ListDocuments.Add(oDocument);
                            //iResult+=this.LoadDocuments()
                        }
                    }

                    iRetVal++;
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
                        iResult += this.PrepareDocument(ref oTemp);

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

            private int PrepareDocument(ref BoDocument _oDocument)
            {
                int iRetVal = 0;
                try
                {
                    int iResult = 0;
                    int iSuccess = 7;

                    int iTempHeader, iTempPayment, iTempIssuer, iTempCounterPart, iTempTaxesTotals, iTempDocumentSummary, iTempDetails;
                    iTempHeader = iTempPayment = iTempIssuer = iTempCounterPart = iTempTaxesTotals = iTempDocumentSummary = iTempDetails = 0;

                    _oDocument.AADEDocument = new InvoicesDoc();
                    _oDocument.AADEDocument.invoice = new List<AadeBookInvoiceType>();
                    AadeBookInvoiceType oInvoiceType = new AadeBookInvoiceType();

                    oInvoiceType.invoiceHeader = new InvoiceHeaderType();
                    oInvoiceType.invoiceHeader = this.GetInvoiceHeader(ref _oDocument, out iTempHeader);

                    if (_oDocument.ObjType != "30")
                    {
                        oInvoiceType.paymentMethods = new List<PaymentMethodDetailType>();
                        oInvoiceType.paymentMethods = this.GetPaymentMethods(_oDocument, out iTempPayment);

                        if (oInvoiceType.invoiceHeader.invoiceType != InvoiceType.Item111 &&
                            oInvoiceType.invoiceHeader.invoiceType != InvoiceType.Item112 &&
                            oInvoiceType.invoiceHeader.invoiceType != InvoiceType.Item113 &&
                            oInvoiceType.invoiceHeader.invoiceType != InvoiceType.Item114 &&
                            oInvoiceType.invoiceHeader.invoiceType != InvoiceType.Item115
                            )
                        {
                            oInvoiceType.counterpart = new PartyType();
                            oInvoiceType.counterpart = this.GetCounterPart(_oDocument, out iTempCounterPart);
                        }
                        else
                        {
                            iTempCounterPart++;
                        }
                    }

                    oInvoiceType.issuer = new PartyType();
                    oInvoiceType.issuer = this.GetIssuer(out iTempIssuer);

                    if (_oDocument.TotalTaxesAmount > 0)
                    {
                        List<TaxTotalsType> ListRet = new List<TaxTotalsType>();
                        ListRet = this.GetTaxesTotals(ref _oDocument, out iTempTaxesTotals);
                        oInvoiceType.taxesTotals = ListRet.ToArray();
                    }
                    else
                    {
                        iTempTaxesTotals = 1;
                    }

                    oInvoiceType.invoiceDetails = new List<InvoiceRowType>();
                    oInvoiceType.invoiceDetails = this.GetDetails(_oDocument, out iTempDetails);

                    oInvoiceType.invoiceSummary = new InvoiceSummaryType();
                    oInvoiceType.invoiceSummary = this.GetInvoiceSummary(_oDocument, out iTempDocumentSummary);

                    _oDocument.AADEDocument.invoice.Add(oInvoiceType);

                    iResult = iTempHeader + iTempPayment + iTempIssuer + iTempCounterPart + iTempTaxesTotals + iTempDocumentSummary + iTempDetails;

                    if (iResult == iSuccess)
                    {
                        iRetVal++;
                        _oDocument.DocumentStatus = DocumentPrepared.p_Success;
                    }
                    else
                    {
                        Logging.WriteToLog("Error Found On Document:" + _oDocument.ObjType + " / " + _oDocument.DocNum + "", Logging.LogStatus.ERROR);
                        _oDocument.DocumentStatus = DocumentPrepared.pFailure;
                        this.SetIgnoreDue2Error(_oDocument);
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
            private List<InvoiceRowType> GetDetails(BoDocument _oDocument, out int _iResult)
            {
                _iResult = 0;
                List<InvoiceRowType> oRet = new List<InvoiceRowType>();
                string sSQL = "";
                try
                {
                    InvoiceRowType oRow = null;
                    IncomeClassificationType oIncomeClassificationType = null;

                    sSQL = "SELECT * FROM TKA_V_ELECTRONIC_INVOICES_DETAILS WHERE 1=1 AND ObjType = '" + _oDocument.ObjType + "' AND DocEntry = '" + _oDocument.DocEntry + "'";
                    string sFileLocation = "C:\\Program Files\\sap\\SAPmyDataService\\ConfParams.ini";
                    CommonLibrary.Ini.IniFile ini = new CommonLibrary.Ini.IniFile(sFileLocation);

                    string sConnectionString = ini.IniReadValue("Default", "MSSQLConnectionString");
                    sConnectionString = sConnectionString.Replace("#DB_NAME", _oDocument.CompanyDB);

                    using (SqlConnection oConnection = new SqlConnection(sConnectionString))
                    {
                        oConnection.Open();
                        DataTable dtJE = new DataTable();
                        using (SqlDataAdapter oSQLAdapter = new SqlDataAdapter(sSQL, oConnection))
                        {
                            //oSQLAdapter.SelectCommand.Parameters.AddWithValue("@USERNAME", "manager");
                            oSQLAdapter.SelectCommand.CommandTimeout = 0;
                            //SQLAdapter.SelectCommand.CommandType = CommandType.;
                            oSQLAdapter.Fill(dtJE);
                        }

                        int iRow = 0;
                        foreach (DataRow dtRow in dtJE.Rows)
                        {
                            iRow++;
                            oRow = new InvoiceRowType();

                            #region Required
                            oRow.lineNumber = iRow;
                            //oRow.netValue = double.Parse(dtRow["netValue"].ToString());
                            oRow.netValue = Math.Round((double.Parse(dtRow["netValue"].ToString())), 2);
                            oRow.vatCategory = int.Parse(dtRow["vatCategory"].ToString());
                            //oRow.vatAmount = double.Parse(dtRow["vatAmount"].ToString());
                            oRow.vatAmount = Math.Round((double.Parse(dtRow["vatAmount"].ToString())), 2);

                            oIncomeClassificationType = new IncomeClassificationType();
                            //oIncomeClassificationType.amount = double.Parse(dtRow["ClassificationTypeAmount"].ToString());
                            oIncomeClassificationType.amount = Math.Round((double.Parse(dtRow["ClassificationTypeAmount"].ToString())), 2);

                            oIncomeClassificationType.classificationCategory = (IncomeClassificationCategoryType)Enum.Parse(typeof(IncomeClassificationCategoryType), dtRow["classificationCategory"].ToString());
                            oIncomeClassificationType.classificationType = (IncomeClassificationValueType)Enum.Parse(typeof(IncomeClassificationValueType), dtRow["classificationType"].ToString());
                            oIncomeClassificationType.idSpecified = false;

                            oRow.incomeClassification = new List<IncomeClassificationType>();
                            oRow.incomeClassification.Add(oIncomeClassificationType);
                            #endregion

                            #region Not Required
                            //TODO
                            //if (string.IsNullOrEmpty(dtRow["aaaa"].ToString()))
                            //{
                            //    oRow.dienergia = ;
                            //}

                            if (string.IsNullOrEmpty(dtRow["lineComments"].ToString()))
                            {
                                oRow.lineComments = dtRow["lineComments"].ToString();
                            }

                            if (string.IsNullOrEmpty(dtRow["quantity"].ToString()) || double.Parse(dtRow["quantity"].ToString()) == -1)
                            {
                                oRow.quantitySpecified = false;
                            }
                            else
                            {
                                oRow.quantitySpecified = true;
                                //oRow.quantity = double.Parse(dtRow["quantity"].ToString());
                                oRow.quantity = Math.Round((double.Parse(dtRow["quantity"].ToString())), 2);
                            }

                            if (string.IsNullOrEmpty(dtRow["deductionsAmount"].ToString()))
                            {
                                oRow.deductionsAmountSpecified = false;
                            }
                            else
                            {
                                oRow.deductionsAmountSpecified = true;
                                //oRow.deductionsAmount = double.Parse(dtRow["deductionsAmount"].ToString());
                                oRow.deductionsAmount = Math.Round((double.Parse(dtRow["deductionsAmount"].ToString())), 2);
                            }

                            if (string.IsNullOrEmpty(dtRow["otherTaxesAmount"].ToString()))
                            {
                                oRow.otherTaxesAmountSpecified = false;
                            }
                            else
                            {
                                oRow.otherTaxesAmountSpecified = true;
                                //oRow.otherTaxesAmount = double.Parse(dtRow["otherTaxesAmount"].ToString());
                                oRow.otherTaxesAmount = Math.Round((double.Parse(dtRow["otherTaxesAmount"].ToString())), 2);
                            }

                            if (string.IsNullOrEmpty(dtRow["otherTaxesPercentCategory"].ToString()))
                            {
                                oRow.otherTaxesPercentCategorySpecified = false;
                            }
                            else
                            {
                                oRow.otherTaxesPercentCategorySpecified = true;
                                oRow.otherTaxesPercentCategory = int.Parse(dtRow["otherTaxesPercentCategory"].ToString());
                            }

                            if (string.IsNullOrEmpty(dtRow["feesPercentCategory"].ToString()))
                            {
                                oRow.feesPercentCategorySpecified = false;
                            }
                            else
                            {
                                oRow.feesPercentCategorySpecified = true;
                                oRow.feesPercentCategory = int.Parse(dtRow["feesPercentCategory"].ToString());
                            }

                            if (string.IsNullOrEmpty(dtRow["feesAmount"].ToString()))
                            {
                                oRow.feesAmountSpecified = false;
                            }
                            else
                            {
                                oRow.feesAmountSpecified = true;
                                //oRow.feesAmount = double.Parse(dtRow["feesAmount"].ToString());
                                oRow.feesAmount = Math.Round((double.Parse(dtRow["feesAmount"].ToString())), 2);
                            }

                            if (string.IsNullOrEmpty(dtRow["stampDutyPercentCategory"].ToString()))
                            {
                                oRow.stampDutyPercentCategorySpecified = false;
                            }
                            else
                            {
                                oRow.stampDutyPercentCategorySpecified = true;
                                oRow.stampDutyPercentCategory = int.Parse(dtRow["stampDutyPercentCategory"].ToString());
                            }

                            if (string.IsNullOrEmpty(dtRow["stampDutyAmount"].ToString()))
                            {
                                oRow.stampDutyAmountSpecified = false;
                            }
                            else
                            {
                                oRow.stampDutyAmountSpecified = true;
                                //oRow.stampDutyAmount = double.Parse(dtRow["stampDutyAmount"].ToString());
                                oRow.stampDutyAmount = Math.Round((double.Parse(dtRow["stampDutyAmount"].ToString())), 2);
                            }

                            if (string.IsNullOrEmpty(dtRow["withheldPercentCategory"].ToString()) || double.Parse(dtRow["withheldPercentCategory"].ToString()) == -112)
                            {
                                oRow.withheldPercentCategorySpecified = false;
                            }
                            else
                            {
                                oRow.withheldPercentCategorySpecified = true;
                                oRow.withheldPercentCategory = int.Parse(dtRow["withheldPercentCategory"].ToString());

                                if (string.IsNullOrEmpty(dtRow["withheldAmount"].ToString()))
                                {
                                    oRow.withheldAmountSpecified = false;
                                }
                                else
                                {
                                    oRow.withheldAmountSpecified = true;
                                    //oRow.withheldAmount = double.Parse(dtRow["withheldAmount"].ToString());
                                    oRow.withheldAmount = Math.Round((double.Parse(dtRow["withheldAmount"].ToString())), 2);
                                }
                            }

                            if (string.IsNullOrEmpty(dtRow["discountOption"].ToString()))
                            {
                                oRow.discountOptionSpecified = false;
                            }
                            else
                            {
                                oRow.discountOptionSpecified = true;
                                oRow.discountOption = dtRow["discountOption"].ToString() == "false" ? false : true;
                            }

                            if (string.IsNullOrEmpty(dtRow["vatExemptionCategory"].ToString()))
                            {
                                oRow.vatExemptionCategorySpecified = false;
                            }
                            else
                            {
                                oRow.vatExemptionCategorySpecified = true;
                                oRow.vatExemptionCategory = int.Parse(dtRow["vatExemptionCategory"].ToString());
                            }

                            if (string.IsNullOrEmpty(dtRow["measurementUnit"].ToString()))
                            {
                                oRow.measurementUnitSpecified = false;
                            }
                            else
                            {
                                oRow.measurementUnitSpecified = true;
                                oRow.measurementUnit = int.Parse(dtRow["measurementUnit"].ToString());
                            }

                            if (string.IsNullOrEmpty(dtRow["invoiceDetailType"].ToString()))
                            {
                                oRow.invoiceDetailTypeSpecified = false;
                            }
                            else
                            {
                                oRow.invoiceDetailTypeSpecified = true;
                                oRow.invoiceDetailType = int.Parse(dtRow["invoiceDetailType"].ToString());
                            }



                            oRow.expensesClassification = null;
                            #endregion

                            oRet.Add(oRow);
                        }
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

            private List<TaxTotalsType> GetTaxesTotals(ref BoDocument _oDocument, out int _iRetVal)
            {
                _iRetVal = 0;
                List<TaxTotalsType> oRet = new List<TaxTotalsType>();
                string sSQL = "";
                try
                {
                    TaxTotalsType oType = null;
                    oRet = new List<TaxTotalsType>();

                    string sFileLocation = "C:\\Program Files\\sap\\SAPmyDataService\\ConfParams.ini";
                    CommonLibrary.Ini.IniFile ini = new CommonLibrary.Ini.IniFile(sFileLocation);

                    string sConnectionString = ini.IniReadValue("Default", "MSSQLConnectionString");
                    sConnectionString = sConnectionString.Replace("#DB_NAME", _oDocument.CompanyDB);

                    sSQL = "SELECT * FROM TKA_V_ELECTRONIC_INVOICES_TAXES_TOTALS WHERE 1=1 AND ObjType = '" + _oDocument.ObjType + "' AND DocEntry = '" + _oDocument.DocEntry + "'";

                    using (SqlConnection oConnection = new SqlConnection(sConnectionString))
                    {
                        oConnection.Open();
                        DataTable dtJE = new DataTable();
                        using (SqlDataAdapter oSQLAdapter = new SqlDataAdapter(sSQL, oConnection))
                        {
                            //oSQLAdapter.SelectCommand.Parameters.AddWithValue("@USERNAME", "manager");
                            oSQLAdapter.SelectCommand.CommandTimeout = 0;
                            //SQLAdapter.SelectCommand.CommandType = CommandType.;
                            oSQLAdapter.Fill(dtJE);
                        }

                        foreach (DataRow dtRow in dtJE.Rows)
                        {
                            oType = new TaxTotalsType();
                            //oType.taxAmount = double.Parse(dtRow["TAX_AMOUNT"].ToString());
                            oType.taxAmount = Math.Round((double.Parse(dtRow["TAX_AMOUNT"].ToString())), 2);
                            oType.taxCategorySpecified = true;
                            oType.taxCategory = int.Parse(dtRow["TAX_CATEGORY"].ToString());
                            oType.taxType = int.Parse(dtRow["TAX_CODE"].ToString());
                            oType.underlyingValueSpecified = true;
                            //oType.underlyingValue = double.Parse(dtRow["TAX_BASE_AMOUNT"].ToString());
                            oType.underlyingValue = Math.Round((double.Parse(dtRow["TAX_BASE_AMOUNT"].ToString())), 2);

                            oRet.Add(oType);
                        }
                    }
                    _iRetVal++;
                }
                catch (Exception ex)
                {
                    var a = new Logging("myDataMethods.LoadnCreateClass.GetInvoiceSummary", ex);
                }
                return oRet;
            }

            private InvoiceSummaryType GetInvoiceSummary(BoDocument _oDocument, out int _iResult)
            {
                _iResult = 0;
                InvoiceSummaryType oRet = new InvoiceSummaryType();
                string sSQL = "";
                try
                {
                    IncomeClassificationType oIncomeClassificationType = null;
                    oRet = new InvoiceSummaryType();
                    oRet.incomeClassification = new List<IncomeClassificationType>();

                    double dTotal = 0;

                    sSQL = "SELECT * FROM TKA_V_ELECTRONIC_INVOICES_TOTALS WHERE 1=1 AND ObjType = '" + _oDocument.ObjType + "' AND DocEntry = '" + _oDocument.DocEntry + "'";

                    string sFileLocation = "C:\\Program Files\\sap\\SAPmyDataService\\ConfParams.ini";
                    CommonLibrary.Ini.IniFile ini = new CommonLibrary.Ini.IniFile(sFileLocation);

                    string sConnectionString = ini.IniReadValue("Default", "MSSQLConnectionString");
                    sConnectionString = sConnectionString.Replace("#DB_NAME", _oDocument.CompanyDB);

                    using (SqlConnection oConnection = new SqlConnection(sConnectionString))
                    {
                        oConnection.Open();
                        DataTable dtJE = new DataTable();
                        using (SqlDataAdapter oSQLAdapter = new SqlDataAdapter(sSQL, oConnection))
                        {
                            //oSQLAdapter.SelectCommand.Parameters.AddWithValue("@USERNAME", "manager");
                            oSQLAdapter.SelectCommand.CommandTimeout = 0;
                            //SQLAdapter.SelectCommand.CommandType = CommandType.;
                            oSQLAdapter.Fill(dtJE);
                        }

                        foreach (DataRow dtRow in dtJE.Rows)
                        {
                            dTotal += double.Parse(dtRow["Amount"].ToString());

                            oIncomeClassificationType = new IncomeClassificationType();
                            //oIncomeClassificationType.amount = double.Parse(dtRow["Amount"].ToString());
                            oIncomeClassificationType.amount = Math.Round((double.Parse(dtRow["Amount"].ToString())), 2);
                            oIncomeClassificationType.classificationCategory = (IncomeClassificationCategoryType)Enum.Parse(typeof(IncomeClassificationCategoryType), dtRow["classificationCategory"].ToString());
                            oIncomeClassificationType.classificationType = (IncomeClassificationValueType)Enum.Parse(typeof(IncomeClassificationValueType), dtRow["classificationType"].ToString());
                            oIncomeClassificationType.idSpecified = false;

                            oRet.incomeClassification.Add(oIncomeClassificationType);

                        }
                    }

                    //***NOTE*** ALL FIELDS ARE REQUIRED!!!!
                    oRet.totalDeductionsAmount = 0.00;
                    oRet.totalFeesAmount = 0.00;
                    //oRet.totalGrossValue = dTotal + _oDocument.TotalVATAmount;//Net + taxes (Το Taxes περιλαμβάνει όλους τους επιπλέον φόρους βλ. View Φόρων)
                    oRet.totalGrossValue = Math.Round((dTotal + _oDocument.TotalVATAmount), 2);//Net + taxes (Το Taxes περιλαμβάνει όλους τους επιπλέον φόρους βλ. View Φόρων)
                    //oRet.totalNetValue = dTotal;
                    oRet.totalNetValue = Math.Round(dTotal, 2);
                    oRet.totalOtherTaxesAmount = 0.00;
                    oRet.totalStampDutyAmount = 0.00;
                    oRet.totalVatAmount = Math.Round((_oDocument.TotalVATAmount), 2);
                    oRet.totalWithheldAmount = 0.00;
                    oRet.expensesClassification = null;

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
            private PartyType GetIssuer(out int _iResult)
            {
                _iResult = 0;
                PartyType oRet = new PartyType();
                try
                {
                    oRet.vatNumber = AddOnSettings.AADE_AFM;
                    oRet.country = CountryType.GR;
                    oRet.branch = 0;
                    #region Not Requiered
                    //oRet.address.postalCode = "";
                    //oRet.address.city = "";
                    //oRet.address.street = "";
                    //oRet.address.number = "";
                    #endregion

                    _iResult++;
                }
                catch (Exception ex)
                {
                    var a = new Logging("myDataMethods.LoadnCreateClass.GetIssuer", ex);
                }
                return oRet;
            }

            /// <summary>
            /// Δημιουργία Αντικειμένου για τον Συν/μένο
            /// </summary>
            /// <param name="_iResult">1 For Success, 0 For Failure</param>
            /// <param name="_oDocument">To Αντικείμενο του Παραστατικού</param>
            /// <returns>Το Αντικείμενο της ΑΑΔΕ για τον Συν/μένο</returns>
            private PartyType GetCounterPart(BoDocument _oDocument, out int _iResult)
            {
                PartyType oRet = new PartyType();
                _iResult = 0;
                try
                {
                    switch (_oDocument.CounterPart_Define_Area)
                    {
                        case "GR":
                            oRet.vatNumber = _oDocument.CounterPart_vatNumber;
                            oRet.country = (CountryType)Enum.Parse(typeof(CountryType), _oDocument.CounterPart_country);
                            oRet.branch = 0;
                            break;
                        case "EU":
                            oRet.name = _oDocument.CounterPart_name;
                            oRet.country = (CountryType)Enum.Parse(typeof(CountryType), _oDocument.CounterPart_country);
                            oRet.vatNumber = _oDocument.CounterPart_vatNumber;
                            oRet.branch = 0;
                            oRet.address = new AddressType();
                            oRet.address.city = _oDocument.CounterPart_country;
                            oRet.address.street = _oDocument.CounterPart_address_street;
                            oRet.address.postalCode = _oDocument.CounterPart_address_postalCode;
                            break;
                        case "TX":
                            oRet.name = _oDocument.CounterPart_name;
                            oRet.country = (CountryType)Enum.Parse(typeof(CountryType), _oDocument.CounterPart_country);
                            oRet.vatNumber = _oDocument.CounterPart_vatNumber;
                            oRet.branch = 0;
                            oRet.address = new AddressType();
                            oRet.address.city = _oDocument.CounterPart_address_city;
                            oRet.address.street = _oDocument.CounterPart_address_street;
                            oRet.address.postalCode = _oDocument.CounterPart_address_postalCode;
                            break;
                    }
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
            private List<PaymentMethodDetailType> GetPaymentMethods(BoDocument _oDocument, out int _iResult)
            {
                _iResult = 0;
                string sSQL = "";
                List<PaymentMethodDetailType> oRet = new List<PaymentMethodDetailType>();
                try
                {
                    sSQL = "SELECT * FROM TKA_V_ELECTRONIC_INVOICES_PAYMENT_TOTALS WHERE 1=1 AND ObjType = '" + _oDocument.ObjType + "' AND DocEntry = '" + _oDocument.DocEntry + "'";
                    CommonLibrary.Ini.IniFile ini = new CommonLibrary.Ini.IniFile("C:\\Program Files\\sap\\SAPmyDataService\\ConfParams.ini");
                    using (SqlConnection oConnection = new SqlConnection(ini.IniReadValue("Default", "MSSQLConnectionString")))
                    {
                        oConnection.Open();
                        DataTable dtRet = new DataTable();
                        using (SqlDataAdapter oSQLAdapter = new SqlDataAdapter(sSQL, oConnection))
                        {
                            oSQLAdapter.SelectCommand.CommandTimeout = 0;
                            //SQLAdapter.SelectCommand.CommandType = CommandType.;
                            oSQLAdapter.Fill(dtRet);
                        }

                        foreach (DataRow dtRow in dtRet.Rows)
                        {
                            PaymentMethodDetailType oPayment = null;

                            oPayment = new PaymentMethodDetailType();
                            //oPayment.amount = double.Parse(dtRow["amount"].ToString());
                            oPayment.amount = Math.Round((double.Parse(dtRow["amount"].ToString())), 2);
                            oPayment.type = int.Parse(dtRow["type"].ToString());
                            oRet.Add(oPayment);
                        }

                    }
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
            private InvoiceHeaderType GetInvoiceHeader(ref BoDocument _oDocument, out int _iResult)
            {
                _iResult = 0;
                InvoiceHeaderType oRet = new InvoiceHeaderType();
                string sSQL = "";
                try
                {
                    DateTime dtRefDate = DateTime.Now;

                    string sFileLocation = "C:\\Program Files\\sap\\SAPmyDataService\\ConfParams.ini";
                    CommonLibrary.Ini.IniFile ini = new CommonLibrary.Ini.IniFile(sFileLocation);

                    //ΠΡΕΠΕΙ ΝΑ ΣΚΕΦΤΩ ΚΑΤΙ ΔΗΜΙΟΥΡΓΙΚΟ ΓΙΑ ΝΑ ΣΥΝΔΕΟΜΑΙ ΣΕ ΔΙΑΦΟΡΕΤΙΚΗ ΒΑΣΗ ΑΝΑΛΟΓΑ ΜΕ ΤΟ DBNAME (HANA EDITION)
                    string sConnectionString = ini.IniReadValue("Default", "MSSQLConnectionString");
                    sConnectionString = sConnectionString.Replace("#DB_NAME", _oDocument.CompanyDB);

                    sSQL = "SELECT * FROM TKA_V_ELECTRONIC_INVOICES_HEADER WHERE 1=1 AND ObjType = '" + _oDocument.ObjType + "' AND DocEntry = '" + _oDocument.DocEntry + "'";
                    using (SqlConnection oConnection = new SqlConnection(sConnectionString))
                    {
                        oConnection.Open();
                        DataTable dtJE = new DataTable();
                        using (SqlDataAdapter oSQLAdapter = new SqlDataAdapter(sSQL, oConnection))
                        {
                            //oSQLAdapter.SelectCommand.Parameters.AddWithValue("@USERNAME", "manager");
                            oSQLAdapter.SelectCommand.CommandTimeout = 0;
                            //SQLAdapter.SelectCommand.CommandType = CommandType.;
                            oSQLAdapter.Fill(dtJE);
                        }

                        foreach (DataRow dtRow in dtJE.Rows)
                        {
                            #region CounterPart Data
                            _oDocument.CounterPart_address_city = dtRow["CounterPart_address_city"].ToString();
                            _oDocument.CounterPart_address_postalCode = dtRow["CounterPart_address_postalCode"].ToString();
                            _oDocument.CounterPart_address_street = dtRow["CounterPart_address_street"].ToString();
                            _oDocument.CounterPart_branch = dtRow["CounterPart_branch"].ToString();
                            _oDocument.CounterPart_country = dtRow["CounterPart_country"].ToString();
                            _oDocument.CounterPart_name = dtRow["CounterPart_name"].ToString();
                            _oDocument.CounterPart_vatNumber = dtRow["CounterPart_vatNumber"].ToString();
                            _oDocument.CounterPart_Define_Area = dtRow["CounterPart_Define_Area"].ToString();

                            #endregion

                            #region Required
                            //oRet.aa = sTransID;
                            oRet.aa = dtRow["aa"].ToString();
                            oRet.series = dtRow["series"].ToString();
                            oRet.issueDate = DateTime.Parse(dtRow["issueDate"].ToString());
                            oRet.invoiceType = (InvoiceType)Enum.Parse(typeof(InvoiceType), dtRow["invoiceType"].ToString().Replace(".", ""));
                            #endregion

                            #region NotRequired
                            if (string.IsNullOrEmpty(dtRow["currency"].ToString()))
                            {
                                oRet.currencySpecified = false;
                            }
                            else
                            {
                                CurrencyType enCur = (CurrencyType)Enum.Parse(typeof(CurrencyType), dtRow["currency"].ToString());
                                oRet.currencySpecified = true;
                                oRet.currency = enCur;
                            }

                            if (string.IsNullOrEmpty(dtRow["vatPaymentSuspension"].ToString()))
                            {
                                oRet.vatPaymentSuspensionSpecified = false;
                            }
                            else
                            {
                                oRet.vatPaymentSuspensionSpecified = true;
                                oRet.vatPaymentSuspension = dtRow["vatPaymentSuspension"].ToString() == "false" ? false : true;
                            }
                            if (string.IsNullOrEmpty(dtRow["exchangeRate"].ToString()) || double.Parse(dtRow["exchangeRate"].ToString()) == -775)
                            {
                                oRet.exchangeRateSpecified = false;
                            }
                            else
                            {
                                oRet.exchangeRateSpecified = true;
                                //oRet.exchangeRate = double.Parse(dtRow["exchangeRate"].ToString());
                                oRet.exchangeRate = Math.Round((double.Parse(dtRow["exchangeRate"].ToString())), 2);
                            }
                            if (string.IsNullOrEmpty(dtRow["selfPricing"].ToString()))
                            {
                                oRet.selfPricingSpecified = false;
                            }
                            else
                            {
                                oRet.selfPricingSpecified = true;
                                oRet.selfPricing = dtRow["selfPricing"].ToString() == "false" ? false : true;
                            }
                            if (string.IsNullOrEmpty(dtRow["dispatchDate"].ToString()))
                            {
                                oRet.dispatchDateSpecified = false;
                            }
                            else
                            {
                                oRet.dispatchDateSpecified = true;
                                oRet.dispatchDate = DateTime.Parse(dtRow["dispatchDate"].ToString());
                            }
                            if (string.IsNullOrEmpty(dtRow["dispatchTime"].ToString()))
                            {
                                oRet.dispatchTimeSpecified = false;
                            }
                            else
                            {
                                oRet.dispatchTimeSpecified = true;
                                oRet.dispatchTime = DateTime.Parse(dtRow["dispatchTime"].ToString());
                            }
                            if (!string.IsNullOrEmpty(dtRow["vehicleNumber"].ToString()))
                            {
                                oRet.vehicleNumber = dtRow["vehicleNumber"].ToString();
                            }

                            if (oRet.invoiceType != InvoiceType.Item21 &&
                                oRet.invoiceType != InvoiceType.Item22 &&
                                oRet.invoiceType != InvoiceType.Item23 &&
                                oRet.invoiceType != InvoiceType.Item24)
                            {
                                if (string.IsNullOrEmpty(dtRow["movePurpose"].ToString()))
                                {
                                    oRet.movePurposeSpecified = false;
                                }
                                else
                                {
                                    oRet.movePurposeSpecified = true;
                                    oRet.movePurpose = int.Parse(dtRow["movePurpose"].ToString());
                                }
                            }
                            #endregion
                            //TODO
                            //List<long> correlatedInvoicesField;
                        }
                    }
                    _iResult++;
                }
                catch (Exception ex)
                {
                    Logging.WriteToLog("sSQL=" + sSQL, Logging.LogStatus.RET_VAL);
                    var a = new Logging("myDataMethods.LoadnCreateClass.GetInvoiceHeader", ex);
                }
                return oRet;
            }

            private void SetIgnoreDue2Error(BoDocument _oDocument)
            {
                try
                {
                    DAL.BoUpdateDB oLog = new DAL.BoUpdateDB();
                    oLog.Company = _oDocument.CompanyDB;
                    oLog.ObjType = _oDocument.ObjType;
                    oLog.DocEntry = _oDocument.DocEntry;
                    oLog.DocNum = _oDocument.DocNum;
                    int iResult = oLog.UpdateDocumentSETIgnore();
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

                    //Logging.WriteToLog("myDataMethods.LoadnCreateClass.LoadDocumentsProcess", Logging.LogStatus.START);
                    iResult += this.LoadDocumentsProcess();
                    //Logging.WriteToLog("myDataMethods.LoadnCreateClass.LoadDocumentsProcess", Logging.LogStatus.END);

                    if (iResult == 1)
                    {
                        //Logging.WriteToLog("myDataMethods.LoadnCreateClass.PrepareDocumentsProcess", Logging.LogStatus.START);
                        iResult += this.PrepareDocumentsProcess();
                        //Logging.WriteToLog("myDataMethods.LoadnCreateClass.PrepareDocumentsProcess", Logging.LogStatus.END);
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
        #endregion
    }
}
