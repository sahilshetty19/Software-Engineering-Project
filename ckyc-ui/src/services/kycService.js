// src/services/kycService.js

const BASE_URL = "https://localhost:7094";

async function readError(resp) {
  const text = await resp.text().catch(() => "");
  return text || `${resp.status} ${resp.statusText}`;
}

/**
 * Fetch active counties from backend.
 * Expected response: [{ id: "guid", name: "Dublin" }, ...]
 */
export async function getCounties() {
  const resp = await fetch(`${BASE_URL}/api/counties`, {
    method: "GET",
  });

  if (!resp.ok) {
    throw new Error(`getCounties failed: ${await readError(resp)}`);
  }
  return resp.json();
}

/**
 * Fetch cities for a countyId from backend.
 * Expected response: [{ id: "guid", name: "Dublin 1" }, ...]
 */
export async function getCities(countyId) {
  const resp = await fetch(`${BASE_URL}/api/cities?countyId=${encodeURIComponent(countyId)}`, {
    method: "GET",
  });

  if (!resp.ok) {
    throw new Error(`getCities failed: ${await readError(resp)}`);
  }
  return resp.json();
}

/**
 * Upload the KYC form to the server as multipart/form-data.
 * IMPORTANT:
 * - countyId/cityId MUST be GUID strings.
 * - Documents key MUST be "Documents"
 * - Do NOT set Content-Type manually.
 */
export async function uploadKyc(form) {
  const fd = new FormData();

  // Must match C# ViewModel property names EXACTLY:
  fd.append("FirstName", form.firstName ?? "");
  fd.append("LastName", form.lastName ?? "");
  fd.append("DateOfBirth", form.dob ?? ""); // yyyy-mm-dd
  fd.append("PPSN", (form.pps ?? "").toUpperCase());
  fd.append("Email", form.email ?? "");
  fd.append("Phone", form.phone ?? "");
  fd.append("AddressLine1", form.address1 ?? "");
  fd.append("CountyId", form.countyId ?? ""); // GUID
  fd.append("CityId", form.cityId ?? "");     // GUID
  fd.append("Eircode", (form.eircode ?? "").toUpperCase());
  fd.append("IsPEP", String(!!form.pep));
  fd.append("RiskRating", form.riskRating ?? "Low"); // try "0" if enum parsing fails

  (form.files || []).forEach((file) => {
    fd.append("Documents", file);
  });

  // Debug: see what you're sending (optional)
  // for (const [k, v] of fd.entries()) console.log("FD:", k, v);

  const resp = await fetch(`${BASE_URL}/api/upload`, {
    method: "POST",
    body: fd,
  });

  if (!resp.ok) {
    throw new Error(`uploadKyc failed: ${await readError(resp)}`);
  }

  // Your controller might return JSON OR redirect/HTML.
  const contentType = resp.headers.get("content-type") || "";
  if (contentType.includes("application/json")) {
    return resp.json();
  }
  return resp.text();
}