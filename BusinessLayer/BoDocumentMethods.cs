using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonLibrary.ExceptionHandling;

namespace SAPmyDataService.BusinessLayer
{
    public class BoDocumentMethods
    {
        #region Public Properties
        #endregion

        public BoDocumentMethods()
        { }

        #region Public Methods
        public int BuildObject(ref BoDocument _oDocument)
        {
            int iRetVal = 0;
            try
            {
                _oDocument.AADEDocument = new InvoicesDoc();
                _oDocument.AADEDocument.invoice = new List<AadeBookInvoiceType>();
                AadeBookInvoiceType oInvoiceType = new AadeBookInvoiceType();

                oInvoiceType.invoiceHeader = new InvoiceHeaderType();
                oInvoiceType.invoiceHeader = this.GetInvoiceHeader(ref _oDocument);

                if (_oDocument.ObjType != "30")
                {
                    oInvoiceType.paymentMethods = new List<PaymentMethodDetailType>();
                    oInvoiceType.paymentMethods = this.GetPaymentMethods(_oDocument);

                    oInvoiceType.counterpart = new PartyType();
                    oInvoiceType.counterpart = this.GetCounterPart(_oDocument);
                }

                oInvoiceType.issuer = new PartyType();
                oInvoiceType.issuer = this.GetIssuer();

                //if (_oDocument.VATLocation != "GR")
                if (_oDocument.ContainTaxes == true && _oDocument.TaxAmount > 0)
                {
                    //oInvoiceType.taxesTotals = new List<TaxTotalsType>();
                    List<TaxTotalsType> ListRet = new List<TaxTotalsType>();
                    ListRet = this.GetTaxesTotals(ref _oDocument);
                    oInvoiceType.taxesTotals = ListRet.ToArray();
                }

                //details
                oInvoiceType.invoiceDetails = new List<InvoiceRowType>();
                oInvoiceType.invoiceDetails = this.GetDetails(_oDocument);



                oInvoiceType.invoiceSummary = new InvoiceSummaryType();
                oInvoiceType.invoiceSummary = this.GetInvoiceSummary(_oDocument);

                _oDocument.AADEDocument.invoice.Add(oInvoiceType);
                iRetVal++;
            }
            catch (Exception ex)
            {
                var a = new Logging("BoDocumentMethods.BuildObject", ex);
            }
            return iRetVal;
        }
        #endregion

        #region Private Methods
        //private List<InvoiceRowType> GetDetails(BoDocument _oDocument)
        //{
        //    List<InvoiceRowType> oRet = new List<InvoiceRowType>();
        //    string sSQL = "";
        //    try
        //    {
        //        InvoiceRowType oRow = null;
        //        IncomeClassificationType oIncomeClassificationType = null;

        //        sSQL = "SELECT * FROM TKA_F_SELECT_MY_DATA_JOURNAL_DATA('" + _oDocument.ObjType + "', '" + _oDocument.DocEntry + "', '" + _oDocument.TransId + "')";
        //        string sFileLocation = AppDomain.CurrentDomain.BaseDirectory + "\\" + "ConfParams.ini";
        //        CommonLibrary.Ini.IniFile ini = new CommonLibrary.Ini.IniFile(sFileLocation);

        //        string sConnectionString = ini.IniReadValue("Default", "SAPConnectionString");
        //        sConnectionString = sConnectionString.Replace("#DB_NAME", _oDocument.CompanyDB);

        //        using (SqlConnection oConnection = new SqlConnection(sConnectionString))
        //        {
        //            oConnection.Open();
        //            DataTable dtJE = new DataTable();
        //            using (SqlDataAdapter oSQLAdapter = new SqlDataAdapter(sSQL, oConnection))
        //            {
        //                //oSQLAdapter.SelectCommand.Parameters.AddWithValue("@USERNAME", "manager");
        //                oSQLAdapter.SelectCommand.CommandTimeout = 0;
        //                //SQLAdapter.SelectCommand.CommandType = CommandType.;
        //                oSQLAdapter.Fill(dtJE);
        //            }

        //            int iRow = 0;
        //            foreach (DataRow dtRow in dtJE.Rows)
        //            {
        //                iRow++;
        //                oRow = new InvoiceRowType();
        //                #region Required
        //                oRow.lineNumber = iRow;
        //                oRow.netValue = 896.74;// double.Parse(dtRow["Amount"].ToString());
        //                oRow.vatCategory = int.Parse(dtRow["VATCategory"].ToString());
        //                oRow.vatAmount = double.Parse(dtRow["VATAmount"].ToString());
        //                #endregion

        //                #region Not Required
        //                //oRow.quantity = 1;
        //                oRow.quantitySpecified = false;
        //                //oRow.measurementUnit = 1;
        //                oRow.measurementUnitSpecified = false;
        //                //oRow.invoiceDetailType = 1;
        //                oRow.invoiceDetailTypeSpecified = false;

