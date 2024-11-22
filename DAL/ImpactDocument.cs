using System;
public class ImpactDocument
{
    public string currencyCode { get; set; }
    public string category { get; set; }

    public string invoiceType { get; set; }
    public string invoiceTypeCode { get; set; }
    public int specialInvoiceCategory { get; set; }
    public int variationType { get; set; }
    public string documentTypeCode { get; set; }
    public string series { get; set; }
    public string number { get; set; }
    public bool isDeliveryNote { get; set; }

    public DateTime dateIssued { get; set; }

    public string orderNumber { get; set; }
    public string remarks { get; set; }
    public string branchCode { get; set; }
    public string branchId { get; set; }
    public string[] RelativeDocuments { get; set; }
    public string[] branchPhones { get; set; }
    public string[] branchFaxes { get; set; }
    public B2GDetails B2GDetails { get; set; }
    public Issuer Issuer { get; set; }
   // public Recipient Recipient { get; set; }

    public Counterparty counterParty { get; set; }
    public Distributiondetails DistributionDetails { get; set; }
    //public Deliveryorigindetails deliveryOriginDetails { get; set; }
    //public Deliverydestinationdetails deliveryDestinationDetails { get; set; }
    public Paymentdetails paymentDetails { get; set; }
    public Additionaldetails additionalDetails { get; set; }
    public Detail[] details { get; set; }
   // public Τaxes[] Τaxes { get; set; }
    public Summaries summaries { get; set; }
    public taxes[] taxes { get; set; }
    public VatAnalysis[] vatAnalysis { get; set; }
    public Miscellaneousdata MiscellaneousData { get; set; }
    public branchAddress branchAddress { get; set; }
    public banks[] banks { get; set; }
    public int isDelayedCode { get; set; }
}

public class B2GDetails
{
    public string ContractingAuthorityCode { get; set; }
    public string ContractingAuthority { get; set; }
    public string ContractTypeCode { get; set; }
    public string ContractReferenceCode { get; set; }
}

public class Issuer
{
    public string RegisteredName { get; set; }
    public string ΒrandName { get; set; }
    public string Vat { get; set; }
    public string taxOfficeCode { get; set; }
    public string[] Activities { get; set; }
    public string GeneralCommercialRegistryNumber { get; set; }
    public string RegistrationNumber { get; set; }
    public string otherInfo { get; set; }
    public string[] Phones { get; set; }
    public string[] faxes { get; set; }
    public string[] emails { get; set; }
    public string Url { get; set; }
    public Address Address { get; set; }
}

public class Address
{
    public string Country { get; set; }
    public string CountryCode { get; set; }
    public string City { get; set; }
    public string Street { get; set; }
    public string Postal { get; set; }
    public string Number { get; set; }

}

public class Counterparty
{
    public string Code { get; set; }
    public string registeredName { get; set; }
    public string Vat { get; set; }
    public string TaxOffice { get; set; }
    public string[] Activities { get; set; }
    public string[] Phones { get; set; }
    public string[] faxes { get; set; }
    public Address1 address { get; set; }
}

public class Address1
{
    public string countryCode { get; set; }
    public string city { get; set; }
    public string Street { get; set; }
    public string postal { get; set; }
    public string number { get; set; }

}

public class Distributiondetails
{
    public string InternalDocumentId { get; set; }
    public DateTime deliveryDate { get; set; }
    public string movePurpose { get; set; }
    public int movePurposeCode { get; set; }

    public string shippingMethod { get; set; }
    public string vehileNumber { get; set; }
    public string billOfLading { get; set; }
    public double totalQuantity { get; set; }
    public deliveryDestinationDetails deliveryDestinationDetails { get; set; }
    public deliveryOriginDetails deliveryOriginDetails { get; set; }

}

public class deliveryDestinationDetails
{
    public deliveryDestinationAddress address { get; set; }
}

public class deliveryOriginDetails
{
    public deliveryDestinationAddress address { get; set; }
}
public class deliveryDestinationAddress
{
    public string countryCode { get; set; }
    public string city { get; set; }
    public string Street { get; set; }
    public string postal { get; set; }
    public string number { get; set; }

}


public class Paymentdetails
{
    public decimal previousBalance { get; set; }
    public decimal newBalance { get; set; }
    public string electronicPaymentCode { get; set; }
    public string otherPaymentDetails { get; set; }
    public string paymentReferenceID { get; set; }

