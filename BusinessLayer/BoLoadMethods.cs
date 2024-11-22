using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using CommonLibrary.ExceptionHandling;
using SAPmyDataService.Enumerators;

namespace SAPmyDataService.BusinessLayer
{
    public class BoLoadMethods
    {
        #region Public Properties
        public List<BoDocument> ListDocuments { get; set; }
        #endregion

        #region Private Properties
        #endregion

        public BoLoadMethods()
        { }

        #region Private Methods
        #endregion

        #region Public Methods
        /// <summary>
        /// Φόρτωση Δεδομένων για Αποστολή
        /// </summary>
        /// <param name="_enType">Τύπος Αντικειμένου Φόρτωσης</param>
        /// <returns>1 For Success, 0 For Failure</returns>
        public int Load(Enumerators.ot_Object _enType)
        {
            int iRetVal = 0;
            try
            {
                this.ListDocuments = new List<BoDocument>();

                LoadClass oLoad = new LoadClass();
                iRetVal = oLoad.Exec(_enType);

                this.ListDocuments = oLoad.ListDocuments;
            }
            catch (Exception ex)
            {
                var a = new Logging("ElectronicInvoicingMethods.Load", ex);
            }
            return iRetVal;
        }

        /// <summary>
        /// Αποστολή Δεδομένων στο API
        /// </summary>
        /// <param name="_enType">Τύπος Αντικειμένου Αποστολής</param>
        /// <returns>1 For Success, 0 For Failure</returns>
        public int Send(Enumerators.ot_Object _enType)
        {
            int iRetVal = 0;
            try
            {
                //SendClass oSend = new SendClass();
                //oSend.ListDocuments = this.ListDocuments;

                //iRetVal = oSend.Exec(_enType);
            }
            catch (Exception ex)
            {
                var a = new Logging("ElectronicInvoicingMethods.Send", ex);
            }
            return iRetVal;
        }

        #endregion

        #region Internal Classes
        internal class LoadClass
        {
            #region Public Properties
            public List<BoDocument> ListDocuments { get; set; }
            #endregion

            #region Private Properties

            #endregion

            public LoadClass()
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

                    string sFileLocation = AppDomain.CurrentDomain.BaseDirectory + "\\" + "ConfParams.ini";
                    CommonLibrary.Ini.IniFile ini = new CommonLibrary.Ini.IniFile(sFileLocation);

                    string sConnectionString = ini.IniReadValue("Default", "SAPConnectionString");

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

                            this.ListDocuments.Add(oDocument);
                            //iResult+=this.LoadDocuments()
                        }
                    }

