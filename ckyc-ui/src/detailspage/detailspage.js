import React from "react";
import "./detailspage.css";

export default function DetailsPage() {
  // Demo data (replace with API data)
  const details = {
    requestRef: "REQ-20260226152031-e6687dca26814aa6967f65b661e57676",
    name: "Sahil Shetty",
    dob: "26-02-2008",
    ppsn: "0372967UB",
    email: "sahil.shetty73@gmail.com",
    phone: "0899482618",
    county: "Westmeath",
    city: "Moate",
    status: "Draft",
  };

  const uploadedDocuments = [
    {
      fileName: "Screenshot 2025-09-24 200142.png",
      type: "Other",
      sizeBytes: 60302,
      hash: "65de08725f4714d35437cfe1a2841136700d8f06526676b91643b5f0ff4962cc",
    },
  ];

  const onSearchCkyc = () => {
    // Hook to your search route/modal
    alert("Search CKYC (demo)");
  };

  const formatBytes = (n) => {
    if (typeof n !== "number") return "-";
    return n.toLocaleString("en-IE");
  };

  return (
    <div className="details-page bg-light min-vh-100 d-flex align-items-center">
      <div className="container py-5">
        <div className="card details-wrap mx-auto shadow-sm" style={{ maxWidth: '900px' }}>
          <div className="card-body p-4 p-md-5">
          {/* Header */}
          <div className="d-flex align-items-start justify-content-between gap-3 flex-wrap">
            <h1 className="details-title mb-0">KYC Upload Details</h1>

            <button type="button" className="btn btn-outline-secondary btn-sm" onClick={onSearchCkyc}>
              Search CKYC
            </button>
          </div>

          <hr className="details-divider my-3" />

          {/* Key/Value block */}
          <div className="details-kv">
            <div className="kv-row row mb-2">
              <div className="kv-key col-sm-4">Request Ref</div>
              <div className="kv-value col-sm-8 text-break">{details.requestRef}</div>
            </div>

            <div className="kv-row row mb-2">
              <div className="kv-key col-sm-4">Name</div>
              <div className="kv-value col-sm-8">{details.name}</div>
            </div>

            <div className="kv-row row mb-2">
              <div className="kv-key col-sm-4">DOB</div>
              <div className="kv-value col-sm-8">{details.dob}</div>
            </div>

            <div className="kv-row row mb-2">
              <div className="kv-key col-sm-4">PPSN</div>
              <div className="kv-value col-sm-8">{details.ppsn}</div>
            </div>

            <div className="kv-row row mb-2">
              <div className="kv-key col-sm-4">Email</div>
              <div className="kv-value col-sm-8">{details.email}</div>
            </div>

            <div className="kv-row row mb-2">
              <div className="kv-key col-sm-4">Phone</div>
              <div className="kv-value col-sm-8">{details.phone}</div>
            </div>

            <div className="kv-row row mb-2">
              <div className="kv-key col-sm-4">County</div>
              <div className="kv-value col-sm-8">{details.county}</div>
            </div>

            <div className="kv-row row mb-2">
              <div className="kv-key col-sm-4">City</div>
              <div className="kv-value col-sm-8">{details.city}</div>
            </div>

            <div className="kv-row row mb-2">
              <div className="kv-key col-sm-4">Status</div>
              <div className="kv-value col-sm-8">
                <span className={`status-pill status-${details.status.toLowerCase()}`}>{details.status}</span>
              </div>
            </div>
          </div>

          <hr className="details-divider my-4" />

          {/* Documents */}
          <h2 className="docs-title">Uploaded Documents</h2>

          <div className="table-responsive mt-2">
            <table className="table table-bordered table-sm align-middle docs-table">
              <thead className="table-light">
                <tr>
                  <th style={{ width: "45%" }}>File Name</th>
                  <th style={{ width: "10%" }}>Type</th>
                  <th style={{ width: "15%" }}>Size (bytes)</th>
                  <th style={{ width: "30%" }}>Hash</th>
                </tr>
              </thead>
              <tbody>
                {uploadedDocuments.length === 0 ? (
                  <tr>
                    <td colSpan={4} className="text-muted">
                      No documents uploaded.
                    </td>
                  </tr>
                ) : (
                  uploadedDocuments.map((doc, i) => (
                    <tr key={`${doc.hash}-${i}`}>
                      <td className="text-break">{doc.fileName}</td>
                      <td>{doc.type}</td>
                      <td>{formatBytes(doc.sizeBytes)}</td>
                      <td className="hash-cell text-break">{doc.hash}</td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>

          {/* Footer */}
          <div className="details-footer small text-muted mt-4 d-flex align-items-center gap-2">
            <span>© 2026 - Bank_Web</span>
            <span className="dot" aria-hidden="true">•</span>
            <a href="#privacy" className="link-secondary">
              Privacy
            </a>
          </div>
        </div>
      </div>
    </div>
  </div>
  );
}