        //                if (int.Parse(dtRow["VATCategory"].ToString()) == 7)
        //                {
        //                    oRow.vatExemptionCategory = int.Parse(dtRow["VATExceptionCategory"].ToString());
        //                    oRow.vatExemptionCategorySpecified = true;
        //                }
        //                else
        //                {
        //                    //oRow.vatExemptionCategory = 1;
        //                    oRow.vatExemptionCategorySpecified = false;
        //                }
        //                //oRow.dienergia = "";

        //                //oRow.discountOption = true;
        //                oRow.discountOptionSpecified = false;
        //                //oRow.withheldAmount = 0;
        //                oRow.withheldAmountSpecified = false;
        //                //oRow.withheldPercentCategory = 0;
        //                oRow.withheldPercentCategorySpecified = false;
        //                //oRow.stampDutyAmount = 0;
        //                oRow.stampDutyAmountSpecified = false;
        //                //oRow.feesAmount = 0;
        //                oRow.feesAmountSpecified = false;
        //                //oRow.feesPercentCategory = 0;
        //                oRow.feesPercentCategorySpecified = false;

        //                //oRow.otherTaxesAmount = 0;
        //                oRow.otherTaxesAmountSpecified = false;
        //                //oRow.otherTaxesPercentCategory = 1;
        //                oRow.otherTaxesPercentCategorySpecified = false;


        //                //oRow.otherTaxesAmount = 61.8;
        //                //oRow.otherTaxesAmountSpecified = true;
        //                //oRow.otherTaxesPercentCategory = 1;
        //                //oRow.otherTaxesPercentCategorySpecified = true;


        //                //oRow.deductionsAmount = 0;
        //                oRow.deductionsAmountSpecified = false;
        //                oRow.lineComments = "";

        //                oIncomeClassificationType = new IncomeClassificationType();
        //                oIncomeClassificationType.amount = 896.74;// double.Parse(dtRow["Amount"].ToString());
        //                oIncomeClassificationType.classificationCategory = this.AllocateClassificationCategory(dtRow["Classification"].ToString());
        //                oIncomeClassificationType.classificationType = this.AllocateClassificationType(dtRow["IncomeType"].ToString());
        //                oIncomeClassificationType.idSpecified = false;

        //                oRow.incomeClassification = new List<IncomeClassificationType>();
        //                oRow.incomeClassification.Add(oIncomeClassificationType);

