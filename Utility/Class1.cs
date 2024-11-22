using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Data;
using CommonLibrary.ExceptionHandling;
using System.Drawing;
using SAPbobsCOM;
using System.Collections;


namespace CommonLibrary.Functions
{
    public static class Database
    {
        public static string GetTableNameByTransType(string transType, int level)
        {
            //SALES: Offer(149), Order(139), Delivery note(140), Returns(180), Invoice(133), Return Invoice (179)
            //PURCHASES: Order(142), Delivery note(143), Returns(182), Invoice(141), Return Invoice(181)
            //STOCK: Goods Receipt(721), Goods Issue(720), Stock Transfer(940)

            string ret = string.Empty;

            if (level == 0)
            {

                switch (transType)
                {
                    case "23": ret = "OQUT"; break;
                    case "17": ret = "ORDR"; break;
                    case "15": ret = "ODLN"; break;
                    case "16": ret = "ORDN"; break;
                    case "13": ret = "OINV"; break;
                    case "14": ret = "ORIN"; break;

                    case "22": ret = "OPOR"; break;
                    case "20": ret = "OPDN"; break;
                    case "21": ret = "ORPD"; break;
                    case "18": ret = "OPCH"; break;
                    case "19": ret = "ORPC"; break;

                    case "59": ret = "OIGN"; break;
                    case "60": ret = "OIGE"; break;
                    case "67": ret = "OWTR"; break;
                }
            }
            else if (level == 1)
            {
                switch (transType)
                {

                    case "23": ret = "QUT1"; break;
                    case "17": ret = "RDR1"; break;
                    case "15": ret = "DLN1"; break;
                    case "16": ret = "RDN1"; break;
                    case "13": ret = "INV1"; break;
                    case "14": ret = "RIN1"; break;

                    case "22": ret = "POR1"; break;
                    case "20": ret = "PDN1"; break;
                    case "21": ret = "RPD1"; break;
                    case "18": ret = "PCH1"; break;
                    case "19": ret = "RPC1"; break;

                    case "59": ret = "IGN1"; break;
                    case "60": ret = "IGE1"; break;
                    case "67": ret = "WTR1"; break;
                }
            }

            return ret;
        }

        /// <summary>
        ///Έλεγχος για Διπλοεγγραφές
        /// </summary>
        /// <param name="_sTable">Ο Πίνακας που θα γίνει ο έγελχος</param>
        /// <param name="_sField">Το πεδίο που θα εφαρμοστεί το κρίτήριο</param>
        /// <param name="_sValue">Η Τιμή που θα γίνει έλεγχος για Διπλοεγγραφή</param>
        public static int ExistDublicates(string _sTable, string _sField, string _sValue, bool _bUpdate)
        {
            int iRetVal = 0;
            string sSQL = "";
            try
            {
                if (_bUpdate == false)
                {
                    sSQL = "SELECT  COUNT(V.Resulte) AS Resulte" + Environment.NewLine +
                        " FROM (SELECT COUNT(" + _sField + ") AS Resulte ," + Environment.NewLine +
                        " " + _sField + "" + Environment.NewLine +
                        " FROM " + _sTable + "" + Environment.NewLine +
                        " WHERE " + _sField + " = N'" + _sValue + "'" + Environment.NewLine +
                        " GROUP BY  " + _sField + "" + Environment.NewLine +
                        " HAVING COUNT(*) >= 1" + Environment.NewLine +
                        " ) V";
                }
                else
                {
                    sSQL = "SELECT  COUNT(V.Resulte) AS Resulte" + Environment.NewLine +
                        " FROM (SELECT COUNT(" + _sField + ") AS Resulte ," + Environment.NewLine +
                        " " + _sField + "" + Environment.NewLine +
                        " FROM " + _sTable + "" + Environment.NewLine +
                        " WHERE " + _sField + " = N'" + _sValue + "'" + Environment.NewLine +
                        " GROUP BY  " + _sField + "" + Environment.NewLine +
                        " HAVING COUNT(*) > 1" + Environment.NewLine +
                        " ) V";

                }
                //iRetVal = int.Parse(ReturnValues(sSQL, "Resulte").ToString());
            }

            catch (Exception ex)
            {
                throw new Logging("Functions.Database.ExistDublicates", ex);
            }
            return iRetVal;
        }

        /// <summary>
        ///Επιστροφή Τιμών σε Object. 
        /// </summary>
        /// <param name="_sTableName">Ο Πίνακας που θα εκτελεστεί η SQL εντολη</param>
        /// <param name="_sFieldName">Το Πεδίο που Θα Γίνει το Select</param>
        /// <param name="_oCompany">SAPB1 Company</param>
        public static object ReturnDBValues(string _sSQL, string _sFieldName, SAPbobsCOM.Company _oCompany)
        {
            try
            {
                object oRetVal = null;

                //SAPbobsCOM.Recordset rsTmp = SAPAddOnFramework.Utility.DI.Recordset.GetRecordSet(_sSQL);
                SAPbobsCOM.Recordset rsTmp = GetRecordSet(_sSQL, _oCompany);

                oRetVal = rsTmp.Fields.Item(_sFieldName).Value;

                return oRetVal;
            }

