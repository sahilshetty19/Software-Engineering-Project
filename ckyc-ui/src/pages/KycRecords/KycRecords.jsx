import { useCallback, useEffect, useMemo, useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import {
  Container,
  Card,
  Table,
  Form,
  Button,
  Badge,
  Row,
  Col,
  Modal,
  Alert,
  Spinner,
} from "react-bootstrap";
import {
  getKycRecords,
  uploadBulkZip,
  getBulkBatches,
  getBulkBatchSummary,
  getBulkBatchRows,
  readBulkBatch,
  importBulkBatch,
  getBulkUploadTemplateUrl,
} from "../../services/kycService";

function getStatusBadge(status) {
  switch ((status || "").toLowerCase()) {
    case "completed":
      return <Badge bg="success">Completed</Badge>;
    case "pending":
      return (
        <Badge bg="warning" text="dark">
          Pending
        </Badge>
      );
    case "processing":
      return <Badge bg="primary">Processing</Badge>;
    case "failed":
      return <Badge bg="danger">Failed</Badge>;
    default:
      return <Badge bg="secondary">{status || "Unknown"}</Badge>;
  }
}

function getBatchStatusBadge(status) {
  if (typeof status === "number") {
    switch (status) {
      case 0:
        return (
          <Badge bg="warning" text="dark">
            Uploaded
          </Badge>
        );
      case 1:
        return <Badge bg="primary">Processing</Badge>;
      case 2:
        return <Badge bg="success">Completed</Badge>;
      case 3:
        return (
          <Badge bg="warning" text="dark">
            Partially Completed
          </Badge>
        );
      case 4:
        return <Badge bg="danger">Failed</Badge>;
      default:
        return <Badge bg="secondary">{status}</Badge>;
    }
  }

  switch ((status || "").toLowerCase()) {
    case "completed":
      return <Badge bg="success">Completed</Badge>;
    case "processing":
      return <Badge bg="primary">Processing</Badge>;
    case "uploaded":
      return (
        <Badge bg="warning" text="dark">
          Uploaded
        </Badge>
      );
    case "partiallycompleted":
    case "partially completed":
      return (
        <Badge bg="warning" text="dark">
          Partially Completed
        </Badge>
      );
    case "failed":
      return <Badge bg="danger">Failed</Badge>;
    default:
      return <Badge bg="secondary">{status || "Unknown"}</Badge>;
  }
}

function getAutomationBadge(status) {
  switch ((status || "").toLowerCase()) {
    case "completed":
      return <Badge bg="success">Completed</Badge>;
    case "queued":
      return <Badge bg="secondary">Queued</Badge>;
    case "running":
      return <Badge bg="primary">Running</Badge>;
    case "waiting retry":
      return (
        <Badge bg="warning" text="dark">
          Waiting Retry
        </Badge>
      );
    case "terminal failed":
      return <Badge bg="danger">Terminal Failed</Badge>;
    default:
      return <Badge bg="secondary">{status || "Unknown"}</Badge>;
  }
}

function getBulkRowStatusBadge(status) {
  switch ((status || "").toLowerCase()) {
    case "completed":
      return <Badge bg="success">Completed</Badge>;
    case "imported":
      return <Badge bg="info">Imported</Badge>;
    case "processing":
      return <Badge bg="primary">Processing</Badge>;
    case "waiting retry":
      return (
        <Badge bg="warning" text="dark">
          Waiting Retry
        </Badge>
      );
    case "pending":
      return (
        <Badge bg="warning" text="dark">
          Pending
        </Badge>
      );
    case "failed":
      return <Badge bg="danger">Failed</Badge>;
    default:
      return <Badge bg="secondary">{status || "Unknown"}</Badge>;
  }
}

function formatDateTime(value) {
  if (!value) return "-";
  const dt = new Date(value);
  if (Number.isNaN(dt.getTime())) return value;
  return dt.toLocaleString();
}

export default function KycRecords() {
  const [records, setRecords] = useState([]);
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(true);
  const [searchText, setSearchText] = useState("");

  const [showBulkModal, setShowBulkModal] = useState(false);
  const [bulkZipFile, setBulkZipFile] = useState(null);
  const [bulkBatches, setBulkBatches] = useState([]);
  const [selectedBatchId, setSelectedBatchId] = useState("");
  const [selectedBatchSummary, setSelectedBatchSummary] = useState(null);
  const [selectedBatchRows, setSelectedBatchRows] = useState([]);
  const [bulkLoading, setBulkLoading] = useState(false);
  const [bulkMessage, setBulkMessage] = useState("");
  const [bulkError, setBulkError] = useState("");

  const location = useLocation();
  const navigate = useNavigate();

  const query = new URLSearchParams(location.search);
  const status = query.get("status") || "";
  const county = query.get("county") || "";
  const createdDate = query.get("createdDate") || "";
  const transactionStatus = query.get("transactionStatus") || "";

  const loadRecords = useCallback(async () => {
    const params = {
      status: status || undefined,
      county: county || undefined,
      createdDate: createdDate || undefined,
      transactionStatus: transactionStatus || undefined,
    };

    try {
      setLoading(true);
      const res = await getKycRecords(params);
      setRecords(res.data);
      setError("");
    } catch (err) {
      console.error(err);
      setError("Failed to load KYC records.");
    } finally {
      setLoading(false);
    }
  }, [status, county, createdDate, transactionStatus]);

  useEffect(() => {
    loadRecords();
  }, [loadRecords]);

  const filteredRecords = useMemo(() => {
    const search = searchText.trim().toLowerCase();
    const matchingRecords = !search
      ? records
      : records.filter((record) => {
          return (
            record.kycId?.toLowerCase().includes(search) ||
            record.customerName?.toLowerCase().includes(search) ||
            record.ppsNumber?.toLowerCase().includes(search) ||
            record.county?.toLowerCase().includes(search) ||
            record.status?.toLowerCase().includes(search) ||
            record.automationStatus?.toLowerCase().includes(search) ||
            record.lastFailedStep?.toLowerCase().includes(search)
          );
        });

    return [...matchingRecords].sort((left, right) => {
      const leftDate = new Date(left.createdDate || 0).getTime();
      const rightDate = new Date(right.createdDate || 0).getTime();

      if (rightDate !== leftDate) {
        return rightDate - leftDate;
      }

      return (right.kycId || "").localeCompare(left.kycId || "");
    });
  }, [records, searchText]);

  const countyOptions = useMemo(() => {
    return [...new Set(records.map((record) => record.county).filter(Boolean))].sort(
      (left, right) => left.localeCompare(right)
    );
  }, [records]);

  const getFilterText = () => {
    const activeFilters = [];

    if (status) activeFilters.push(`Status = ${status}`);
    if (county) activeFilters.push(`County = ${county}`);
    if (createdDate) activeFilters.push(`Created Date = ${createdDate}`);
    if (transactionStatus) activeFilters.push(`Transaction Status = ${transactionStatus}`);

    return activeFilters.length > 0 ? activeFilters.join(" | ") : "All Records";
  };

  const handleStatusChange = (e) => {
    const newStatus = e.target.value;
    const params = new URLSearchParams(location.search);

    if (newStatus) {
      params.set("status", newStatus);
    } else {
      params.delete("status");
    }

    navigate(`/kyc-records?${params.toString()}`);
  };

  const handleCountyChange = (e) => {
    const newCounty = e.target.value;
    const params = new URLSearchParams(location.search);

    if (newCounty) {
      params.set("county", newCounty);
    } else {
      params.delete("county");
    }

    navigate(`/kyc-records?${params.toString()}`);
  };

  const handleDateChange = (e) => {
    const newDate = e.target.value;
    const params = new URLSearchParams(location.search);

    if (newDate) {
      params.set("createdDate", newDate);
    } else {
      params.delete("createdDate");
    }

    navigate(`/kyc-records?${params.toString()}`);
  };

  const handleClearFilters = () => {
    setSearchText("");
    navigate("/kyc-records");
  };

  const loadBulkBatches = async () => {
    const res = await getBulkBatches();
    setBulkBatches(res.data || []);
  };

  const loadBulkBatchDetails = async (batchId) => {
    if (!batchId) return;

    const [summaryRes, rowsRes] = await Promise.all([
      getBulkBatchSummary(batchId),
      getBulkBatchRows(batchId),
    ]);

    setSelectedBatchId(batchId);
    setSelectedBatchSummary(summaryRes.data);
    setSelectedBatchRows(rowsRes.data || []);
  };

  const handleOpenBulkModal = async () => {
    try {
      setShowBulkModal(true);
      setBulkMessage("");
      setBulkError("");
      await loadBulkBatches();
    } catch (err) {
      console.error(err);
      setBulkError("Failed to load bulk upload batches.");
    }
  };

  const handleCloseBulkModal = () => {
    setShowBulkModal(false);
    setBulkZipFile(null);
    setBulkMessage("");
    setBulkError("");
  };

  const handleUploadBulkZip = async () => {
    if (!bulkZipFile) {
      setBulkError("Please select a ZIP file.");
      return;
    }

    try {
      setBulkLoading(true);
      setBulkMessage("");
      setBulkError("");

      const formData = new FormData();
      formData.append("file", bulkZipFile);

      const res = await uploadBulkZip(formData);
      const newBatchId = res.data?.batchId;

      setBulkMessage(res.data?.message || "Bulk ZIP uploaded successfully.");
      await loadBulkBatches();

      if (newBatchId) {
        await loadBulkBatchDetails(newBatchId);
      }
    } catch (err) {
      console.error(err);
      setBulkError(err?.response?.data || "Bulk ZIP upload failed.");
    } finally {
      setBulkLoading(false);
    }
  };

  const handleReadBatch = async () => {
    if (!selectedBatchId) {
      setBulkError("Please select a batch first.");
      return;
    }

    try {
      setBulkLoading(true);
      setBulkMessage("");
      setBulkError("");

      const res = await readBulkBatch(selectedBatchId);
      setBulkMessage(
        res.data?.message ||
          `Batch read completed. Total: ${res.data?.totalRows}, Valid: ${res.data?.validRows}, Invalid: ${res.data?.invalidRows}`
      );

      await loadBulkBatches();
      await loadBulkBatchDetails(selectedBatchId);
    } catch (err) {
      console.error(err);
      setBulkError(err?.response?.data || "Read batch failed.");
    } finally {
      setBulkLoading(false);
    }
  };

  const handleImportBatch = async () => {
    if (!selectedBatchId) {
      setBulkError("Please select a batch first.");
      return;
    }

    try {
      setBulkLoading(true);
      setBulkMessage("");
      setBulkError("");

      const res = await importBulkBatch(selectedBatchId);
      setBulkMessage(res.data?.message || "Batch import completed successfully.");

      await loadBulkBatches();
      await loadBulkBatchDetails(selectedBatchId);
      await loadRecords();
    } catch (err) {
      console.error(err);
      setBulkError(err?.response?.data || "Import batch failed.");
    } finally {
      setBulkLoading(false);
    }
  };

  const handleDownloadTemplate = () => {
    window.open(getBulkUploadTemplateUrl(), "_blank");
  };

  if (loading) return <h2 className="p-4">Loading KYC records...</h2>;
  if (error) return <h2 className="p-4">{error}</h2>;

  return (
    <Container fluid className="p-4">
      <div className="d-flex justify-content-between align-items-center mb-4">
        <div>
          <h2 className="mb-1">KYC Records</h2>
        </div>
        <div className="d-flex gap-2">
          <Button variant="outline-secondary" onClick={() => navigate("/")}>
            Back to Dashboard
          </Button>
          <Button variant="outline-dark" onClick={handleOpenBulkModal}>
            Bulk Upload
          </Button>
          <Button onClick={() => navigate("/kyc/new")}>+ New KYC Upload</Button>
        </div>
      </div>

      <Card className="shadow-sm">
        <Card.Body>
          <Row className="g-3 align-items-end mb-3">
            <Col md={3}>
              <div>
                <strong>Filter:</strong> {getFilterText()}
                <div>
                  <strong>Total Records:</strong> {filteredRecords.length}
                </div>
              </div>
            </Col>

            <Col md={2}>
              <Form.Group>
                <Form.Label>Status Filter</Form.Label>
                <Form.Select value={status} onChange={handleStatusChange}>
                  <option value="">All Statuses</option>
                  <option value="Pending">Pending</option>
                  <option value="Processing">Processing</option>
                  <option value="Completed">Completed</option>
                  <option value="Failed">Failed</option>
                </Form.Select>
              </Form.Group>
            </Col>

            <Col md={2}>
              <Form.Group>
                <Form.Label>County Filter</Form.Label>
                <Form.Select value={county} onChange={handleCountyChange}>
                  <option value="">All Counties</option>
                  {countyOptions.map((item) => (
                    <option key={item} value={item}>
                      {item}
                    </option>
                  ))}
                </Form.Select>
              </Form.Group>
            </Col>

            <Col md={2}>
              <Form.Group>
                <Form.Label>Created Date</Form.Label>
                <Form.Control type="date" value={createdDate} onChange={handleDateChange} />
              </Form.Group>
            </Col>

            <Col md={3}>
              <Form.Group>
                <Form.Label>Search</Form.Label>
                <Form.Control
                  type="text"
                  placeholder="Search KYC..."
                  value={searchText}
                  onChange={(e) => setSearchText(e.target.value)}
                />
              </Form.Group>
            </Col>
          </Row>

          <div className="mb-3">
            {(status || county || createdDate || transactionStatus) && (
              <Button variant="secondary" onClick={handleClearFilters}>
                Clear Filter
              </Button>
            )}
          </div>

          <Table hover responsive>
            <thead>
              <tr>
                <th>KYC ID</th>
                <th>Customer Name</th>
                <th>PPS Number</th>
                <th>County</th>
                <th>Status</th>
                <th>Automation</th>
                <th>Retries</th>
                <th>Created Date</th>
              </tr>
            </thead>
            <tbody>
              {filteredRecords.map((record) => (
                <tr
                  key={record.id}
                  onClick={() => navigate(`/kyc/${record.id}`)}
                  style={{ cursor: "pointer" }}
                >
                  <td>{record.kycId}</td>
                  <td>{record.customerName}</td>
                  <td>{record.ppsNumber}</td>
                  <td>{record.county}</td>
                  <td>{getStatusBadge(record.status)}</td>
                  <td>{getAutomationBadge(record.automationStatus)}</td>
                  <td>{record.retryAttemptCount}/{record.maxRetryAttempts}</td>
                  <td>{new Date(record.createdDate).toLocaleDateString()}</td>
                </tr>
              ))}

              {filteredRecords.length === 0 && (
                <tr>
                  <td colSpan="8" style={{ textAlign: "center" }}>
                    No records found
                  </td>
                </tr>
              )}
            </tbody>
          </Table>
        </Card.Body>
      </Card>

      <Modal show={showBulkModal} onHide={handleCloseBulkModal} size="xl" centered>
        <Modal.Header closeButton>
          <Modal.Title>Bulk Upload</Modal.Title>
        </Modal.Header>

        <Modal.Body>
          {bulkMessage && <Alert variant="success">{bulkMessage}</Alert>}
          {bulkError && <Alert variant="danger">{bulkError}</Alert>}

          <Card className="mb-4">
            <Card.Body>
              <Row className="g-3 align-items-end">
                <Col md={5}>
                  <Form.Group>
                    <Form.Label>Upload ZIP package</Form.Label>
                    <Form.Control
                      type="file"
                      accept=".zip"
                      onChange={(e) => setBulkZipFile(e.target.files?.[0] || null)}
                    />
                  </Form.Group>
                </Col>

                <Col md={7}>
                  <div className="d-flex flex-wrap gap-2">
                    <Button variant="outline-primary" onClick={handleDownloadTemplate}>
                      Download Template
                    </Button>

                    <Button disabled={bulkLoading} onClick={handleUploadBulkZip}>
                      {bulkLoading ? (
                        <>
                          <Spinner size="sm" className="me-2" />
                          Processing...
                        </>
                      ) : (
                        "Upload ZIP"
                      )}
                    </Button>

                    <Button
                      variant="outline-secondary"
                      disabled={bulkLoading || !selectedBatchId}
                      onClick={handleReadBatch}
                    >
                      Read Batch
                    </Button>

                    <Button
                      variant="outline-success"
                      disabled={bulkLoading || !selectedBatchId}
                      onClick={handleImportBatch}
                    >
                      Import Batch
                    </Button>

                    <Button
                      variant="outline-dark"
                      disabled={bulkLoading}
                      onClick={async () => {
                        try {
                          setBulkLoading(true);
                          await loadBulkBatches();
                          if (selectedBatchId) {
                            await loadBulkBatchDetails(selectedBatchId);
                          }
                        } catch (err) {
                          console.error(err);
                          setBulkError("Failed to refresh batch data.");
                        } finally {
                          setBulkLoading(false);
                        }
                      }}
                    >
                      Refresh
                    </Button>
                  </div>
                </Col>
              </Row>
            </Card.Body>
          </Card>

          <Row className="g-4">
            <Col md={5}>
              <Card>
                <Card.Body>
                  <Card.Title>Recent Batches</Card.Title>
                  <div style={{ maxHeight: "300px", overflowY: "auto" }}>
                    <Table hover responsive size="sm">
                      <thead>
                        <tr>
                          <th>File</th>
                          <th>Status</th>
                          <th>Total</th>
                        </tr>
                      </thead>
                      <tbody>
                        {bulkBatches.map((batch) => (
                          <tr
                            key={batch.bulkUploadBatchId}
                            onClick={() => loadBulkBatchDetails(batch.bulkUploadBatchId)}
                            style={{
                              cursor: "pointer",
                              backgroundColor:
                                selectedBatchId === batch.bulkUploadBatchId ? "#f8f9fa" : "",
                            }}
                          >
                            <td>{batch.originalFileName}</td>
                            <td>{getBatchStatusBadge(batch.status)}</td>
                            <td>{batch.totalRows}</td>
                          </tr>
                        ))}

                        {bulkBatches.length === 0 && (
                          <tr>
                            <td colSpan="3" className="text-center">
                              No batches found
                            </td>
                          </tr>
                        )}
                      </tbody>
                    </Table>
                  </div>
                </Card.Body>
              </Card>
            </Col>

            <Col md={7}>
              <Card>
                <Card.Body>
                  <Card.Title>Batch Summary</Card.Title>

                  {selectedBatchSummary ? (
                    <Row className="g-3">
                      <Col md={6}>
                        <div>
                          <strong>Batch ID:</strong>
                          <div style={{ wordBreak: "break-all" }}>
                            {selectedBatchSummary.bulkUploadBatchId}
                          </div>
                        </div>
                      </Col>
                      <Col md={6}>
                        <div>
                          <strong>File Name:</strong>
                          <div>{selectedBatchSummary.originalFileName}</div>
                        </div>
                      </Col>
                      <Col md={4}>
                        <strong>Status:</strong>
                        <div>{getBatchStatusBadge(selectedBatchSummary.status)}</div>
                      </Col>
                      <Col md={4}>
                        <strong>Total Rows:</strong>
                        <div>{selectedBatchSummary.totalRows}</div>
                      </Col>
                      <Col md={4}>
                        <strong>Success Rows:</strong>
                        <div>{selectedBatchSummary.successRows}</div>
                      </Col>
                      <Col md={4}>
                        <strong>Failed Rows:</strong>
                        <div>{selectedBatchSummary.failedRows}</div>
                      </Col>
                      <Col md={8}>
                        <strong>Failure Reason:</strong>
                        <div>{selectedBatchSummary.failureReason || "-"}</div>
                      </Col>
                    </Row>
                  ) : (
                    <div>Select a batch to view summary.</div>
                  )}
                </Card.Body>
              </Card>
            </Col>
          </Row>

          <Card className="mt-4">
            <Card.Body>
              <Card.Title>Batch Rows</Card.Title>
              <div style={{ maxHeight: "350px", overflowY: "auto" }}>
                <Table hover responsive size="sm">
                  <thead>
                    <tr>
                      <th>Row No</th>
                      <th>RowRef</th>
                      <th>Status</th>
                      <th>Automation</th>
                      <th>Retries</th>
                      <th>Next Retry</th>
                      <th>RequestRef</th>
                      <th>KycUploadId</th>
                      <th>Error</th>
                    </tr>
                  </thead>
                  <tbody>
                    {selectedBatchRows.map((row) => (
                      <tr key={row.bulkUploadRowResultId}>
                        <td>{row.rowNumber}</td>
                        <td>{row.rowRef}</td>
                        <td>{getBulkRowStatusBadge(row.status)}</td>
                        <td>{getAutomationBadge(row.automationStatus)}</td>
                        <td>{row.maxRetryAttempts ? `${row.retryAttemptCount}/${row.maxRetryAttempts}` : "-"}</td>
                        <td>{formatDateTime(row.nextRetryAtUtc)}</td>
                        <td>{row.requestRef || "-"}</td>
                        <td style={{ wordBreak: "break-all" }}>{row.kycUploadId || "-"}</td>
                        <td>{row.lastAutomationError || row.errorMessage || row.lastFailedStep || "-"}</td>
                      </tr>
                    ))}

                    {selectedBatchRows.length === 0 && (
                      <tr>
                        <td colSpan="9" className="text-center">
                          No row results available
                        </td>
                      </tr>
                    )}
                  </tbody>
                </Table>
              </div>
            </Card.Body>
          </Card>
        </Modal.Body>
      </Modal>
    </Container>
  );
}