        //                oRow.expensesClassification = null;
        //                #endregion
        //                oRet.Add(oRow);
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Logging.WriteToLog("sSQL=" + sSQL, Logging.LogStatus.RET_VAL);
        //        var a = new Logging("BoDocumentMethods.GetDetails", ex);
        //    }
        //    return oRet;
        //}
        private List<InvoiceRowType> GetDetails(BoDocument _oDocument)
        {
            List<InvoiceRowType> oRet = new List<InvoiceRowType>();
            string sSQL = "";
            try
            {
                InvoiceRowType oRow = null;
                IncomeClassificationType oIncomeClassificationType = null;

                sSQL = "SELECT * FROM TKA_F_SELECT_MY_DATA_JOURNAL_DATA_HARDCODED('" + _oDocument.TransId + "')";
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
                        if (_oDocument.ContainTaxes == true)
                        {
                            //oRow.netValue = Math.Round((double.Parse(dtRow["Amount"].ToString()) - double.Parse(dtRow["TAX_AMOUNT"].ToString())), 2);
                            oRow.netValue = double.Parse(dtRow["Amount"].ToString());
                        }
                        else
                        {
                            oRow.netValue = double.Parse(dtRow["Amount"].ToString());
                        }
                        oRow.vatCategory = int.Parse(dtRow["VATCategory"].ToString());
                        oRow.vatAmount = double.Parse(dtRow["VATAmount"].ToString());
                        #endregion

                        #region Not Required
                        //oRow.quantity = 1;
                        oRow.quantitySpecified = false;
                        //oRow.measurementUnit = 1;
                        oRow.measurementUnitSpecified = false;
                        //oRow.invoiceDetailType = 1;
                        oRow.invoiceDetailTypeSpecified = false;

                        if (int.Parse(dtRow["VATCategory"].ToString()) == 7)
                        {
                            oRow.vatExemptionCategory = int.Parse(dtRow["VATExceptionCategory"].ToString());
                            oRow.vatExemptionCategorySpecified = true;
                        }
                        else
                        {
                            //oRow.vatExemptionCategory = 1;
                            oRow.vatExemptionCategorySpecified = false;
                        }
                        //oRow.dienergia = "";

                        //oRow.discountOption = true;
                        oRow.discountOptionSpecified = false;
                        //oRow.withheldAmount = 0;
                        oRow.withheldAmountSpecified = false;
                        //oRow.withheldPercentCategory = 0;
                        oRow.withheldPercentCategorySpecified = false;
                        //oRow.stampDutyAmount = 0;
                        oRow.stampDutyAmountSpecified = false;
                        //oRow.feesAmount = 0;
                        oRow.feesAmountSpecified = false;
                        //oRow.feesPercentCategory = 0;
                        oRow.feesPercentCategorySpecified = false;

                        //oRow.otherTaxesAmount = 0;
                        oRow.otherTaxesAmountSpecified = false;
                        //oRow.otherTaxesPercentCategory = 1;
                        oRow.otherTaxesPercentCategorySpecified = false;

                        //oRow.otherTaxesAmount = 61.8;
                        //oRow.otherTaxesAmountSpecified = true;
                        //oRow.otherTaxesPercentCategory = 1;
                        //oRow.otherTaxesPercentCategorySpecified = true;


                        //oRow.deductionsAmount = 0;
                        oRow.deductionsAmountSpecified = false;

                        oRow.lineComments = "";

                        oIncomeClassificationType = new IncomeClassificationType();
                        oIncomeClassificationType.amount = double.Parse(dtRow["Amount"].ToString());
                        oIncomeClassificationType.classificationCategory = this.AllocateClassificationCategory(dtRow["Classification"].ToString());
                        oIncomeClassificationType.classificationType = this.AllocateClassificationType(dtRow["IncomeType"].ToString());
                        oIncomeClassificationType.idSpecified = false;

                        oRow.incomeClassification = new List<IncomeClassificationType>();
                        oRow.incomeClassification.Add(oIncomeClassificationType);

                        oRow.expensesClassification = null;
                        #endregion
                        oRet.Add(oRow);
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.WriteToLog("sSQL=" + sSQL, Logging.LogStatus.RET_VAL);
                var a = new Logging("BoDocumentMethods.GetDetails", ex);
            }
            return oRet;
        }
        private PartyType GetIssuer()
        {
            PartyType oRet = new PartyType();
            try
            {
                string sFileLocation = AppDomain.CurrentDomain.BaseDirectory + "\\" + "ConfParams.ini";
                CommonLibrary.Ini.IniFile ini = new CommonLibrary.Ini.IniFile(sFileLocation);

                string sVATNumber = ini.IniReadValue("Default", "ISSUER_VAT_ID");

                oRet.vatNumber = sVATNumber;
                oRet.country = CountryType.GR;
                oRet.branch = 0;
            }
            catch (Exception ex)
            {
                var a = new Logging("BoDocumentMethods.GetIssuer", ex);
            }
            return oRet;
        }
        private PartyType GetCounterPart(BoDocument _oDocument)
        {
            PartyType oRet = new PartyType();
            try
            {
                if (_oDocument.ContainTaxes == false) //εσωτερικού
                {
                    oRet.vatNumber = _oDocument.AFM;
                    //oRet.country = CountryType.GR;
                    oRet.country = (CountryType)Enum.Parse(typeof(CountryType), _oDocument.VATLocation);
                    oRet.branch = 0;
                    //oRet.address = new AddressType();
                    //oRet.address.city = "ΑΘΗΝΑ";
                    //oRet.address.postalCode = "17341";
                }
                else
                {
                    oRet.name = _oDocument.CardName;
                    oRet.country = (CountryType)Enum.Parse(typeof(CountryType), _oDocument.BPCountry);
                    oRet.vatNumber = "000000000";
                    oRet.branch = 0;
                    oRet.address = new AddressType();
                    oRet.address.city = _oDocument.BPCountry;
                    oRet.address.street = "abc";
                    oRet.address.postalCode = "1111";
                }
            }
            catch (Exception ex)
            {
                var a = new Logging("BoDocumentMethods.GetCounterPart", ex);
            }
            return oRet;
        }
        //private InvoiceHeaderType GetInvoiceHeader(ref BoDocument _oDocument)
        //{
        //    InvoiceHeaderType oRet = new InvoiceHeaderType();
        //    string sSQL = "";
        //    try
        //    {
        //        string sInvoiceType = "";
        //        string sTransID = "";
        //        string sSeries = "";
        //        string sAFM = "";
        //        string sContract = "";
        //        string sVATLocation = "";
        //        string sSalesOrderDocNum = "";
        //        int iPaymentMethod = -1;
        //        double dAmount = 0.00;
        //        DateTime dtRefDate = DateTime.Now;

        //        string sFileLocation = AppDomain.CurrentDomain.BaseDirectory + "\\" + "ConfParams.ini";
        //        CommonLibrary.Ini.IniFile ini = new CommonLibrary.Ini.IniFile(sFileLocation);

        //        string sConnectionString = ini.IniReadValue("Default", "SAPConnectionString");
        //        sConnectionString = sConnectionString.Replace("#DB_NAME", _oDocument.CompanyDB);