                    iRetVal++;
                }
                catch (Exception ex)
                {
                    Logging.WriteToLog("_sSQL=" + sSQL, Logging.LogStatus.RET_VAL);
                    var a = new Logging("BoLoadMethods.LoadClass.LoadDocumentsProcess", ex);
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
                    var a = new Logging("BoLoadMethods.LoadClass.LoadDocuments", ex);
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

                        oInvoiceType.counterpart = new PartyType();
                        oInvoiceType.counterpart = this.GetCounterPart(_oDocument, out iTempCounterPart);
                    }

                    oInvoiceType.issuer = new PartyType();
                    oInvoiceType.issuer = this.GetIssuer(out iTempIssuer);

                    List<TaxTotalsType> ListRet = new List<TaxTotalsType>();
                    ListRet = this.GetTaxesTotals(ref _oDocument, out iTempTaxesTotals);
                    oInvoiceType.taxesTotals = ListRet.ToArray();

                    oInvoiceType.invoiceDetails = new List<InvoiceRowType>();
                    oInvoiceType.invoiceDetails = this.GetDetails(_oDocument, out iTempDetails);

                    oInvoiceType.invoiceSummary = new InvoiceSummaryType();
                    oInvoiceType.invoiceSummary = this.GetInvoiceSummary(_oDocument, out iTempDocumentSummary);

                    _oDocument.AADEDocument.invoice.Add(oInvoiceType);

                    iResult = iTempHeader + iTempPayment + iTempIssuer + iTempCounterPart + iTempTaxesTotals + iTempDocumentSummary + iTempDetails;

                    if (iResult == iSuccess)
                    {
                        iRetVal++;
                    }
                }
                catch (Exception ex)
                {
                    var a = new Logging("BoLoadMethods.LoadClass.LoadDocuments", ex);
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

                    sSQL = "";
                    string sFileLocation = AppDomain.CurrentDomain.BaseDirectory + "\\" + "ConfParams.ini";
                    CommonLibrary.Ini.IniFile ini = new CommonLibrary.Ini.IniFile(sFileLocation);

                    string sConnectionString = ini.IniReadValue("Default", "SAPConnectionString");
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
                            oRow.netValue = double.Parse(dtRow["netValue"].ToString());
                            oRow.vatCategory = int.Parse(dtRow["vatCategory"].ToString());
                            oRow.vatAmount = double.Parse(dtRow["vatAmount"].ToString());

                            oIncomeClassificationType = new IncomeClassificationType();
                            oIncomeClassificationType.amount = double.Parse(dtRow["ClassificationTypeAmount"].ToString());

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

                            if (string.IsNullOrEmpty(dtRow["quantity"].ToString()))
                            {
                                oRow.quantitySpecified = false;
                            }
                            else
                            {
                                oRow.quantitySpecified = true;
                                oRow.quantity = double.Parse(dtRow["quantity"].ToString());
                            }

                            if (string.IsNullOrEmpty(dtRow["deductionsAmount"].ToString()))
                            {
                                oRow.deductionsAmountSpecified = false;
                            }
                            else
                            {
                                oRow.deductionsAmountSpecified = true;
                                oRow.deductionsAmount = double.Parse(dtRow["deductionsAmount"].ToString());
                            }

                            if (string.IsNullOrEmpty(dtRow["otherTaxesAmount"].ToString()))
                            {
                                oRow.otherTaxesAmountSpecified = false;
                            }
                            else
                            {
                                oRow.otherTaxesAmountSpecified = true;
                                oRow.otherTaxesAmount = double.Parse(dtRow["otherTaxesAmount"].ToString());
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
                                oRow.feesAmount = double.Parse(dtRow["feesAmount"].ToString());
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
                                oRow.stampDutyAmount = double.Parse(dtRow["stampDutyAmount"].ToString());
                            }

                            if (string.IsNullOrEmpty(dtRow["withheldAmount"].ToString()))
                            {
                                oRow.withheldAmountSpecified = false;
                            }
                            else
                            {
                                oRow.withheldAmountSpecified = true;
                                oRow.withheldAmount = double.Parse(dtRow["withheldAmount"].ToString());
                            }

                            if (string.IsNullOrEmpty(dtRow["withheldPercentCategory"].ToString()))
                            {
                                oRow.withheldPercentCategorySpecified = false;
                            }
                            else
                            {
                                oRow.withheldPercentCategorySpecified = true;
                                oRow.withheldPercentCategory = int.Parse(dtRow["withheldPercentCategory"].ToString());
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
                    var a = new Logging("BoLoadMethods.LoadClass.GetDetails", ex);
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

                    string sFileLocation = AppDomain.CurrentDomain.BaseDirectory + "\\" + "ConfParams.ini";
                    CommonLibrary.Ini.IniFile ini = new CommonLibrary.Ini.IniFile(sFileLocation);

                    string sConnectionString = ini.IniReadValue("Default", "SAPConnectionString");
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
                            oType = new TaxTotalsType();
                            oType.taxAmount = double.Parse(dtRow["TAX_AMOUNT"].ToString());
                            oType.taxCategorySpecified = true;
                            oType.taxCategory = int.Parse(dtRow["TAX_CATEGORY"].ToString());
                            oType.taxType = int.Parse(dtRow["TAX_CODE"].ToString());
                            oType.underlyingValueSpecified = true;
                            oType.underlyingValue = double.Parse(dtRow["TAX_BASE_AMOUNT"].ToString());

                            oRet.Add(oType);
                        }
                    }
                    _iRetVal++;
                }
                catch (Exception ex)
                {
                    var a = new Logging("BoLoadMethods.LoadClass.GetInvoiceSummary", ex);
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

                    sSQL = "SELECT " + Environment.NewLine +
                        " Classification," + Environment.NewLine +
                        " IncomeType," + Environment.NewLine +
                        " Amount = SUM(Amount)" + Environment.NewLine +
                        " FROM TKA_F_SELECT_MY_DATA_JOURNAL_DATA_HARDCODED('" + _oDocument.TransId + "')" + Environment.NewLine +
                        " GROUP BY" + Environment.NewLine +
                        " Classification," + Environment.NewLine +
                        " IncomeType";

                    string sFileLocation = AppDomain.CurrentDomain.BaseDirectory + "\\" + "ConfParams.ini";
                    CommonLibrary.Ini.IniFile ini = new CommonLibrary.Ini.IniFile(sFileLocation);

                    string sConnectionString = ini.IniReadValue("Default", "SAPConnectionString");
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
                            oIncomeClassificationType.amount = double.Parse(dtRow["Amount"].ToString());
                            oIncomeClassificationType.classificationCategory = (IncomeClassificationCategoryType)Enum.Parse(typeof(IncomeClassificationCategoryType), dtRow["classificationCategory"].ToString());
                            oIncomeClassificationType.classificationType = (IncomeClassificationValueType)Enum.Parse(typeof(IncomeClassificationValueType), dtRow["classificationType"].ToString());
                            oIncomeClassificationType.idSpecified = false;

                            oRet.incomeClassification.Add(oIncomeClassificationType);

                        }
                    }

                    //***NOTE*** ALL FIELDS ARE REQUIRED!!!!
                    oRet.totalDeductionsAmount = 0;
                    oRet.totalFeesAmount = 0;
                    oRet.totalGrossValue = 0;//Net + taxes (Το Taxes περιλαμβάνει όλους τους επιπλέον φόρους βλ. View Φόρων)
                    oRet.totalNetValue = dTotal;
                    oRet.totalOtherTaxesAmount = 0;
                    oRet.totalStampDutyAmount = 0;
                    oRet.totalVatAmount = 0;
                    oRet.totalWithheldAmount = 0;
                    oRet.expensesClassification = null;

                    _iResult++;
                }
                catch (Exception ex)
                {
                    var a = new Logging("BoLoadMethods.LoadClass.GetInvoiceSummary", ex);
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
                    var a = new Logging("BoLoadMethods.LoadClass.GetIssuer", ex);
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
                    //θα πρέπει να γίνει διακριση αν είναι Ελλάδα / ΕΕ / ΤΧ
                    string sCountry = "1";

                    switch (sCountry)//Ελλάδα
                    {
                        case "1":
                            oRet.vatNumber = _oDocument.CounterPart_vatNumber;
                            oRet.country = (CountryType)Enum.Parse(typeof(CountryType), _oDocument.CounterPart_country);
                            oRet.branch = 0;
                            break;
                        case "2":
                            oRet.name = _oDocument.CounterPart_name;
                            oRet.country = (CountryType)Enum.Parse(typeof(CountryType), _oDocument.CounterPart_country);
                            oRet.vatNumber = _oDocument.CounterPart_vatNumber;
                            oRet.branch = 0;
                            oRet.address = new AddressType();
                            oRet.address.city = _oDocument.CounterPart_country;
                            oRet.address.street = _oDocument.CounterPart_address_street;
                            oRet.address.postalCode = _oDocument.CounterPart_address_postalCode;
                            break;
                        case "3":
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
                    var a = new Logging("BoLoadMethods.LoadClass.GetCounterPart", ex);
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
                    sSQL = "";
                    CommonLibrary.Ini.IniFile ini = new CommonLibrary.Ini.IniFile("C:\\Program Files\\sap\\ServicesLogs\\Connection.ini");
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
                            oPayment.amount = double.Parse(dtRow["amount"].ToString());
                            oPayment.type = int.Parse(dtRow["type"].ToString());
                            oRet.Add(oPayment);
                        }

                    }
                    _iResult++;
                }
                catch (Exception ex)
                {
                    Logging.WriteToLog("sSQL=" + sSQL, Logging.LogStatus.RET_VAL);
                    var a = new Logging("BoLoadMethods.LoadClass.GetPaymentMethods", ex);
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

                    string sFileLocation = AppDomain.CurrentDomain.BaseDirectory + "\\" + "ConfParams.ini";
                    CommonLibrary.Ini.IniFile ini = new CommonLibrary.Ini.IniFile(sFileLocation);

                    //ΠΡΕΠΕΙ ΝΑ ΣΚΕΦΤΩ ΚΑΤΙ ΔΗΜΙΟΥΡΓΙΚΟ ΓΙΑ ΝΑ ΣΥΝΔΕΟΜΑΙ ΣΕ ΔΙΑΦΟΡΕΤΙΚΗ ΒΑΣΗ ΑΝΑΛΟΓΑ ΜΕ ΤΟ DBNAME (HANA EDITION)
                    string sConnectionString = ini.IniReadValue("Default", "SAPConnectionString");
                    sConnectionString = sConnectionString.Replace("#DB_NAME", _oDocument.CompanyDB);

                    sSQL = "SELECT * FROM TKA_F_SELECT_MY_DATA_JOURNAL_DATA_HARDCODED('" + _oDocument.TransId + "')";
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
                            #endregion

                            #region Required
                            //oRet.aa = sTransID;
                            oRet.aa = dtRow["aa"].ToString();
                            oRet.series = dtRow["series"].ToString();
                            oRet.issueDate = DateTime.Parse(dtRow["issueDate"].ToString());
                            oRet.invoiceType = (InvoiceType)Enum.Parse(typeof(InvoiceType), dtRow["invoiceType"].ToString());
                            #endregion

                            #region NotRequired
                            if (string.IsNullOrEmpty(dtRow["currencyField"].ToString()))
                            {
                                oRet.currencySpecified = false;
                            }
                            else
                            {
                                CurrencyType enCur = (CurrencyType)Enum.Parse(typeof(CurrencyType), dtRow["currencyField"].ToString());
                                oRet.currencySpecified = true;
                                oRet.currency = enCur;
                            }

                            if (string.IsNullOrEmpty(dtRow["vatPaymentSuspensionField"].ToString()))
                            {
                                oRet.vatPaymentSuspensionSpecified = false;
                            }
                            else
                            {
                                oRet.vatPaymentSuspensionSpecified = true;
                                oRet.vatPaymentSuspension = dtRow["vatPaymentSuspensionField"].ToString() == "false" ? false : true;
                            }
                            if (string.IsNullOrEmpty(dtRow["exchangeRateField"].ToString()))
                            {
                                oRet.exchangeRateSpecified = false;
                            }
                            else
                            {
                                oRet.exchangeRateSpecified = true;
                                oRet.exchangeRate = double.Parse(dtRow["exchangeRateField"].ToString());
                            }
                            if (string.IsNullOrEmpty(dtRow["selfPricingField"].ToString()))
                            {
                                oRet.selfPricingSpecified = false;
                            }
                            else
                            {
                                oRet.selfPricingSpecified = true;
                                oRet.selfPricing = dtRow["selfPricingField"].ToString() == "false" ? false : true;
                            }
                            if (string.IsNullOrEmpty(dtRow["dispatchDateField"].ToString()))
                            {
                                oRet.dispatchDateSpecified = false;
                            }
                            else
                            {
                                oRet.dispatchDateSpecified = true;
                                oRet.dispatchDate = DateTime.Parse(dtRow["dispatchDateField"].ToString());
                            }
                            if (string.IsNullOrEmpty(dtRow["dispatchTimeField"].ToString()))
                            {
                                oRet.dispatchTimeSpecified = false;
                            }
                            else
                            {
                                oRet.dispatchTimeSpecified = true;
                                oRet.dispatchTime = DateTime.Parse(dtRow["dispatchTimeField"].ToString());
                            }
                            if (!string.IsNullOrEmpty(dtRow["vehicleNumberField"].ToString()))
                            {
                                oRet.vehicleNumber = dtRow["vehicleNumberField"].ToString();
                            }
                            if (string.IsNullOrEmpty(dtRow["movePurposeField"].ToString()))
                            {
                                oRet.movePurposeSpecified = false;
                            }
                            else
                            {
                                oRet.movePurposeSpecified = true;
                                oRet.movePurpose = int.Parse(dtRow["movePurposeField"].ToString());
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
                    var a = new Logging("BoLoadMethods.LoadClass.GetInvoiceHeader", ex);
                }
                return oRet;
            }
            #endregion

            #region Public Methods
            public int Exec(Enumerators.ot_Object _enType)
            {
                int iRetVal = 0;
                try
                {
                    string sSQL = "";
                    int iSuccess = 2;
                    int iResult = 0;

                    //Logging.WriteToLog("BoLoadMethods.LoadClass.LoadDocumentsProcess", Logging.LogStatus.START);
                    iResult += this.LoadDocumentsProcess();
                    //Logging.WriteToLog("BoLoadMethods.LoadClass.LoadDocumentsProcess", Logging.LogStatus.END);

                    if (iResult == 1)
                    {
                        //Logging.WriteToLog("BoLoadMethods.LoadClass.PrepareDocumentsProcess", Logging.LogStatus.START);
                        this.PrepareDocumentsProcess();
                        //Logging.WriteToLog("BoLoadMethods.LoadClass.PrepareDocumentsProcess", Logging.LogStatus.END);
                    }

                    if (iResult == iSuccess)
                    {
                        iRetVal++;
                    }
                }
                catch (Exception ex)
                {
                    var a = new Logging("ElectronicInvoicingMethods.LoadClass.Exec", ex);
                }
                return iRetVal;
            }
            #endregion
        }

        //internal class SendClass
        //{
        //    #region Public Properties
        //    public List<BoDocument> ListDocuments { get; set; }
        //    #endregion

        //    #region Private Properties
        //    private BoTransmission APIConnector { get; set; }
        //    private string TRANCode { get; set; }
        //    private string APPICallID { get; set; }
        //    private string UserSign { get; set; }
        //    #endregion

        //    public SendClass()
        //    {
        //        this.ListDocuments = new List<BoDocument>();
        //        this.APIConnector = new BoTransmission("");
        //    }

        //    #region Private Methods
        //    private int PostDocumentProcess()
        //    {
        //        int iRetVal = 0;
        //        try
        //        {
        //            BoEpsilonDocument oEpsilonDocument = null;
        //            BoDocument oDocument = null;

        //            int iSuccess = this.ListDocuments.Count;
        //            int iResult = 0;
        //            int iTempResult = 0;
        //            int iTempSuccess = 3;

        //            for (int i = 0; i < this.ListDocuments.Count; i++)
        //            {
        //                iTempResult = 0;
        //                oDocument = new BoDocument();
        //                oDocument = this.ListDocuments[i];

        //                int iAlreadyProccessed = BoDAL.ProccessedDocumentExist(oDocument.ObjectCode, oDocument.DocNum);
        //                int iLogged = 0;
        //                if (iAlreadyProccessed == 0)
        //                {
        //                    iLogged = BoDAL.AddProccessedDocument(oDocument.ObjectCode, oDocument.DocNum, oDocument.DocEntry, "0", "0", "", "");
        //                }
        //                else
        //                {
        //                    iLogged = 1;
        //                    string sDocumentID = "";
        //                    BoDAL.GetDocumentID(oDocument.ObjectCode, oDocument.DocNum, out sDocumentID);

        //                    oDocument.DocumentID = sDocumentID;
        //                }

        //                if (iLogged == 0)
        //                {
        //                    Logging.WriteToLog("Cannot Log Document With ObjectCode: " + oDocument.ObjectCode + " And DocNum:" + oDocument.DocNum + "", Logging.LogStatus.START);
        //                }
        //                else
        //                {
        //                    if (string.IsNullOrEmpty(oDocument.DocumentID))
        //                    {
        //                        string sDocumentID = "";
        //                        BoDAL.GetDocumentID(oDocument.ObjectCode, oDocument.DocNum, out sDocumentID);

        //                        oDocument.DocumentID = sDocumentID;
        //                    }

        //                    //Logging.WriteToLog("ElectronicInvoicingMethods.SendClass.LogCall", Logging.LogStatus.START);
        //                    iTempResult += this.LogCall(LogTypes.lg_TranAdd, this.ListDocuments[i]);
        //                    //Logging.WriteToLog("ElectronicInvoicingMethods.SendClass.LogCall", Logging.LogStatus.END);

        //                    //Logging.WriteToLog("ElectronicInvoicingMethods.SendClass.LogCall", Logging.LogStatus.START);
        //                    iTempResult += this.LogCall(LogTypes.lg_LoadTransactionCode, this.ListDocuments[i]);
        //                    //Logging.WriteToLog("ElectronicInvoicingMethods.SendClass.LogCall", Logging.LogStatus.END);

        //                    if (iTempResult == 2)
        //                    {
        //                        oDocument.TRANCodePK = this.TRANCode;
        //                    }

        //                    //Logging.WriteToLog("ElectronicInvoicingMethods.SendClass.PostDocument", Logging.LogStatus.START);
        //                    iTempResult += this.PostDocument(ref oDocument);
        //                    //Logging.WriteToLog("ElectronicInvoicingMethods.SendClass.PostDocument", Logging.LogStatus.END);
        //                }
        //                if (iTempResult == iTempSuccess)
        //                {
        //                    iResult++;
        //                }
        //            }

        //            if (iResult == iSuccess)
        //            {
        //                iRetVal++;
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            var a = new Logging("ElectronicInvoicingMethods.SendClass.PostDocumentProcess", ex);
        //        }
        //        return iRetVal;
        //    }

        //    /// <summary>
        //    /// Αποστολή Εγγράφου στην Epsilon
        //    /// </summary>
        //    /// <param name="_oDocument">Το Bo Παραστατικό</param>
        //    /// <returns>1 For Success, 0 For Failure</returns>
        //    private int PostDocument(ref BoDocument _oDocument)
        //    {
        //        int iRetVal = 0;
        //        try
        //        {
        //            BoEpsilonDocumentWrapper oDocumentWrapper = new BoEpsilonDocumentWrapper();
        //            BoEpsilonDocumentReply oReply = new BoEpsilonDocumentReply();

        //            BoEpsilonDocument oTmpDocument = new BoEpsilonDocument();
        //            oTmpDocument = _oDocument.EpsilonDocument;

        //            Logging.WriteToLog("ElectronicInvoicingMethods.Prepare", Logging.LogStatus.START);
        //            _oDocument.EpsilonDocumentWrapper.Prepare(oTmpDocument, _oDocument.ObjectCode);
        //            Logging.WriteToLog("ElectronicInvoicingMethods.Prepare", Logging.LogStatus.END);

        //            string sJSON = new JavaScriptSerializer().Serialize(_oDocument.EpsilonDocument);

        //            _oDocument.JSON2Send = sJSON;
        //            oDocumentWrapper = _oDocument.EpsilonDocumentWrapper;

        //            Logging.WriteToLog("ElectronicInvoicingMethods.Prepare", Logging.LogStatus.START);
        //            this.APIConnector.Issue(oDocumentWrapper, out oReply, this.TRANCode);
        //            Logging.WriteToLog("ElectronicInvoicingMethods.Prepare", Logging.LogStatus.END);

        //            sJSON = new JavaScriptSerializer().Serialize(oReply);
        //            _oDocument.JSONResponse = sJSON;

        //            if (string.IsNullOrEmpty(oReply.errorCode)) //Success
        //            {
        //                BoDAL.UpdateTransactionResult(_oDocument.DocumentID, _oDocument.TRANCodePK, "1", _oDocument.JSONResponse, oReply.documentId);
        //                Console.WriteLine("Επιτυχής Αποστολή του Εγγράφου, Τύπου: " + _oDocument.ObjectCode + " με Κωδικό: " + _oDocument.DocNum + "");

        //                BoDAL.UpdateEpsilonDocumentCode(_oDocument.DocumentID, oReply.documentId, oReply.qrCode);
        //                iRetVal++;
        //            }
        //            else //Failure
        //            {
        //                BoDAL.UpdateTransactionResult(_oDocument.DocumentID, _oDocument.TRANCodePK, "0", _oDocument.JSONResponse, oReply.documentId);
        //                Console.WriteLine("Σφάλμα Κατά την Αποστολή του Εγγράφου, Τύπου: " + _oDocument.ObjectCode + " με Κωδικό: " + _oDocument.DocNum + "");
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            var a = new Logging("ElectronicInvoicingMethods.SendClass.PostDocument", ex);
        //        }
        //        return iRetVal;
        //    }

        //    /// <summary>
        //    /// Σύνδεση με το API
        //    /// </summary>
        //    /// <returns>1 For Success, 0 For Failure</returns>
        //    private int Connect2API()
        //    {
        //        int iRetVal = 0;
        //        try
        //        {
        //            this.APIConnector = new BoTransmission(this.APPICallID);
        //            iRetVal = this.APIConnector.Login();
        //        }
        //        catch (Exception ex)
        //        {
        //            var a = new Logging("ElectronicInvoicingMethods.SendClass.Connect2API", ex);
        //        }
        //        return iRetVal;
        //    }

        //    /// <summary>
        //    /// Ενημέρωση SAP Business One Παραστατικού / Ημ. Εγγραφής
        //    /// </summary>
        //    /// <param name="_oDocument">Αντικείμενο Bo Παραστατικού</param>
        //    /// <returns>1 For Success, 0 For Failure</returns>
        //    private int UpdateSAP(ref BoDocument _oDocument)
        //    {
        //        int iRetVal = 0;
        //        try
        //        {
        //            SAPbobsCOM.Documents oDIDocument = (SAPbobsCOM.Documents)Connection.oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oInvoices);
        //            oDIDocument.GetByKey(int.Parse(_oDocument.GetDocEntry()));

        //            oDIDocument.EDocNum = "MARK";

        //            int iDIResult = oDIDocument.Update();

        //            if (iDIResult == 0)
        //            {
        //                iRetVal++;
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            var a = new Logging("ElectronicInvoicingMethods.SendClass.PostDocument", ex);
        //        }
        //        return iRetVal;
        //    }

        //    /// <summary>
        //    /// Καταγραφή στον Πίνακα Κινήσεων
        //    /// </summary>
        //    /// <param name="_enType">Τύπος Καταγραφής</param>
        //    /// <param name="_oDocument">Παραστατικό</param>
        //    /// <returns>1 For Success, 0 For Failure</returns>
        //    private int LogCall(LogTypes _enType, BoDocument _oDocument)
        //    {
        //        int iRetVal = 0;
        //        try
        //        {
        //            switch (_enType)
        //            {
        //                case LogTypes.lg_APICall:
        //                    BoDAL.LogNewCall(this.UserSign);
        //                    break;
        //                case LogTypes.lg_TranAdd:
        //                    BoDAL.LogNewTransaction(this.APPICallID, _oDocument);
        //                    break;
        //                case LogTypes.lg_TranUpdate:
        //                    BoDAL.LogUpdateTransaction();
        //                    break;
        //                case LogTypes.lg_LoadAPICall:
        //                    string sAPICallID = "";
        //                    BoDAL.LoadAPICallID(this.UserSign, out sAPICallID);

        //                    this.APPICallID = sAPICallID;
        //                    break;
        //                case LogTypes.lg_LoadTransactionCode:
        //                    string sTranCode = "";
        //                    BoDAL.LoadTransactionCodeID(this.APPICallID, _oDocument.DocNum, out sTranCode);

        //                    this.TRANCode = sTranCode;
        //                    break;
        //            }
        //            iRetVal++;
        //        }
        //        catch (Exception ex)
        //        {
        //            var a = new Logging("ElectronicInvoicingMethods.SendClass.LogCall", ex);
        //        }
        //        return iRetVal;
        //    }
        //    #endregion

        //    #region Public Methods
        //    public int Exec(TransactionTypes _enType)
        //    {
        //        int iRetVal = 0;
        //        try
        //        {
        //            CommonLibrary.Ini.IniFile ini = new CommonLibrary.Ini.IniFile("C:\\Program Files\\sap\\ServicesLogs\\Connection.ini");

        //            int iResult = 0;
        //            int iSuccess = 4;
        //            this.UserSign = ini.IniReadValue("Default", "B1UserName");

        //            //Logging.WriteToLog("ElectronicInvoicingMethods.SendClass.LogCall", Logging.LogStatus.START);
        //            iResult += this.LogCall(LogTypes.lg_APICall, null);
        //            //Logging.WriteToLog("ElectronicInvoicingMethods.SendClass.LogCall", Logging.LogStatus.END);

        //            //Logging.WriteToLog("ElectronicInvoicingMethods.SendClass.LogCall", Logging.LogStatus.START);
        //            iResult += this.LogCall(LogTypes.lg_LoadAPICall, null);
        //            //Logging.WriteToLog("ElectronicInvoicingMethods.SendClass.LogCall", Logging.LogStatus.END);

        //            if (iResult == 2)
        //            {
        //                //Logging.WriteToLog("ElectronicInvoicingMethods.SendClass.Connect2API", Logging.LogStatus.START);
        //                iResult += this.Connect2API();
        //                //Logging.WriteToLog("ElectronicInvoicingMethods.SendClass.Connect2API", Logging.LogStatus.END);
        //            }

        //            if (iResult == 3)
        //            {
        //                //Logging.WriteToLog("ElectronicInvoicingMethods.SendClass.PostDocumentProcess", Logging.LogStatus.START);
        //                iResult += this.PostDocumentProcess();
        //                //Logging.WriteToLog("ElectronicInvoicingMethods.SendClass.PostDocumentProcess", Logging.LogStatus.END);
        //            }

        //            if (iSuccess == iResult)
        //            {
        //                iRetVal++;
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            var a = new Logging("ElectronicInvoicingMethods.SendClass.Exec", ex);
        //        }
        //        return iRetVal;
        //    }
        //    #endregion
        //}
        #endregion
    }
}
