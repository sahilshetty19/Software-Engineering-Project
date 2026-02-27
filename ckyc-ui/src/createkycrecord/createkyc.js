// src/pages/createkyc.js
import React, { useEffect, useMemo, useState } from "react";
import "./createkyc.css";
import { getCounties, getCities, uploadKyc } from "../services/kycService";

const MAX_FILE_MB = 10;
const MAX_FILE_BYTES = MAX_FILE_MB * 1024 * 1024;

function isValidPps(value) {
  const v = (value || "").trim().toUpperCase();
  return /^(\d{7})([A-Z]{1,2})$/.test(v);
}

function isValidEircode(value) {
  const v = (value || "").trim().toUpperCase();
  return /^[A-Z0-9]{3}\s?[A-Z0-9]{4}$/.test(v);
}

export default function NewKycUpload() {
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isLoadingLookups, setIsLoadingLookups] = useState(true);

  const [counties, setCounties] = useState([]); // [{id, name}]
  const [cities, setCitiesState] = useState([]); // [{id, name}]

  const [form, setForm] = useState({
    firstName: "",
    lastName: "",
    dob: "",
    pps: "",
    email: "",
    phone: "",
    address1: "",
    countyId: "", // GUID
    cityId: "",   // GUID
    eircode: "",
    riskRating: "Low",
    pep: false,
    files: [],
  });

  const [errors, setErrors] = useState({});
  const [pageError, setPageError] = useState("");

  const setField = (name, value) => {
    setForm((prev) => {
      const next = { ...prev, [name]: value };
      // reset city when county changes
      if (name === "countyId") next.cityId = "";
      return next;
    });
  };

  const validateFiles = (files) => {
    const problems = [];
    for (const f of files) {
      if (f.size > MAX_FILE_BYTES) problems.push(`${f.name} exceeds ${MAX_FILE_MB}MB`);
    }
    return problems;
  };

  const validate = () => {
    const nextErrors = {};

    if (!form.firstName.trim()) nextErrors.firstName = "First name is required.";
    if (!form.lastName.trim()) nextErrors.lastName = "Last name is required.";
    if (!form.dob) nextErrors.dob = "Date of birth is required.";

    if (!form.pps.trim()) nextErrors.pps = "PPS number is required.";
    else if (!isValidPps(form.pps)) nextErrors.pps = "Enter a valid PPS (e.g., 1234567AB).";

    if (!form.email.trim()) nextErrors.email = "Email is required.";
    else if (!/^\S+@\S+\.\S+$/.test(form.email)) nextErrors.email = "Enter a valid email.";

    if (!form.phone.trim()) nextErrors.phone = "Phone number is required.";
    else if (!/^[0-9+\s()-]{7,}$/.test(form.phone)) nextErrors.phone = "Enter a valid phone number.";

    if (!form.address1.trim()) nextErrors.address1 = "Address line 1 is required.";

    // GUID required
    if (!form.countyId) nextErrors.countyId = "County is required.";
    if (!form.cityId) nextErrors.cityId = "City is required.";

    if (!form.eircode.trim()) nextErrors.eircode = "Eircode is required.";
    else if (!isValidEircode(form.eircode)) nextErrors.eircode = "Enter a valid Eircode (e.g., N37F4E2).";

    if (!form.files || form.files.length === 0) nextErrors.files = "Please upload at least one document.";
    else {
      const fileProblems = validateFiles(form.files);
      if (fileProblems.length) nextErrors.files = fileProblems.join(", ");
    }

    setErrors(nextErrors);
    return Object.keys(nextErrors).length === 0;
  };

  const onChooseFiles = (e) => {
    const chosen = Array.from(e.target.files || []);
    const fileProblems = validateFiles(chosen);

    setForm((prev) => ({ ...prev, files: chosen }));

    setErrors((prev) => ({
      ...prev,
      files:
        chosen.length === 0
          ? "Please upload at least one document."
          : fileProblems.length
          ? fileProblems.join(", ")
          : undefined,
    }));
  };

  const removeFile = (index) => {
    setForm((prev) => ({ ...prev, files: prev.files.filter((_, i) => i !== index) }));
  };

  // Load counties on mount
  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        setIsLoadingLookups(true);
        const data = await getCounties();
        if (!cancelled) setCounties(data);
      } catch (e) {
        if (!cancelled) setPageError(String(e.message || e));
      } finally {
        if (!cancelled) setIsLoadingLookups(false);
      }
    })();
    return () => { cancelled = true; };
  }, []);

  // Load cities when countyId changes
  useEffect(() => {
    let cancelled = false;
    (async () => {
      if (!form.countyId) {
        setCitiesState([]);
        return;
      }
      try {
        const data = await getCities(form.countyId);
        if (!cancelled) setCitiesState(data);
      } catch (e) {
        if (!cancelled) setPageError(String(e.message || e));
      }
    })();
    return () => { cancelled = true; };
  }, [form.countyId]);

  const onSubmit = async (e) => {
    e.preventDefault();
    setPageError("");
    if (!validate()) return;

    setIsSubmitting(true);
    try {
      const result = await uploadKyc(form);
      console.log("upload result:", result);
      alert("KYC successfully uploaded");
      onCancel();
    } catch (err) {
      console.error("upload failed", err);
      setPageError(err?.message || "Upload failed");
      alert("Error uploading KYC. Please try again.");
    } finally {
      setIsSubmitting(false);
    }
  };

  const onCancel = () => {
    setForm({
      firstName: "",
      lastName: "",
      dob: "",
      pps: "",
      email: "",
      phone: "",
      address1: "",
      countyId: "",
      cityId: "",
      eircode: "",
      riskRating: "Low",
      pep: false,
      files: [],
    });
    setCitiesState([]);
    setErrors({});
    setPageError("");
  };

  return (
    <div className="kyc-page bg-light min-vh-100 d-flex flex-column">
      <nav className="navbar navbar-expand-lg navbar-light bg-white shadow-sm">
        <div className="container">
          <span className="navbar-brand fw-bold">CKYC Portal</span>
        </div>
      </nav>

      <div className="container py-4">
        <div className="d-flex justify-content-center">
          <div className="card kyc-card shadow-sm rounded-4 w-100" style={{ maxWidth: 980 }}>
            <div className="card-header bg-primary text-white border-0 rounded-top-4">
              <div className="d-flex align-items-center justify-content-between">
                <h2 className="h5 mb-0">New KYC Upload</h2>
                <span className="badge bg-light text-primary kyc-badge">Bank App</span>
              </div>
            </div>

            <div className="card-body p-4 p-md-5">
              <p className="text-muted mb-4">
                Create a new KYC request. The final internal customer record is created only after CKYC verification.
              </p>

              {pageError && (
                <div className="alert alert-danger" role="alert">
                  {pageError}
                </div>
              )}

              <form onSubmit={onSubmit} noValidate>
                <div className="section-head">
                  <div className="section-title">CUSTOMER DETAILS</div>
                  <div className="section-line" />
                </div>

                <div className="row g-3 mt-1">
                  <div className="col-12 col-md-6">
                    <label className="form-label">First Name</label>
                    <input
                      className={`form-control ${errors.firstName ? "is-invalid" : ""}`}
                      value={form.firstName}
                      onChange={(e) => setField("firstName", e.target.value)}
                    />
                    {errors.firstName && <div className="invalid-feedback">{errors.firstName}</div>}
                  </div>

                  <div className="col-12 col-md-6">
                    <label className="form-label">Last Name</label>
                    <input
                      className={`form-control ${errors.lastName ? "is-invalid" : ""}`}
                      value={form.lastName}
                      onChange={(e) => setField("lastName", e.target.value)}
                    />
                    {errors.lastName && <div className="invalid-feedback">{errors.lastName}</div>}
                  </div>

                  <div className="col-12 col-md-6">
                    <label className="form-label">Date of Birth</label>
                    <input
                      type="date"
                      className={`form-control ${errors.dob ? "is-invalid" : ""}`}
                      value={form.dob}
                      onChange={(e) => setField("dob", e.target.value)}
                    />
                    {errors.dob && <div className="invalid-feedback d-block">{errors.dob}</div>}
                  </div>

                  <div className="col-12 col-md-6">
                    <label className="form-label">PPS Number</label>
                    <input
                      className={`form-control ${errors.pps ? "is-invalid" : ""}`}
                      value={form.pps}
                      onChange={(e) => setField("pps", e.target.value.toUpperCase())}
                    />
                    {errors.pps && <div className="invalid-feedback">{errors.pps}</div>}
                  </div>

                  <div className="col-12 col-md-6">
                    <label className="form-label">Email Address</label>
                    <input
                      type="email"
                      className={`form-control ${errors.email ? "is-invalid" : ""}`}
                      value={form.email}
                      onChange={(e) => setField("email", e.target.value)}
                    />
                    {errors.email && <div className="invalid-feedback">{errors.email}</div>}
                  </div>

                  <div className="col-12 col-md-6">
                    <label className="form-label">Phone Number</label>
                    <input
                      className={`form-control ${errors.phone ? "is-invalid" : ""}`}
                      value={form.phone}
                      onChange={(e) => setField("phone", e.target.value)}
                    />
                    {errors.phone && <div className="invalid-feedback">{errors.phone}</div>}
                  </div>

                  <div className="col-12">
                    <label className="form-label">Address Line 1</label>
                    <input
                      className={`form-control ${errors.address1 ? "is-invalid" : ""}`}
                      value={form.address1}
                      onChange={(e) => setField("address1", e.target.value)}
                    />
                    {errors.address1 && <div className="invalid-feedback">{errors.address1}</div>}
                  </div>

                  <div className="col-12 col-md-4">
                    <label className="form-label">County</label>
                    <select
                      className={`form-select ${errors.countyId ? "is-invalid" : ""}`}
                      value={form.countyId}
                      onChange={(e) => setField("countyId", e.target.value)}
                      disabled={isLoadingLookups}
                    >
                      <option value="">{isLoadingLookups ? "Loading..." : "Select County"}</option>
                      {counties.map((c) => (
                        <option key={c.id} value={c.id}>
                          {c.name}
                        </option>
                      ))}
                    </select>
                    {errors.countyId && <div className="invalid-feedback">{errors.countyId}</div>}
                  </div>

                  <div className="col-12 col-md-4">
                    <label className="form-label">City</label>
                    <select
                      className={`form-select ${errors.cityId ? "is-invalid" : ""}`}
                      value={form.cityId}
                      onChange={(e) => setField("cityId", e.target.value)}
                      disabled={!form.countyId}
                    >
                      <option value="">{form.countyId ? "Select City" : "Select County first"}</option>
                      {cities.map((ct) => (
                        <option key={ct.id} value={ct.id}>
                          {ct.name}
                        </option>
                      ))}
                    </select>
                    {errors.cityId && <div className="invalid-feedback">{errors.cityId}</div>}
                  </div>

                  <div className="col-12 col-md-4">
                    <label className="form-label">Eircode</label>
                    <input
                      className={`form-control ${errors.eircode ? "is-invalid" : ""}`}
                      value={form.eircode}
                      onChange={(e) => setField("eircode", e.target.value.toUpperCase())}
                    />
                    {errors.eircode && <div className="invalid-feedback">{errors.eircode}</div>}
                  </div>
                </div>

                <hr className="my-4" />

                <div className="section-head">
                  <div className="section-title">RISK &amp; UPLOAD</div>
                  <div className="section-line" />
                </div>

                <div className="row g-3 mt-1">
                  <div className="col-12 col-md-6">
                    <label className="form-label">Risk Rating</label>
                    <select
                      className="form-select"
                      value={form.riskRating}
                      onChange={(e) => setField("riskRating", e.target.value)}
                    >
                      <option>Low</option>
                      <option>Medium</option>
                      <option>High</option>
                    </select>
                  </div>

                  <div className="col-12 col-md-6 d-flex align-items-end">
                    <div className="form-check mb-2">
                      <input
                        className="form-check-input"
                        type="checkbox"
                        id="pep"
                        checked={form.pep}
                        onChange={(e) => setField("pep", e.target.checked)}
                      />
                      <label className="form-check-label" htmlFor="pep">
                        Politically Exposed Person (PEP)
                      </label>
                    </div>
                  </div>

                  <div className="col-12">
                    <label className="form-label">Upload Documents (multiple)</label>
                    <input className="form-control" type="file" multiple accept=".pdf,image/*" onChange={onChooseFiles} />
                    <div className="form-text mt-2">Max {MAX_FILE_MB}MB per file (JPG/PNG/PDF).</div>
                    {errors.files && <div className="text-danger small mt-2">{errors.files}</div>}

                    {form.files.length > 0 && (
                      <ul className="list-group list-group-flush mt-3">
                        {form.files.map((f, idx) => (
                          <li key={`${f.name}-${idx}`} className="list-group-item px-0 d-flex justify-content-between">
                            <div className="text-break">
                              <div className="fw-semibold">{f.name}</div>
                              <div className="text-muted small">{(f.size / (1024 * 1024)).toFixed(2)} MB</div>
                            </div>
                            <button type="button" className="btn btn-sm btn-outline-danger" onClick={() => removeFile(idx)}>
                              Remove
                            </button>
                          </li>
                        ))}
                      </ul>
                    )}
                  </div>
                </div>

                <div className="d-flex justify-content-end gap-2 mt-4">
                  <button type="button" className="btn btn-outline-secondary" onClick={onCancel} disabled={isSubmitting}>
                    Cancel
                  </button>
                  <button type="submit" className="btn btn-primary px-4" disabled={isSubmitting}>
                    {isSubmitting ? "Submitting..." : "Submit KYC Upload"}
                  </button>
                </div>
              </form>

              <div className="kyc-tip text-center text-muted mt-4">
                Tip: County/City are pulled from master tables in PostgreSQL.
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}