            catch (Exception ex)
            {
                return null;
                throw new Logging("Database.ReturnDBValues", ex);
            }
        }

        /// <summary>
        ///Επιστροφή Τιμών σε Object. 
        /// </summary>
        /// <param name="_sTableName">Ο Πίνακας που θα εκτελεστεί η SQL εντολη</param>
        /// <param name="_sColName">Η στήλη της οποίας η τιμή θα επιστραφεί</param>
        /// <param name="_sWhereClause">Τα κριτήρια με -AND- eg. "AND ID=1"</param>
        /// <param name="_oCompany">SAPB1 Company</param>
        public static object ReturnDBValues(string _sTableName, string _sColName, string _sWhereClause, SAPbobsCOM.Company _oCompany)
        {
            try
            {
                object oRetVal = null;

                string sSQL = "SELECT " + _sColName + " FROM " + _sTableName + " WHERE 1=1 " + _sWhereClause + "";

                //SAPbobsCOM.Recordset rsTmp = SAPAddOnFramework.Utility.DI.Recordset.GetRecordSet(sSQL);
                SAPbobsCOM.Recordset rsTmp = GetRecordSet(sSQL, _oCompany);

                oRetVal = rsTmp.Fields.Item(_sColName).Value;

                return oRetVal;



            }
            catch (Exception ex)
            {
                return null;
                throw new Logging("Database.ReturnDBValues", ex);
            }
        }

        /// <summary>
        /// Επαναφορά Αρίθμισης
        /// </summary>
        /// <param name="_sTableName">Το Όνομα Του Πίνακα Που Θα γίνει Αποκατάσταση</param>
        /// <param name="_oCompany">SAPB1 Company</param>
        /// <returns>1 For Success 0 For Failure</returns>
        public static int RestoreNumbering(string _sTableName, SAPbobsCOM.Company _oCompany)
        {
            int iRetVal = 0;
            string sSQL = "";
            try
            {
                sSQL = "UPDATE  ONNM" + Environment.NewLine +
                    " SET     AutoKey = ( SELECT  ISNULL(MAX(DocEntry),0) + 1" + Environment.NewLine +
                    " FROM    [@" + _sTableName + "])" + Environment.NewLine +
                    " WHERE   ObjectCode =N'" + _sTableName + "'";

                //SAPAddOnFramework.Utility.DI.Recordset.GetRecordSet(sSQL);
                GetRecordSet(sSQL, _oCompany);

                iRetVal++;
            }
            catch (Exception ex)
            {
                Logging.WriteToLog("sSQL=" + sSQL, Logging.LogStatus.RET_VAL);
                var a = new Logging("Database.DeleteTemplateHeader", ex);
                iRetVal = 0;
            }
            return iRetVal;
        }

        public static SAPbobsCOM.Recordset GetRecordSet(string _sSQL, SAPbobsCOM.Company _oCompany)
        {
            SAPbobsCOM.Recordset oRS = null;
            try
            {
                //oRS = (SAPbobsCOM.Recordset)SAPAddOnFramework.Globals.Company.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
                oRS = (SAPbobsCOM.Recordset)_oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
                oRS.DoQuery(_sSQL);
            }
            catch (Exception ex)
            {
                oRS = null;
                Logging.WriteToLog("_sSQL=" + _sSQL, Logging.LogStatus.RET_VAL);
                var a = new Logging("Database.GetRecordSet", ex);
            }
            return oRS;
        }

        public static SAPbobsCOM.Recordset GetRecordSet(string _sSQLProcedureName, List<BusinessLayer.RecordSetParams> _ParamList, SAPbobsCOM.Company _oCompany)
        {
            SAPbobsCOM.Recordset oRS = null;
            try
            {
                oRS = (SAPbobsCOM.Recordset)_oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
                oRS.Command.Name = _sSQLProcedureName;

                if (_ParamList != null)
                {
                    for (int i = 0; i < _ParamList.Count; i++)
                    {
                        switch (_ParamList[i].DataType)
                        {
                            case BusinessLayer.RecordSetParams.RecordSetParamDataType.dt_DateTime:
                                oRS.Command.Parameters.Item(_ParamList[i].Name).Value = _ParamList[i].Value.ToString();
                                break;
                            case BusinessLayer.RecordSetParams.RecordSetParamDataType.dt_decimal:
                                oRS.Command.Parameters.Item(_ParamList[i].Name).Value = _ParamList[i].Value.ToString().Replace(",", ".");
                                break;
                            case BusinessLayer.RecordSetParams.RecordSetParamDataType.dt_Int:
                                oRS.Command.Parameters.Item(_ParamList[i].Name).Value = int.Parse(_ParamList[i].Value.ToString());
                                break;
                            case BusinessLayer.RecordSetParams.RecordSetParamDataType.dt_String:
                                oRS.Command.Parameters.Item(_ParamList[i].Name).Value = _ParamList[i].Value.ToString();
                                break;
                        }
                    }
                }
                oRS.Command.Execute();
            }
            catch (Exception ex)
            {
                var a = new Logging("Database.GetRecordSet", ex);
            }
            return oRS;
        }
    }

