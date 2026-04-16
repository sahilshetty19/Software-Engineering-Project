import axios from "axios";

export const API_BASE_URL = "https://localhost:7094";

const api = axios.create({
  baseURL: `${API_BASE_URL}/api/kyc`,
});

const bulkApi = axios.create({
  baseURL: `${API_BASE_URL}/api/kyc/bulk-upload`,
});

export const getDashboard = () => api.get("/dashboard");
export const getKycRecords = (params = {}) => api.get("", { params });
export const getKycById = (id) => api.get(`/${id}`);

export const createKycRecord = (formData) =>
  api.post("", formData, {
    headers: {
      "Content-Type": "multipart/form-data",
    },
  });

export const updateFailedKycRecord = (id, formData) =>
  api.post(`/${id}/update-failed`, formData, {
    headers: {
      "Content-Type": "multipart/form-data",
    },
  });

export const restartFailedKycAutomation = (id) =>
  api.post(`/${id}/restart-automation`);

export const getDownloadUrl = (id, fileName) =>
  `${API_BASE_URL}/api/kyc/${id}/download?fileName=${encodeURIComponent(fileName)}`;

export const validatePscFront = (id) => api.post(`/${id}/validate-psc-front`);
export const validatePscBack = (id) => api.post(`/${id}/validate-psc-back`);
export const checkDedupe = (id) => api.post(`/${id}/check-dedupe`);
export const searchCkyc = (id) => api.post(`/${id}/search-ckyc`);
export const downloadCkyc = (id) => api.post(`/${id}/download-ckyc`);
export const generateZip = (id) => api.post(`/${id}/generate-zip`);
export const sendZipToSftp = (id) => api.post(`/${id}/send-zip`);
export const checkCkycStatus = (id) => api.post(`/${id}/check-ckyc-status`);
export const pushToInternal = (id) => api.post(`/${id}/push-to-internal`);

export const getBulkUploadTemplateUrl = () =>
  `${API_BASE_URL}/api/kyc/bulk-upload/template`;

export const uploadBulkZip = (formData) =>
  bulkApi.post("", formData, {
    headers: {
      "Content-Type": "multipart/form-data",
    },
  });

export const getBulkBatches = () => bulkApi.get("");
export const getBulkBatchSummary = (batchId) => bulkApi.get(`/${batchId}`);
export const getBulkBatchRows = (batchId) => bulkApi.get(`/${batchId}/rows`);
export const readBulkBatch = (batchId) => bulkApi.post(`/${batchId}/read`);
export const importBulkBatch = (batchId) => bulkApi.post(`/${batchId}/import`);
