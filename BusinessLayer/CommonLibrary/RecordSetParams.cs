using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CommonLibrary.BusinessLayer
{
   public class RecordSetParams
    {
        public enum RecordSetParamDataType {dt_decimal, dt_Int, dt_DateTime, dt_String}
        #region Public Properties
        public string Name { get; set; }
        public object Value { get; set; }
        public RecordSetParamDataType DataType { get; set; }
        #endregion

        public RecordSetParams()
        { }
    }
}