        //        switch (_oDocument.DocumentScenario)
        //        {
        //            case Enumerators.otCases.Local:
        //                #region Enumerators.otCases.Local
        //                sSQL = "SELECT * FROM TKA_F_SELECT_MY_DATA_JOURNAL_DATA('" + _oDocument.ObjType + "', '" + _oDocument.DocEntry + "', '" + _oDocument.TransId + "')";
        //                using (SqlConnection oConnection = new SqlConnection(sConnectionString))
        //                {
        //                    oConnection.Open();
        //                    DataTable dtJE = new DataTable();
        //                    using (SqlDataAdapter oSQLAdapter = new SqlDataAdapter(sSQL, oConnection))
        //                    {
        //                        //oSQLAdapter.SelectCommand.Parameters.AddWithValue("@USERNAME", "manager");
        //                        oSQLAdapter.SelectCommand.CommandTimeout = 0;
        //                        //SQLAdapter.SelectCommand.CommandType = CommandType.;
        //                        oSQLAdapter.Fill(dtJE);
        //                    }

        //                    foreach (DataRow dtRow in dtJE.Rows)
        //                    {
        //                        sInvoiceType = dtRow["InvoiceType"].ToString();
        //                        dtRefDate = DateTime.Parse(dtRow["RefDate"].ToString());
        //                        sTransID = dtRow["TransId"].ToString();
        //                        sSeries = dtRow["TransId"].ToString();
        //                        sAFM = dtRow["LicTradNum"].ToString().Replace("EL", "");
        //                        sContract = dtRow["ContractNo"].ToString();
        //                        sSalesOrderDocNum = dtRow["SalesOrderDocNum"].ToString();
        //                        iPaymentMethod = int.Parse(dtRow["PaymentMethod"].ToString());
        //                        dAmount = double.Parse(dtRow["Amount"].ToString());
        //                        sVATLocation = dtRow["VATLocation"].ToString();
        //                    }

        //                    //Τα βάζω στο αντικείμενο σε μεταβλητές ώστε να μην τα φορτώνω εκ νέου
        //                    _oDocument.PaymentMethod = iPaymentMethod;
        //                    _oDocument.Amount = dAmount;
        //                    _oDocument.AFM = sAFM;
        //                    _oDocument.ContractNo = sContract;
        //                    _oDocument.VATLocation = sVATLocation;
        //                    _oDocument.LoadRelatedJournalEntries(sSalesOrderDocNum);
        //                }

        //                #region Required
        //                //oRet.aa = sTransID;
        //                oRet.aa = "1111";//sContract;
        //                oRet.series = sSeries;
        //                oRet.issueDate = dtRefDate;
        //                oRet.invoiceType = this.AllocateInvoiceType(sInvoiceType);
        //                #endregion

        //                #region Not Required
        //                //oRet.correlatedInvoices = "";
        //                oRet.currency = CurrencyType.EUR;
        //                oRet.currencySpecified = true;

        //                if (_oDocument.ObjType != "30")
        //                {
        //                    oRet.dispatchDate = DateTime.Now;
        //                    oRet.dispatchDateSpecified = true;
        //                }
        //                else
        //                {
        //                    oRet.dispatchDateSpecified = false;
        //                }
        //                //oRet.dispatchTime
        //                oRet.dispatchTimeSpecified = false;
        //                oRet.exchangeRate = 0;
        //                oRet.exchangeRateSpecified = false;
        //                //oRet.movePurpose = 1;
        //                oRet.movePurposeSpecified = false;
        //                oRet.selfPricing = false;
        //                oRet.selfPricingSpecified = false;
        //                oRet.vatPaymentSuspension = false;
        //                oRet.vatPaymentSuspensionSpecified = false;

        //                if (_oDocument.ObjType != "30")
        //                {
        //                    oRet.vehicleNumber = "";
        //                }
        //                #endregion
        //                #endregion
        //                break;
        //        }





        //    }
        //    catch (Exception ex)
        //    {
        //        Logging.WriteToLog("sSQL=" + sSQL, Logging.LogStatus.RET_VAL);
        //        var a = new Logging("BoDocumentMethods.GetInvoiceHeader", ex);
        //    }
        //    return oRet;
        //}

