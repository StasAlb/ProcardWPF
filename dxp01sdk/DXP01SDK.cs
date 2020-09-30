////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Datacard Corporation.  All Rights Reserved.
//
// XPS Card Printer SDK constants
////////////////////////////////////////////////////////////////////////////////

namespace dxp01sdk {

    public struct strings {

        // IBidiSpl request strings:
        public const string STARTJOB = "\\Printer.Print:StartJob:Set";

        public const string ENDJOB = "\\Printer.Print:EndJob:Set";
        public const string MAGSTRIPE_READ = "\\Printer.MagstripeUnit:Back:Read";
        public const string MAGSTRIPE_ENCODE = "\\Printer.MagstripeUnit:Back:Encode";
        public const string MAGSTRIPE_READ_FRONT = "\\Printer.MagstripeUnit:Front:Read";
        public const string MAGSTRIPE_ENCODE_FRONT = "\\Printer.MagstripeUnit:Front:Encode";
        public const string SMARTCARD_PARK = "\\Printer.SmartCardUnit:Front:Park";
        public const string SMARTCARD_PARK_BACK = "\\Printer.SmartCardUnit:Back:Park";
        public const string BARCODE_PARK = "\\Printer.BarcodeUnit:Front:Park";
        public const string BARCODE_PARK_BACK = "\\Printer.BarcodeUnit:Back:Park";
        public const string PRINTER_OPTIONS2 = "\\Printer.PrinterOptions2:Read";
        public const string COUNTER_STATUS2 = "\\Printer.CounterStatus2:Read";
        public const string SUPPLIES_STATUS = "\\Printer.SuppliesStatus:Read";
        public const string SUPPLIES_STATUS2 = "\\Printer.SuppliesStatus2:Read";
        public const string SUPPLIES_STATUS3 = "\\Printer.SuppliesStatus3:Read";
        public const string PRINTER_MESSAGES = "\\Printer.PrintMessages:Read";
        public const string JOB_STATUS = "\\Printer.JobStatus:Read";
        public const string PRINTER_ACTION = "\\Printer.Action:Set";
        public const string SDK_VERSION = "\\Printer.SDK:Version";
        public const string SMARTCARD_CONNECT = "\\Printer.SmartCardUnit:SingleWire:Connect";
        public const string SMARTCARD_RECONNECT = "\\Printer.SmartCardUnit:SingleWire:Reconnect";
        public const string SMARTCARD_DISCONNECT = "\\Printer.SmartCardUnit:SingleWire:Disconnect";
        public const string SMARTCARD_TRANSMIT = "\\Printer.SmartCardUnit:SingleWire:Transmit";
        public const string SMARTCARD_STATUS = "\\Printer.SmartCardUnit:SingleWire:Status";
        public const string SMARTCARD_CONTROL = "\\Printer.SmartCardUnit:SingleWire:Control";
        public const string SMARTCARD_GETATTRIB = "\\Printer.SmartCardUnit:SingleWire:GetAttrib";
        public const string CHANGE_LOCK_PASSWORD = "\\Printer.Locks:ChangePassword:Set";
        public const string LOCK_PRINTER = "\\Printer.Locks:ChangeLockState:Set";
        public const string RESTART_PRINTER = "\\Printer.Restart:Set";
        public const string RESET_CARD_COUNTS = "\\Printer.ResetCardCount:Set";

        public const string STATUS_ELEMENT = "PrinterStatus";
        public const string CLIENT_ID_ELEMENT = "ClientID";
        public const string PRINTER_JOB_ID_ELEMENT = "PrinterJobID";
        public const string WINDOWS_JOB_ID_ELEMENT = "WindowsJobID";
        public const string ERROR_CODE_ELEMENT = "ErrorCode";
        public const string ERROR_SEVERITY_ELEMENT = "ErrorSeverity";
        public const string ERROR_STRING_ELEMENT = "ErrorString";
        public const string DATAFROMPRINTER_ELEMENT = "DataFromPrinter";
        public const string PRINTER_ACTION_ELEMENT = "PrinterAction";
        public const string ACTION_ELEMENT = "Action";
        public const string JOB_STATUS_ELEMENT = "JobStatus";
        public const string JOB_STATE_ELEMENT = "JobState";
        public const string JOB_RESTART_COUNT_ELEMENT = "JobRestartCount";

