using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImpactElectronicInvoicing.BusinessLayer
{
    public class BoAuth
    {
        #region Public Properties
        public string SAP_SERVER { get; set; }
        public string B1_USERNAME { get; set; }
        public string B1_PASSWORD { get; set; }
        public string DB_VERSION { get; set; }
        public string DB_USERNAME { get; set; }
        public string DB_PASSWORD { get; set; }
        public string COMPANY_NAME { get; set; }
        public string LICENSE_SERVER { get; set; }
        public string LICENSE_SERVER_PORT { get; set; }
        public string AADE_USER_ID { get; set; }
        public string AADE_SUBSCRIPTION_KEY { get; set; }
        public string ENDPOINT_SEND_INVOICES { get; set; }
        public string ENDPOINT_CANCEL_INVOICE { get; set; }
        public string ISSUER_VAT_ID { get; set; }
        #endregion

        public BoAuth()
        { }
    }
}