        private InvoiceHeaderType GetInvoiceHeader(ref BoDocument _oDocument)
        {
            InvoiceHeaderType oRet = new InvoiceHeaderType();
            string sSQL = "";
            try
            {
                string sInvoiceType = "";
                string sTransID = "";
                string sSeries = "";
                string sAFM = "";
                string sContract = "";
                string sVATLocation = "";
                string sSalesOrderDocNum = "";
                string sCardName = "";
                string sBPCountry = "";
                double dTaxAmount = 0.00;
                int iPaymentMethod = -1;
                double dAmount = 0.00;
                DateTime dtRefDate = DateTime.Now;

                string sFileLocation = AppDomain.CurrentDomain.BaseDirectory + "\\" + "ConfParams.ini";
                CommonLibrary.Ini.IniFile ini = new CommonLibrary.Ini.IniFile(sFileLocation);

                string sConnectionString = ini.IniReadValue("Default", "SAPConnectionString");
                sConnectionString = sConnectionString.Replace("#DB_NAME", _oDocument.CompanyDB);

                //switch (_oDocument.DocumentScenario)
                //{
                //    case Enumerators.otCases.Local:
                //        #region Enumerators.otCases.Local
                //        sSQL = "SELECT * FROM TKA_F_SELECT_MY_DATA_JOURNAL_DATA_HARDCODED('" + _oDocument.TransId + "')";
                //        using (SqlConnection oConnection = new SqlConnection(sConnectionString))
                //        {
                //            oConnection.Open();
                //            DataTable dtJE = new DataTable();
                //            using (SqlDataAdapter oSQLAdapter = new SqlDataAdapter(sSQL, oConnection))
                //            {
                //                //oSQLAdapter.SelectCommand.Parameters.AddWithValue("@USERNAME", "manager");
                //                oSQLAdapter.SelectCommand.CommandTimeout = 0;
                //                //SQLAdapter.SelectCommand.CommandType = CommandType.;
                //                oSQLAdapter.Fill(dtJE);
                //            }

                //            foreach (DataRow dtRow in dtJE.Rows)
                //            {
                //                dTaxAmount = 0; double.Parse(dtRow["TAX_AMOUNT"].ToString());
                //                sBPCountry = dtRow["VATLocation"].ToString();
                //                sCardName = dtRow["CardName"].ToString();
                //                sInvoiceType = dtRow["InvoiceType"].ToString();
                //                dtRefDate = DateTime.Parse(dtRow["RefDate"].ToString());
                //                sTransID = dtRow["TransId"].ToString();
                //                sSeries = dtRow["TransId"].ToString();
                //                sAFM = dtRow["LicTradNum"].ToString().Replace("EL", "");
                //                sContract = dtRow["ContractNo"].ToString();
                //                sSalesOrderDocNum = dtRow["SalesOrderDocNum"].ToString();
                //                iPaymentMethod = int.Parse(dtRow["PaymentMethod"].ToString());
                //                dAmount = double.Parse(dtRow["Amount"].ToString());
                //                sVATLocation = dtRow["VATLocation"].ToString();
                //            }

                //            //Τα βάζω στο αντικείμενο σε μεταβλητές ώστε να μην τα φορτώνω εκ νέου
                //            _oDocument.PaymentMethod = iPaymentMethod;
                //            _oDocument.BPCountry = sBPCountry;
                //            _oDocument.CardName = sCardName;
                //            _oDocument.Amount = dAmount;
                //            _oDocument.AFM = sAFM;
                //            _oDocument.ContractNo = sContract;
                //            _oDocument.VATLocation = sVATLocation;
                //            _oDocument.TaxAmount = dTaxAmount;
                //            _oDocument.LoadRelatedJournalEntries(sSalesOrderDocNum);
                //        }

                //        #region Required
                //        //oRet.aa = sTransID;
                //        oRet.aa = sContract;
                //        oRet.series = sSeries;
                //        oRet.issueDate = dtRefDate;
                //        oRet.invoiceType = this.AllocateInvoiceType(sInvoiceType);
                //        #endregion

                //        #region Not Required
                //        //oRet.correlatedInvoices = "";
                //        oRet.currency = CurrencyType.EUR;
                //        oRet.currencySpecified = true;

                //        if (_oDocument.ObjType != "30")
                //        {
                //            oRet.dispatchDate = DateTime.Now;
                //            oRet.dispatchDateSpecified = true;
                //        }
                //        else
                //        {
                //            oRet.dispatchDateSpecified = false;
                //        }
                //        //oRet.dispatchTime
                //        oRet.dispatchTimeSpecified = false;
                //        oRet.exchangeRate = 0;
                //        oRet.exchangeRateSpecified = false;
                //        //oRet.movePurpose = 1;
                //        oRet.movePurposeSpecified = false;
                //        oRet.selfPricing = false;
                //        oRet.selfPricingSpecified = false;
                //        oRet.vatPaymentSuspension = false;
                //        oRet.vatPaymentSuspensionSpecified = false;

                //        if (_oDocument.ObjType != "30")
                //        {
                //            oRet.vehicleNumber = "";
                //        }
                //        #endregion
                //        #endregion
                //        break;
                //}
            }
            catch (Exception ex)
            {
                Logging.WriteToLog("sSQL=" + sSQL, Logging.LogStatus.RET_VAL);
                var a = new Logging("BoDocumentMethods.GetInvoiceHeader", ex);
            }
            return oRet;
        }
        private InvoiceType AllocateInvoiceType(string _sType)
        {
            InvoiceType oRet = InvoiceType.Item11;
            switch (_sType)
            {
                case "Item11":
                    oRet = InvoiceType.Item11;
                    break;
                case "Item12":
                    oRet = InvoiceType.Item12;
                    break;
                case "Item13":
                    oRet = InvoiceType.Item13;
                    break;
                case "Item14":
                    oRet = InvoiceType.Item14;
                    break;
                case "Item15":
                    oRet = InvoiceType.Item15;
                    break;
                case "Item16":
                    oRet = InvoiceType.Item16;
                    break;
                case "Item21":
                    oRet = InvoiceType.Item21;
                    break;
                case "Item22":
                    oRet = InvoiceType.Item22;
                    break;
                case "Item23":
                    oRet = InvoiceType.Item23;
                    break;
                case "Item24":
                    oRet = InvoiceType.Item24;
                    break;
                case "Item31":
                    oRet = InvoiceType.Item31;
                    break;
                case "Item32":
                    oRet = InvoiceType.Item32;
                    break;
                case "Item4 ":
                    oRet = InvoiceType.Item4;
                    break;
                case "Item51":
                    oRet = InvoiceType.Item51;
                    break;
                case "Item52":
                    oRet = InvoiceType.Item52;
                    break;
                case "Item61":
                    oRet = InvoiceType.Item61;
                    break;
                case "Item62":
                    oRet = InvoiceType.Item62;
                    break;
                case "Item71":
                    oRet = InvoiceType.Item71;
                    break;
                case "Item81":
                    oRet = InvoiceType.Item81;
                    break;
                case "Item82":
                    oRet = InvoiceType.Item82;
                    break;
                case "Item111":
                    oRet = InvoiceType.Item111;
                    break;
                case "Item112":
                    oRet = InvoiceType.Item112;
                    break;
                case "Item113":
                    oRet = InvoiceType.Item113;
                    break;
                case "Item114":
                    oRet = InvoiceType.Item114;
                    break;
                case "Item115":
                    oRet = InvoiceType.Item115;
                    break;
                case "Item121":
                    oRet = InvoiceType.Item121;
                    break;
                case "Item131":
                    oRet = InvoiceType.Item131;
                    break;
                case "Item132":
                    oRet = InvoiceType.Item132;
                    break;
                case "Item133":
                    oRet = InvoiceType.Item133;
                    break;
                case "Item134":
                    oRet = InvoiceType.Item134;
                    break;
                case "Item1330":
                    oRet = InvoiceType.Item1330;
                    break;
                case "Item1331":
                    oRet = InvoiceType.Item1331;
                    break;
                case "Item141":
                    oRet = InvoiceType.Item141;
                    break;
                case "Item142":
                    oRet = InvoiceType.Item142;
                    break;
                case "Item143":
                    oRet = InvoiceType.Item143;
                    break;
                case "Item144":
                    oRet = InvoiceType.Item144;
                    break;
                case "Item145":
                    oRet = InvoiceType.Item145;
                    break;
                case "Item1430":
                    oRet = InvoiceType.Item1430;
                    break;
                case "Item1431":
                    oRet = InvoiceType.Item1431;
                    break;
                case "Item151":
                    oRet = InvoiceType.Item151;
                    break;
                case "Item161":
                    oRet = InvoiceType.Item161;
                    break;
                case "Item171":
                    oRet = InvoiceType.Item171;
                    break;
                case "Item172":
                    oRet = InvoiceType.Item172;
                    break;
                case "Item173":
                    oRet = InvoiceType.Item173;
                    break;
                case "Item174":
                    oRet = InvoiceType.Item174;
                    break;
                case "Item175":
                    oRet = InvoiceType.Item175;
                    break;
                case "Item176":
                    oRet = InvoiceType.Item176;
                    break;
            }
            return oRet;
        }
        //private InvoiceSummaryType GetInvoiceSummary(BoDocument _oDocument)
        //{
        //    InvoiceSummaryType oRet = new InvoiceSummaryType();
        //    string sSQL = "";
        //    try
        //    {
        //        IncomeClassificationType oIncomeClassificationType = null;
        //        oRet = new InvoiceSummaryType();
        //        oRet.incomeClassification = new List<IncomeClassificationType>();

