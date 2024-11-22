using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Specialized;
using System.Collections;
using CommonLibrary.ExceptionHandling;
using ImpactElectronicInvoicing.Modules;

namespace ImpactElectronicInvoicing
{
    class AddOnGlobals
    {
        /// <summary>
        /// Μαρκάρισμα της Εγγραφής σας Προβληματική για να μην Ξανα-Απασχολίσει το Service
        /// </summary>
        /// <param name="_sObjectCode">Αντικείμενο</param>
        /// <param name="_sPK">Κλειδί (Primary Key)</param>
        /// <param name="_oCompany">SAP Company</param>
        public static void SetErrorRecord(string _sObjectCode, string _sPK, SAPbobsCOM.Company _oCompany)
        {
            string sSQL = "";
            try
            {
                sSQL = "EXEC BYT_SP_ABERON_SET_ERROR '" + _sObjectCode + "', " + _sPK + "";
                CommonLibrary.Functions.Database.GetRecordSet(sSQL, _oCompany);
            }
            catch (Exception ex)
            {
                Logging.WriteToLog("sSQL=" + sSQL, Logging.LogStatus.RET_VAL);
                var a = new Logging("AddOnGlobals.SetErrorRecord", ex);
            }
        }
    }
}
