using CommonLibrary.ExceptionHandling;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImpactElectronicInvoicing.DAL
{
    public class BoUpdateDB
    {
        #region Public Properties
        public string DocumentAA { get; set; }
        public string Company { get; set; }
        public string ObjType { get; set; }
        public int Cancel { get; set; }
        public string DocEntry { get; set; }
        public string DocNum { get; set; }
        public string XMLReply { get; set; }
        public string MARK { get; set; }
        public string UID { get; set; }
        public string Result { get; set; }
        public string ErrorCode { get; set; }
        public string ErrorDescr { get; set; }
        public string Auth { get; set; }
        public string QR { get; set; }
        public string OfflineQR { get; set; }
        public string IntegritySignature { get; set; }
        public string Signature { get; set; }
        public string Domain { get; set; }
        public string StatusCode { get; set; }

        #endregion

        public BoUpdateDB()
        {
            this.MARK = "";
            this.UID = "";
            this.XMLReply = "";
            this.Result = "";
            this.ErrorCode = "";
            this.ErrorDescr = "";
        }
        public int AddResponse(SAPbobsCOM.Company CompanyConnection)
        {
            int iRetVal = 0;
            string sSQL = "";
            try
            {
                string sFileLocation = "C:\\Program Files\\sap\\ImpactElectronicInvoicingDA\\ConfParams.ini";
                CommonLibrary.Ini.IniFile ini = new CommonLibrary.Ini.IniFile(sFileLocation);
                string sConnectionString = ini.IniReadValue("Default", "MSSQLConnectionString");
                string xml = XMLReply.Replace("'", "");
                if (CompanyConnection.DbServerType == SAPbobsCOM.BoDataServerTypes.dst_HANADB)
                {
                    sSQL = "call \"RESPONSES_INSERT_IMPACT\"(" +
                        "'" + ObjType + "'," +
                        "'" + DocEntry + "'," +
                        "'" + DocNum + "'," +
                        "'" + xml.Substring(0,Math.Min(4000,xml.Length)) +
                        "')";
                    SAPbobsCOM.Recordset oRS = CommonLibrary.Functions.Database.GetRecordSet(sSQL, CompanyConnection);
                    if (oRS == null)
                    {
                        Logging.WriteToLog("failed to insert to responses", Logging.LogStatus.ERROR);
                        Logging.WriteToLog("sSQL=" + sSQL, Logging.LogStatus.ERROR);
                    }
                }
                else
                {
                    using (SqlConnection oConnection = new SqlConnection(sConnectionString))
                    {
                        oConnection.Open();

                        using (SqlCommand oCommand = new SqlCommand("[dbo].RESPONSES_IMPACT_INSERT", oConnection))
                        {
                            oCommand.CommandTimeout = 0;
                            oCommand.Parameters.Add(new SqlParameter("@ObjType", "" + this.ObjType + ""));
                            oCommand.Parameters.Add(new SqlParameter("@DocEntry", "" + this.DocEntry + ""));
                            oCommand.Parameters.Add(new SqlParameter("@DocNum", "" + this.DocNum + ""));
                            oCommand.Parameters.Add(new SqlParameter("@XMLReply", "" + xml.Substring(0, Math.Min(4000, xml.Length)) + ""));

                            oCommand.CommandType = CommandType.StoredProcedure;

                            oCommand.ExecuteScalar();
                        }
                        oConnection.Close();
                    }
                }

                iRetVal++;
            }
            catch (Exception ex)
            {
                Logging.WriteToLog("sSQL=" + sSQL, Logging.LogStatus.RET_VAL);
                var a = new Logging("BoDAL.AddResponse", ex);
            }
            return iRetVal;
        }

        public int UpdateDocument(string _sTableName, SAPbobsCOM.Company CompanyConnection)
        {
            int iRetVal = 0;
            string sSQL = "";
            try
            {
                string sFileLocation = "C:\\Program Files\\sap\\ImpactElectronicInvoicingDA\\ConfParams.ini";
                CommonLibrary.Ini.IniFile ini = new CommonLibrary.Ini.IniFile(sFileLocation);
                string error_dscr = "";
                if (!string.IsNullOrEmpty(ErrorDescr))
                {
                    error_dscr = ErrorDescr.Replace("'", "");
                }
                if (CompanyConnection.DbServerType == SAPbobsCOM.BoDataServerTypes.dst_HANADB)
                {
                    sSQL = "call \"DOCUMENTS_UPDATE_IMPACT\"(" +
                        "'" + this.ObjType + "'," +
                        "'" + this.DocEntry + "'," +
                        "'" + MARK + "'," +
                        "'" + UID + "'," +
                        "'" + Auth + "'," +
                        "'" + QR + "'," +
                        "'" + OfflineQR + "'," +
                        "'" + IntegritySignature + "'," +
                        "'" + this.Signature + "'," +
                        "'" + Domain + "'," +
                        "'" + Result + "'," +
                        "'" + ErrorCode + "'," +
                        "'" + error_dscr + "'";
                    sSQL += ")";
                    SAPbobsCOM.Recordset oRS = CommonLibrary.Functions.Database.GetRecordSet(sSQL, CompanyConnection);

                    if (oRS == null)
                    {
                        Logging.WriteToLog("failed to update eliv_documents", Logging.LogStatus.ERROR);
                        Logging.WriteToLog("sSQL=" + sSQL, Logging.LogStatus.ERROR);
                    }
                }
                else
                {
                    string sConnectionString = ini.IniReadValue("Default", "MSSQLConnectionString");
                    sSQL = "[dbo].DOCUMENTS_UPDATE_IMPACT";
                    using (SqlConnection oConnection = new SqlConnection(sConnectionString))
                    {
                        oConnection.Open();

                        using (SqlCommand oCommand = new SqlCommand(sSQL, oConnection))
                        {
                            oCommand.CommandTimeout = 0;
                            oCommand.Parameters.Add(new SqlParameter("@OBJTYPE", "" + this.ObjType + ""));
                            oCommand.Parameters.Add(new SqlParameter("@DOCENTRY", "" + this.DocEntry + ""));
                            oCommand.Parameters.Add(new SqlParameter("@MARK", "" + this.MARK + ""));
                            oCommand.Parameters.Add(new SqlParameter("@UID", "" + this.UID + ""));
                            oCommand.Parameters.Add(new SqlParameter("@AUTH", "" + this.Auth + ""));
                            oCommand.Parameters.Add(new SqlParameter("@QR", "" + this.QR + ""));
                            oCommand.Parameters.Add(new SqlParameter("@OFFLINE_QR", "" + this.OfflineQR + ""));
                            oCommand.Parameters.Add(new SqlParameter("@INTEGRITY_SIGNATURE", "" + this.IntegritySignature + ""));
                            oCommand.Parameters.Add(new SqlParameter("@SIGNATURE", "" + this.Signature + ""));
                            oCommand.Parameters.Add(new SqlParameter("@DOMAIN", "" + this.Domain + ""));
                            oCommand.Parameters.Add(new SqlParameter("@RESULT", "" + this.Result + ""));
                            oCommand.Parameters.Add(new SqlParameter("@ERROR_CODE", "" + this.ErrorCode + ""));
                            oCommand.Parameters.Add(new SqlParameter("@ERROR_DESCR", "" + this.ErrorDescr + ""));
                            oCommand.CommandType = CommandType.StoredProcedure;
                            oCommand.ExecuteScalar();
                        }
                        oConnection.Close();
                    }
                }
                iRetVal++;
            }
            catch (Exception ex)
            {
                Logging.WriteToLog("sSQL=" + sSQL, Logging.LogStatus.RET_VAL);
                var a = new Logging("BoDAL.UpdateDocument", ex);
            }
            return iRetVal;
        }

      

        //public int UpdateDocument4Cancel(string _sTableName, SAPbobsCOM.Company CompanyConnection)
        //{
        //    int iRetVal = 0;
        //    string sSQL = "";
        //    try
        //    {
        //        string sFileLocation = "C:\\Program Files\\sap\\ImpactElectronicInvoicingDA\\ConfParams.ini";
        //        CommonLibrary.Ini.IniFile ini = new CommonLibrary.Ini.IniFile(sFileLocation);
        //        if (CompanyConnection.DbServerType == SAPbobsCOM.BoDataServerTypes.dst_HANADB)
        //        {
        //            string error_dscr = "";
        //            if (!string.IsNullOrEmpty(ErrorDescr))
        //            {
        //                error_dscr = ErrorDescr.Replace("'", "");
        //            }
        //            sSQL = "call \"" + _sTableName + "_UPDATE_CANCEL_FLOW\"(" +
        //                "'" + DocumentAA + "'," +
        //                "'" + CANCELLED_MARK + "'," +
        //                "'" + Result + "'," +
        //                "'" + ErrorCode + "'," +
        //                "'" + error_dscr
        //                + "')";
        //            SAPbobsCOM.Recordset oRS = CommonLibrary.Functions.Database.GetRecordSet(sSQL, CompanyConnection);
        //        }
        //        else
        //        {
        //            string sConnectionString = ini.IniReadValue("Default", "MSSQLConnectionString");
        //            sSQL = "[dbo]." + _sTableName + "_UPDATE_CANCEL_FLOW";
        //            using (SqlConnection oConnection = new SqlConnection(sConnectionString))
        //            {
        //                oConnection.Open();

        //                using (SqlCommand oCommand = new SqlCommand(sSQL, oConnection))
        //                {
        //                    oCommand.CommandTimeout = 0;
        //                    oCommand.Parameters.Add(new SqlParameter("@DOCUMENT_AA", "" + this.DocumentAA + ""));
        //                    oCommand.Parameters.Add(new SqlParameter("@CANCELLED_MARK", "" + this.CANCELLED_MARK + ""));
        //                    oCommand.Parameters.Add(new SqlParameter("@RESULT", "" + this.Result + ""));
        //                    oCommand.Parameters.Add(new SqlParameter("@ERROR_CODE", "" + this.ErrorCode + ""));
        //                    oCommand.Parameters.Add(new SqlParameter("@ERROR_DESCR", "" + this.ErrorDescr + ""));
        //                    oCommand.CommandType = CommandType.StoredProcedure;

        //                    oCommand.ExecuteScalar();
        //                }
        //                oConnection.Close();
        //            }
        //        }
        //        iRetVal++;
        //    }
        //    catch (Exception ex)
        //    {
        //        Logging.WriteToLog("sSQL=" + sSQL, Logging.LogStatus.RET_VAL);
        //        var a = new Logging("BoDAL.UpdateDocument4Cancel", ex);
        //    }
        //    return iRetVal;
        //}
        public int UpdateDocumentSETIgnore(SAPbobsCOM.Company CompanyConnection)
        {
            int iRetVal = 0;
            string sSQL = "";
            try
            {


                if (CompanyConnection.DbServerType == SAPbobsCOM.BoDataServerTypes.dst_HANADB)
                {
                    sSQL = "call \"DOCUMENTS_UPDATE_SET_IGNORE_IMPACT\"('" + this.ObjType+"','"+ this.DocEntry + "')";

                    SAPbobsCOM.Recordset oRS = CommonLibrary.Functions.Database.GetRecordSet(sSQL, CompanyConnection);
                    if (oRS == null)
                    {
                        Logging.WriteToLog("failed to set ignore eliv_documents", Logging.LogStatus.ERROR);
                        Logging.WriteToLog("sSQL=" + sSQL, Logging.LogStatus.ERROR);
                    }
                }
                else
                {
                    sSQL = "[dbo].DOCUMENTS_UPDATE_SET_IGNORE_IMPACT";

                    string sFileLocation = "C:\\Program Files\\sap\\ImpactElectronicInvoicingDA\\ConfParams.ini";
                    CommonLibrary.Ini.IniFile ini = new CommonLibrary.Ini.IniFile(sFileLocation);

                    string sConnectionString = ini.IniReadValue("Default", "MSSQLConnectionString");

                    using (SqlConnection oConnection = new SqlConnection(sConnectionString))
                    {
                        oConnection.Open();

                        using (SqlCommand oCommand = new SqlCommand(sSQL, oConnection))
                        {
                            oCommand.CommandTimeout = 0;
                            oCommand.Parameters.Add(new SqlParameter("@ObjType", "" + this.ObjType + ""));
                            oCommand.Parameters.Add(new SqlParameter("@DocEntry", "" + this.DocEntry + ""));

                            oCommand.CommandType = CommandType.StoredProcedure;

                            oCommand.ExecuteScalar();
                        }
                        oConnection.Close();
                    }
                }

                iRetVal++;
            }
            catch (Exception ex)
            {
                Logging.WriteToLog("sSQL=" + sSQL, Logging.LogStatus.RET_VAL);
                var a = new Logging("BoDAL.UpdateDocument", ex);
            }
            return iRetVal;
        }
    }
}