        //        double dTotal = 0;

        //        sSQL = "SELECT " + Environment.NewLine +
        //            " Classification," + Environment.NewLine +
        //            " IncomeType," + Environment.NewLine +
        //            " Amount = SUM(Amount)" + Environment.NewLine +
        //            " FROM TKA_F_SELECT_MY_DATA_JOURNAL_DATA('" + _oDocument.ObjType + "', '" + _oDocument.DocEntry + "', '" + _oDocument.TransId + "')" + Environment.NewLine +
        //            " GROUP BY" + Environment.NewLine +
        //            " Classification," + Environment.NewLine +
        //            " IncomeType";

        //        string sFileLocation = AppDomain.CurrentDomain.BaseDirectory + "\\" + "ConfParams.ini";
        //        CommonLibrary.Ini.IniFile ini = new CommonLibrary.Ini.IniFile(sFileLocation);

        //        string sConnectionString = ini.IniReadValue("Default", "SAPConnectionString");
        //        sConnectionString = sConnectionString.Replace("#DB_NAME", _oDocument.CompanyDB);

        //        using (SqlConnection oConnection = new SqlConnection(sConnectionString))
        //        {
        //            oConnection.Open();
        //            DataTable dtJE = new DataTable();
        //            using (SqlDataAdapter oSQLAdapter = new SqlDataAdapter(sSQL, oConnection))
        //            {
        //                //oSQLAdapter.SelectCommand.Parameters.AddWithValue("@USERNAME", "manager");
        //                oSQLAdapter.SelectCommand.CommandTimeout = 0;
        //                //SQLAdapter.SelectCommand.CommandType = CommandType.;
        //                oSQLAdapter.Fill(dtJE);
        //            }

