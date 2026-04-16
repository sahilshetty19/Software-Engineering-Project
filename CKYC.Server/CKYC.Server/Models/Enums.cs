namespace CKYC.Server.Models;

public enum RiskRating : short { Low = 0, Medium = 1, High = 2 }

public enum ProfileStatus : short { Active = 0, NeedsReview = 1, Suspended = 2 }

public enum DocumentType : short
{
    Other = 0,
    Passport = 1,
    NationalId = 2,
    IRP = 3,
    DrivingLicense = 4,
    UtilityBill = 5,
    BankStatement = 6,
    PSCFront = 10,
    PSCBack = 11
}

public enum VerificationStatus : short { NeedsReview = 0, Verified = 1, Rejected = 2 }



public enum FileRole : short { Other = 0, POI = 1, POA = 2, Photo = 3 }

public enum ActorType : short { System = 0, BankUser = 1, Admin = 2 }

public enum AuditOutcome : short { Success = 0, Fail = 1, Denied = 2 }

public enum SubmissionStatus : short
{
    Received = 0,
    Processing = 1,
    Success = 2,
    Failed = 3
}