    public class SequentialNumbering
    {
        private static string sBaseCharacters = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private static int sBaseLength = sBaseCharacters.Length;

        /// <summary>
        /// Gets the next sequential integer code for a specific table and field
        /// </summary>
        /// <param name="tableName">database table name</param>
        /// <param name="fieldName">database field name</param>
        /// <returns></returns>
        public static int GetNextSequentialCode(string tableName, string fieldName, SAPbobsCOM.Company _oCompany)
        {
            try
            {
                int iRet = 0;


                string sql = String.Format("SELECT ISNULL(MAX({0}),0)+1 FROM [{1}]", fieldName, tableName);

                SAPbobsCOM.Recordset rs = CommonLibrary.Functions.Database.GetRecordSet(sql, _oCompany);
                if (rs.RecordCount > 0)
                {
                    iRet = int.Parse(rs.Fields.Item(0).Value.ToString());
                }

                return iRet;
            }
            catch (Exception ex)
            {
                throw new Logging("Error in Utility.SequentialNumbering.GetNextSequentialCode", ex);
            }
        }

        /// <summary>
        /// Gets the next sequential code for a specific table
        /// </summary>
        /// <param name="tableName">database table name</param>
        /// <returns></returns>
        public static string GetNextSequentialCode(string tableName, SAPbobsCOM.Company _oCompany)
        {
            try
            {
                // 1. Read last sequential code from table
                string sLast = ReadLastSequentialCode(tableName, _oCompany);

                // 2. Decode
                long iDecoded = BaseDecode(sLast);

                // 3. Add
                long iNew = iDecoded + 1;

                // 4. Encode & Return
                string sNew = BaseEncode(iNew);

                // 5. Fill characters & return
                string s = FillCode(sNew);
                return s;
            }
            catch (Exception ex)
            {
                throw new Logging("Error in Utility.SequentialNumbering.GetNextSequentialCode", ex);
            }
        }

        private static string ReadLastSequentialCode(string tableName, SAPbobsCOM.Company _oCompany)
        {
            try
            {
                string sRet = string.Empty;

                string sql = String.Format("SELECT TOP 1 CODE FROM [{0}] ORDER BY CODE DESC", tableName);

                SAPbobsCOM.Recordset rs = CommonLibrary.Functions.Database.GetRecordSet(sql, _oCompany);
                if (rs.RecordCount > 0)
                {
                    sRet = rs.Fields.Item(0).Value.ToString();
                }
                else
                {
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    sb.Append(sBaseCharacters[0], 8);
                    sRet = sb.ToString();
                }

                return sRet;
            }
            catch (Exception ex)
            {
                throw new Logging("Error in Utility.SequentialNumbering.ReadLastSequentialCode", ex);
            }
        }

        private static string FillCode(string codeNumber)
        {
            try
            {
                string sRet = string.Empty;

                if (codeNumber.Length < 8)
                {
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    sb.Append(sBaseCharacters[0], 8);
                    string sConcat = sb.Append(codeNumber).ToString();
                    sRet = sConcat.Substring(sConcat.Length - 8);
                }
                else
                {
                    sRet = codeNumber;
                }

                return sRet;
            }
            catch (Exception ex)
            {
                throw new Logging("Error in Utility.SequentialNumbering.FillCode", ex);
            }
        }

        private static string BaseEncode(long value)
        {
            try
            {
                char[] baseChars = sBaseCharacters.ToCharArray();
                string returnValue = "";

                if (value < 0)
                {
                    value *= -1;
                }

                do
                {
                    returnValue = baseChars[value % baseChars.Length] + returnValue;
                    value /= sBaseLength;
                } while (value != 0);

                return returnValue;
            }
            catch (Exception ex)
            {
                throw new Logging("Error in Utility.SequentialNumbering.BaseEncode", ex);
            }
        }

        private static long BaseDecode(string input)
        {
            try
            {
                char[] arrInput = input.ToCharArray();
                Array.Reverse(arrInput);
                long returnValue = 0;

                for (int i = 0; i < arrInput.Length; i++)
                {
                    int valueindex = sBaseCharacters.IndexOf(arrInput[i]);
                    returnValue += Convert.ToInt64(valueindex * Math.Pow(sBaseLength, i));
                }

                return returnValue;
            }
            catch (Exception ex)
            {
                throw new Logging("Error in Utility.SequentialNumbering.BaseDecode", ex);
            }
        }
    }
}