        //            foreach (DataRow dtRow in dtJE.Rows)
        //            {
        //                dTotal += double.Parse(dtRow["Amount"].ToString());

        //                oIncomeClassificationType = new IncomeClassificationType();
        //                oIncomeClassificationType.amount = 896.74;// double.Parse(dtRow["Amount"].ToString());
        //                oIncomeClassificationType.classificationCategory = this.AllocateClassificationCategory(dtRow["Classification"].ToString());
        //                oIncomeClassificationType.classificationType = this.AllocateClassificationType(dtRow["IncomeType"].ToString());
        //                oIncomeClassificationType.idSpecified = false;

        //                oRet.incomeClassification.Add(oIncomeClassificationType);

        //            }
        //        }

        //        //***NOTE*** ALL FIELDS ARE REQUIRED!!!!
        //        oRet.totalDeductionsAmount = 0;
        //        oRet.totalFeesAmount = 0;
        //        oRet.totalGrossValue = dTotal;
        //        oRet.totalNetValue = dTotal - 134.51;// _oDocument.TotalTaxes;
        //        //oRet.totalOtherTaxesAmount = 0;
        //        oRet.totalOtherTaxesAmount = 134.51;// _oDocument.TotalTaxes;
        //        oRet.totalStampDutyAmount = 0;
        //        oRet.totalVatAmount = 0;
        //        oRet.totalWithheldAmount = 0;
        //        oRet.expensesClassification = null;
        //    }
        //    catch (Exception ex)
        //    {
        //        var a = new Logging("BoDocumentMethods.GetInvoiceSummary", ex);
        //    }
        //    return oRet;
        //}
        private InvoiceSummaryType GetInvoiceSummary(BoDocument _oDocument)
        {
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
                        oIncomeClassificationType.classificationCategory = this.AllocateClassificationCategory(dtRow["Classification"].ToString());
                        oIncomeClassificationType.classificationType = this.AllocateClassificationType(dtRow["IncomeType"].ToString());
                        oIncomeClassificationType.idSpecified = false;

                        oRet.incomeClassification.Add(oIncomeClassificationType);

                    }
                }