        public const string JOB_NOT_AVAILABLE = "NotAvailable";
        public const string JOB_ACTIVE = "JobActive";
        public const string JOB_SUCCEEDED = "JobSucceeded";
        public const string JOB_FAILED = "JobFailed";
        public const string JOB_CANCELLED = "JobCancelled";

        public const string LAMINATOR_BARCODE_READ = "\\Printer.Laminator:BarcodeRead:Set";
        public const string LAMINATOR_BARCODE_READ_AND_VERIFY = "\\Printer.Laminator:BarcodeReadAndVerify:Set";
        public const string LAMINATOR_BARCODE_READ_DATA = "\\Printer.Laminator:BarcodeRead:Get";

        public const string LAMINATOR_BARCODE_READ_XML = "<?xml version=\"1.0\"?>"
            + "<Barcode>"
            + "<PrinterJobID>{0}</PrinterJobID>"
            + "<TimeoutMilliseconds>{1}</TimeoutMilliseconds>"
            + "</Barcode>";

        public const string PRINTER_ACTION_XML = "<?xml version=\"1.0\"?>"
            + "<PrinterAction>"
            + "<Action>{0}</Action>"
            + "<PrinterJobID>{1}</PrinterJobID>"
            + "<ErrorCode>{2}</ErrorCode>"
            + "</PrinterAction>";

        public const string JOB_STATUS_XML = "<?xml version=\"1.0\"?>"
            + "<JobStatus>"
            + "<PrinterJobID>{0}</PrinterJobID>"
            + "</JobStatus>";

        public const string STARTJOB_XML = "<?xml version=\"1.0\"?>"
           + "<StartJob>"
           + "<InputHopperSelection>{0}</InputHopperSelection>"
           + "<CheckPrintRibbonSupplies>{1}</CheckPrintRibbonSupplies>"
           + "<CheckEmbossSupplies>{2}</CheckEmbossSupplies>"
           + "</StartJob>";

        public const string SMARTCARD_CONNECT_XML = "<?xml version=\"1.0\"?>"
          + "<SmartcardConnect>"
          + "<PreferredProtocol>{0}</PreferredProtocol>"
          + "</SmartcardConnect>";

        public const string SMARTCARD_RECONNECT_XML = "<?xml version=\"1.0\"?>"
           + "<SmartcardReconnect>"
           + "<PreferredProtocol>{0}</PreferredProtocol>"
           + "<Initialization>{1}</Initialization>"
           + "</SmartcardReconnect>";

        public const string SMARTCARD_DISCONNECT_XML = "<?xml version=\"1.0\"?>"
           + "<SmartcardDisconnect>"
           + "<Disposition>{0}</Disposition>"
           + "</SmartcardDisconnect>";

        public const string SMARTCARD_TRANSMIT_XML = "<?xml version=\"1.0\"?>"
           + "<SmartcardTransmit>"
           + "<SendBuffer>{0}</SendBuffer>"
           + "</SmartcardTransmit>";

        public const string SMARTCARD_GETATTRIB_XML = "<?xml version=\"1.0\"?>"
            + "<SmartcardGetAttrib>"
            + "<Attr>{0}</Attr>"
            + "</SmartcardGetAttrib>";

        public const string SMARTCARD_CONTROL_XML = "<?xml version=\"1.0\"?>"
            + "<SmartcardControl>"
            + "<InBuffer>{0}</InBuffer>"
            + "</SmartcardControl>";

        public const string LOCK_PRINTER_XML = "<?xml version=\"1.0\"?>"
           + "<ChangeLocks>"
           + "<LockPrinter>{0}</LockPrinter>"
           + "<CurrentPassword>{1}</CurrentPassword>"
           + "</ChangeLocks>";

        public const string CHANGE_LOCK_PASSWORD_XML = "<?xml version=\"1.0\"?>"
           + "<ChangeLocksPassword>"
           + "<LockPrinter>{0}</LockPrinter>"
           + "<CurrentPassword>{1}</CurrentPassword>"
           + "<NextPassword>{2}</NextPassword>"
           + "</ChangeLocksPassword>";
    }

    public enum Actions {
        Cancel = 100,
        Resume = 101,
        Restart = 102,
    }

    public enum laminator_barcode_timout {
        INFINITE_WAIT = 0x7FFFFFFF,
    }
}