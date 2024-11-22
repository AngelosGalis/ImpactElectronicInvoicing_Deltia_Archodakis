using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImpactElectronicInvoicing.Enumerators
{
    public enum ot_Object { otSalesDocuments, otPurchaseDocuments, otJournalEntries }
    public enum DocumentPrepared { p_Success, pFailure, p_NA }

    public enum DocumentType { p_Income, p_Matched, p_EU_TX, p_Reject, p_Deviation }

    public enum SAPResult { sr_Success, sr_Failure }

}
