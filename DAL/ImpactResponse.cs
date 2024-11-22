using System;


public class ImpactResponse
{
    public string integritySignature { get; set; }
    public string signature { get; set; }
    public string uid { get; set; }
    public long mark { get; set; }
    public string authenticationCode { get; set; }
    public string myDataResponse { get; set; }
    public string status { get; set; }
    public string series { get; set; }
    public string number { get; set; }
    public DateTime dateIssued { get; set; }
    public string domain { get; set; }
    public string qrCodeString { get; set; }
    public bool success { get; set; }
    public string message { get; set; }
    public Mydataerror[] myDataErrors { get; set; }
    public string[] errors { get; set; }
    public string errorMessage { get; set; }
    public string signatureData { get; set; }
    public string myDataRequest { get; set; }
}

public class Mydataerror
{
    public int key { get; set; }
    public string value { get; set; }
}

