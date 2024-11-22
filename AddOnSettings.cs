using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Specialized;
using System.Collections;
//using SAPAddOnFramework;
//using SAPAddOnFramework.ExceptionHandling;
using CommonLibrary.ExceptionHandling;
using SAPmyDataService.Modules;

namespace SAPmyDataService
{
    class AddOnSettings
    {
        #region Public Static Properties
        public static string AADE_USERNAME { get; set; }
        public static string AADE_SUBSCRIPTION { get; set; }
        public static string AADE_AFM { get; set; }
        #endregion

        public AddOnSettings()
        {
        }

        public static void LoadSettings(SAPbobsCOM.Company _oCompany)
        {
            string sSQL = "";
            try
            {
                CommonLibrary.Ini.IniFile ini = new CommonLibrary.Ini.IniFile("C:\\Program Files\\SAP\\SAPmyDataService\\ConfParams.ini");

                if (_oCompany.DbServerType == SAPbobsCOM.BoDataServerTypes.dst_HANADB)
                {
                    sSQL = "SELECT  \"U_Setting\" ," + Environment.NewLine +
                        " \"U_Value\"" + Environment.NewLine +
                        " FROM    \"@TKA_SETTINGS\"" + Environment.NewLine +
                        " WHERE   1 = 1" + Environment.NewLine +
                        " AND \"U_AddOn\" = N'" + ini.IniReadValue("Default", "ADDON_NAME") + "'";
                }
                else
                {
                    sSQL = "SELECT  U_Setting ," + Environment.NewLine +
                            " U_Value" + Environment.NewLine +
                            " FROM    [@TKA_SETTINGS]" + Environment.NewLine +
                            " WHERE   1 = 1" + Environment.NewLine +
                            " AND U_AddOn = N'" + ini.IniReadValue("Default", "ADDON_NAME") + "'";
                }
                
                SAPbobsCOM.Recordset oRS = CommonLibrary.Functions.Database.GetRecordSet(sSQL, _oCompany);

                while (oRS.EoF == false)
                {
                    string sProperty = oRS.Fields.Item("U_Setting").Value.ToString();
                    string sValue = oRS.Fields.Item("U_Value").Value.ToString();

                    switch (sProperty)
                    {
                        case "AADE_AFM":
                            AddOnSettings.AADE_AFM = sValue;
                            break;
                        case "AADE_SUBSCRIPTION":
                            AddOnSettings.AADE_SUBSCRIPTION = sValue;
                            break;
                        case "AADE_USERNAME":
                            AddOnSettings.AADE_USERNAME = sValue;
                            break;
                    }
                    oRS.MoveNext();
                }
            }
            catch (Exception ex)
            {
                Logging.WriteToLog("sSQL=" + sSQL, Logging.LogStatus.RET_VAL);
                var a = new Logging("AddOnSettings.LoadSettings", ex);
            }
        }
    }
}