    public Paymentmethod[] paymentMethods { get; set; }
}

public class Paymentmethod
{
    public int paymentMethodTypeCode { get; set; }
    public string paymentMethodType { get; set; }

    public decimal amount { get; set; }
    public string paymentDate { get; set; }
    public string remarks { get; set; }
}

public class Additionaldetails
{
    public string[] accountingDepartmentEmails { get; set; }
    public string transmissionMethod { get; set; }
    public bool avoidEmailGrouping { get; set; }
}

public class Summaries
{
    public decimal totalNetAmount { get; set; }
    public decimal totalVATAmount { get; set; }
    public decimal totalAllowances { get; set; }
    public decimal totalGrossValue { get; set; }
    public decimal totalCatalogNetAmount { get; set; }
    //public decimal totalNetAmountAC { get; set; }
    //public decimal totalVATAmountAC { get; set; }
    public decimal totalCharges { get; set; }
    public decimal totalSpecialCharges { get; set; }
    //public decimal totalSpecialChargesAC { get; set; }
    public decimal totalStampDutyAmount { get; set; }
    public decimal totalPayableAmount { get; set; }

}

public class taxes
{
    public string taxType { get; set; }
    public int TaxTypeCode { get; set; }
    public string taxCategory { get; set; }
    public int taxCategoryCode { get; set; }
    public decimal TaxAmount { get; set; }
}
public class Miscellaneousdata
{
    public string MoreInformation1 { get; set; }
    public string MoreInformation2 { get; set; }
    public string MoreInformation3 { get; set; }
}
public class branchAddress
{
    public string countryCode { get; set; }
    public string city { get; set; }
    public string street { get; set; }
    public string postal { get; set; }

}
public class Detail
{
    public int lineNo { get; set; }
    public int classificationLineNo { get; set; }
    public string code { get; set; }
    public string[] descriptions { get; set; }
    public decimal quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal totalNetValueBeforeTotalDiscount { get; set; }
    public decimal netTotal { get; set; }
    public decimal total { get; set; }
    public decimal allowancesTotal { get; set; }
    public Allowancescharge[] allowancesCharges { get; set; }
    public decimal vatTotal { get; set; }
    public string vatCategory { get; set; }
    public int vatCategoryCode { get; set; }
    public int recordTypeCode { get; set; }
    public int vatExemptionCategoryCode { get; set; }
    public bool isInformative { get; set; }
    public bool isHidden { get; set; }
    public string CpvCode { get; set; }
    public string measurementUnitCodeEN { get; set; }
    public string measurementUnit { get; set; }
    public int measurementUnitCode { get; set; }
    public string otherMeasurementUnitTitle { get; set; }
    public decimal otherMeasurementUnitQuantity { get; set; }

    //public decimal unitPriceAC { get; set; }
    //public decimal totalAC { get; set; }
    //public decimal netTotalAC { get; set; }
    public decimal chargesTotal { get; set; }
    public decimal Total { get; set; }
    public string otherTaxesPercentCategory { get; set; }
    public int otherTaxesPercentCategoryCode { get; set; }

    public Incomeclassification incomeClassification { get; set; }
}

public class Incomeclassification
{
    public string classificationTypeCode { get; set; }
    public string classificationCategoryCode { get; set; }
}

public class Allowancescharge
{
    public int Type { get; set; }
    public string descriptions { get; set; }
    public string code { get; set; }
    public decimal underlyingValue { get; set; }
    public string percentage { get; set; }
    public decimal amount { get; set; }
}

//public class Τaxes
//{
//    public string TaxType { get; set; }
//    public int ΤaxTypeCode { get; set; }
//    public string ΤaxCategory { get; set; }
//    public int ΤaxCategoryCode { get; set; }
//    public decimal ΤaxAmount { get; set; }
//}

public class VatAnalysis
{
    public string Name { get; set; }
    public decimal Percentage { get; set; }
    public decimal VatAmount { get; set; }
    public decimal UnderlyingValue { get; set; }
}


//public class Recipient
//{
//    public string RegisteredName { get; set; }
//    public string Vat { get; set; }

//    public RecipientAddress Address { get; set; }
//}

public class RecipientAddress
{
    public string CountryCode { get; set; }
    public string City { get; set; }
    public string Street { get; set; }
    public string Postal { get; set; }
}

public class banks
{
    public string name { get; set; }
    public string iban { get; set; }
}