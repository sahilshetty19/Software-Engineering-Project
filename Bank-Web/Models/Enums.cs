namespace Bank.Web.Models;

public enum UploadSource : short { ManualForm = 0, ExcelUpload = 1 }

public enum KycWorkflowStatus : short
{
    Draft = 0, OcrDone = 1, Validated = 2, Zipped = 3, Sent = 4, Completed = 5, Failed = 6
}

public enum OcrStatus : short { Pending = 0, Completed = 1, Failed = 2 }
public enum ValidationStatus : short { Pass = 0, NeedsReview = 1, Fail = 2 }
public enum DedupeStatus : short { NotChecked = 0, DuplicateFound = 1, Unique = 2 }

public enum DocumentType : short { PSC = 0, IRP = 1, Passport = 2, StudentID = 3, POA = 4, Other = 99 }
public enum FileRole : short { POI = 0, POA = 1, Photo = 2, Other = 99 }

public enum UploadStatus : short { Queued = 0, Uploaded = 1, Failed = 2 }

public enum SearchStatus : short { MatchFound = 0, NotFound = 1, Multiple = 2, Failed = 3 }
public enum DownloadStatus : short { Success = 0, Failed = 1, Partial = 2 }

public enum UpdateStatus : short { Success = 0, Rejected = 1, Failed = 2 }

public enum RiskRating : short { Low = 0, Medium = 1, High = 2 }
public enum AuditOutcome : short { Success = 0, Fail = 1, Denied = 2 }