                //***NOTE*** ALL FIELDS ARE REQUIRED!!!!
                oRet.totalDeductionsAmount = 0;
                oRet.totalFeesAmount = 0;
                oRet.totalGrossValue = dTotal + _oDocument.TotalTaxes;
                oRet.totalNetValue = dTotal;
                oRet.totalOtherTaxesAmount = _oDocument.TotalTaxes;
                oRet.totalStampDutyAmount = 0;
                oRet.totalVatAmount = 0;
                oRet.totalWithheldAmount = 0;
                oRet.expensesClassification = null;
            }
            catch (Exception ex)
            {
                var a = new Logging("BoDocumentMethods.GetInvoiceSummary", ex);
            }
            return oRet;
        }
        //private List<TaxTotalsType> GetTaxesTotals(ref BoDocument _oDocument)
        //{
        //    List<TaxTotalsType> oRet = new List<TaxTotalsType>();
        //    string sSQL = "";
        //    try
        //    {
        //        TaxTotalsType oType = null;
        //        oRet = new List<TaxTotalsType>();

        //        double dTotal = 0;

        //        string sTransIds = string.Join(",", _oDocument.RelatedJournalEntries);

        //        sSQL = "SELECT " + Environment.NewLine +
        //            " TaxType," + Environment.NewLine +
        //            " TaxCode," + Environment.NewLine +
        //            " TotAmount = ISNULL(SUM(ISNULL(Amount, 0)), 0)" + Environment.NewLine +
        //            " FROM TKA_V_MY_DATA_SELECT_DOCUMENT_TAXES" + Environment.NewLine +
        //            " WHERE 1 = 1" + Environment.NewLine +
        //            " AND TransId IN (SELECT VALUE FROM SPLIT(',','" + sTransIds + "'))" + Environment.NewLine +
        //            " GROUP BY TaxType, TaxCode";

        //        string sFileLocation = AppDomain.CurrentDomain.BaseDirectory + "\\" + "ConfParams.ini";
        //        CommonLibrary.Ini.IniFile ini = new CommonLibrary.Ini.IniFile(sFileLocation);

        //        string sConnectionString = ini.IniReadValue("Default", "SAPConnectionString");
        //        sConnectionString = sConnectionString.Replace("#DB_NAME", _oDocument.CompanyDB);

        //        using (SqlConnection oConnection = new SqlConnection(sConnectionString))
        //        {
        //            oConnection.Open();
        //            DataTable dtJE = new DataTable();
        //            using (SqlDataAdapter oSQLAdapter = new SqlDataAdapter(sSQL, oConnection))
        //            {
        //                //oSQLAdapter.SelectCommand.Parameters.AddWithValue("@USERNAME", "manager");
        //                oSQLAdapter.SelectCommand.CommandTimeout = 0;
        //                //SQLAdapter.SelectCommand.CommandType = CommandType.;
        //                oSQLAdapter.Fill(dtJE);
        //            }

        //            foreach (DataRow dtRow in dtJE.Rows)
        //            {
        //                oType = new TaxTotalsType();
        //                oType.taxAmount = 134.51;// double.Parse(dtRow["TotAmount"].ToString());
        //                oType.taxCategorySpecified = true;
        //                oType.taxCategory = 4;// int.Parse(dtRow["TaxCode"].ToString());
        //                oType.taxType = int.Parse(dtRow["TaxType"].ToString());
        //                oType.underlyingValueSpecified = false;

        //                dTotal += double.Parse(dtRow["TotAmount"].ToString());

        //                oRet.Add(oType);
        //            }
        //        }

        //        _oDocument.TotalTaxes = dTotal;
        //    }
        //    catch (Exception ex)
        //    {
        //        var a = new Logging("BoDocumentMethods.GetInvoiceSummary", ex);
        //    }
        //    return oRet;
        //}
        private List<TaxTotalsType> GetTaxesTotals(ref BoDocument _oDocument)
        {
            List<TaxTotalsType> oRet = new List<TaxTotalsType>();
            string sSQL = "";
            try
            {
                TaxTotalsType oType = null;
                oRet = new List<TaxTotalsType>();

                double dTotal = 0;

                string sTransIds = string.Join(",", _oDocument.RelatedJournalEntries);

                sSQL = "SELECT TAX_BASE_AMOUNT, TAX_CODE, TAX_CATEGORY, TAX_AMOUNT FROM TKA_F_SELECT_MY_DATA_JOURNAL_DATA_HARDCODED('" + _oDocument.TransId + "')";

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

                        dTotal += double.Parse(dtRow["TAX_AMOUNT"].ToString());

                        oRet.Add(oType);
                    }
                }

                _oDocument.TotalTaxes = dTotal;
            }
            catch (Exception ex)
            {
                var a = new Logging("BoDocumentMethods.GetInvoiceSummary", ex);
            }
            return oRet;
        }
        private IncomeClassificationCategoryType AllocateClassificationCategory(string _sCategory)
        {
            IncomeClassificationCategoryType oRet = IncomeClassificationCategoryType.category1_1;

            switch (_sCategory)
            {
                case "category1_1": oRet = IncomeClassificationCategoryType.category1_1; break;
                case "category1_2": oRet = IncomeClassificationCategoryType.category1_2; break;
                case "category1_3": oRet = IncomeClassificationCategoryType.category1_3; break;
                case "category1_4": oRet = IncomeClassificationCategoryType.category1_4; break;
                case "category1_5": oRet = IncomeClassificationCategoryType.category1_5; break;
                case "category1_6": oRet = IncomeClassificationCategoryType.category1_6; break;
                case "category1_7": oRet = IncomeClassificationCategoryType.category1_7; break;
                case "category1_8": oRet = IncomeClassificationCategoryType.category1_8; break;
                case "category1_9": oRet = IncomeClassificationCategoryType.category1_9; break;
                case "category1_10": oRet = IncomeClassificationCategoryType.category1_10; break;
                case "category1_95": oRet = IncomeClassificationCategoryType.category1_95; break;
            }

            return oRet;
        }
        private IncomeClassificationValueType AllocateClassificationType(string _sType)
        {
            IncomeClassificationValueType oRet = IncomeClassificationValueType.E3_106;
            oRet = (IncomeClassificationValueType)Enum.Parse(typeof(IncomeClassificationValueType), _sType);

            return oRet;
        }
        private List<PaymentMethodDetailType> GetPaymentMethods(BoDocument _oDocument)
        {
            List<PaymentMethodDetailType> oRet = new List<PaymentMethodDetailType>();
            try
            {
                PaymentMethodDetailType oPayment = null;

                oPayment = new PaymentMethodDetailType();
                oPayment.amount = _oDocument.Amount;
                oPayment.type = _oDocument.PaymentMethod;
                oRet.Add(oPayment);
            }
            catch (Exception ex)
            {
                var a = new Logging("BoDocumentMethods.GetPaymentMethods", ex);
            }
            return oRet;
        }
        #endregion
    }
}
