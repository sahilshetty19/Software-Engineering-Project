import { useEffect, useState, useCallback } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { Container, Row, Col, Card, ListGroup, Form, Button, Badge, Table, Alert } from "react-bootstrap";
import {
  getKycById,
  API_BASE_URL,
  getDownloadUrl,
  updateFailedKycRecord,
  restartFailedKycAutomation,
} from "../../services/kycService";

function ReadOnlyField({ label, value }) {
  return (
    <Form.Group className="mb-3">
      <Form.Label>{label}</Form.Label>
      <Form.Control value={value || ""} readOnly />
    </Form.Group>
  );
}

function EditableField({ label, name, value, onChange, type = "text" }) {
  return (
    <Form.Group className="mb-3">
      <Form.Label>{label}</Form.Label>
      <Form.Control type={type} name={name} value={value || ""} onChange={onChange} />
    </Form.Group>
  );
}

function FileSection({ title, files, recordId }) {
  const handleDownload = (file) => {
    window.open(getDownloadUrl(recordId, file), "_blank");
  };

  return (
    <Card className="mb-4 shadow-sm">
      <Card.Body>
        <Card.Title>{title}</Card.Title>
        <ListGroup variant="flush">
          {files && files.length > 0 ? (
            files.map((file, index) => (
              <ListGroup.Item
                key={index}
                className="d-flex justify-content-between align-items-center"
              >
                <span>{file}</span>
                <Button
                  variant="outline-secondary"
                  size="sm"
                  onClick={() => handleDownload(file)}
                >
                  Download
                </Button>
              </ListGroup.Item>
            ))
          ) : (
            <ListGroup.Item>No files available</ListGroup.Item>
          )}
        </ListGroup>
      </Card.Body>
    </Card>
  );
}

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

function getWorkflowStepBadge(status) {
  switch ((status || "").toLowerCase()) {
    case "success":
      return <Badge bg="success">Success</Badge>;
    case "failed":
      return <Badge bg="danger">Failed</Badge>;
    case "started":
      return <Badge bg="primary">Started</Badge>;
    case "skipped":
      return (
        <Badge bg="warning" text="dark">
          Skipped
        </Badge>
      );
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

function formatDateTime(value) {
  if (!value) return "-";
  const dt = new Date(value);
  if (Number.isNaN(dt.getTime())) return value;
  return dt.toLocaleString();
}

function buildEditState(record) {
  return {
    firstName: record.firstName || "",
    middleName: record.middleName || "",
    lastName: record.lastName || "",
    dateOfBirth: record.dateOfBirth || "",
    ppsNumber: record.ppsNumber || "",
    emailAddress: record.emailAddress || "",
    phoneNumber: record.phoneNumber || "",
    address: record.address || "",
    county: record.county || "",
    city: record.city || "",
    eircode: record.eircode || "",
    riskRating: record.riskRating || "Low",
    isPEP: Boolean(record.isPEP),
  };
}

export default function KycDetails() {
  const { id } = useParams();
  const navigate = useNavigate();

  const [record, setRecord] = useState(null);
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(true);
  const [editMode, setEditMode] = useState(false);
  const [editForm, setEditForm] = useState(null);
  const [replacementFiles, setReplacementFiles] = useState({ pscFront: null, pscBack: null });
  const [actionMessage, setActionMessage] = useState("");
  const [actionVariant, setActionVariant] = useState("success");
  const [isSaving, setIsSaving] = useState(false);
  const [isRestarting, setIsRestarting] = useState(false);

  const loadRecord = useCallback(async () => {
    try {
      setLoading(true);
      const res = await getKycById(id);
      setRecord(res.data);
      setEditForm(buildEditState(res.data));
      setReplacementFiles({ pscFront: null, pscBack: null });
      setError("");
    } catch (err) {
      console.error(err);
      setError("Failed to load KYC details.");
    } finally {
      setLoading(false);
    }
  }, [id]);

  useEffect(() => {
    loadRecord();
  }, [loadRecord]);

  if (loading) return <h2 className="p-4">Loading KYC details...</h2>;
  if (error) return <h2 className="p-4">{error}</h2>;
  if (!record || !editForm) return <h2 className="p-4">No record found.</h2>;

  const requestRef = record.requestRef || record.kycId || "-";
  const canRecover = Boolean(record.canEditAfterFailure || record.canRestartAutomation);
  const hasReplacementFiles = Boolean(replacementFiles.pscFront || replacementFiles.pscBack);

  const handleFieldChange = (event) => {
    const { name, value, type, checked } = event.target;
    setEditForm((prev) => ({
      ...prev,
      [name]: type === "checkbox" ? checked : value,
    }));
  };

  const handleFileChange = (event) => {
    const { name, files } = event.target;
    setReplacementFiles((prev) => ({
      ...prev,
      [name]: files && files.length > 0 ? files[0] : null,
    }));
  };

  const handleCancelEdit = () => {
    setEditForm(buildEditState(record));
    setReplacementFiles({ pscFront: null, pscBack: null });
    setEditMode(false);
    setActionMessage("");
  };

  const handleSaveCorrections = async () => {
    try {
      setIsSaving(true);
      setActionMessage("");

      const formData = new FormData();
      formData.append("FirstName", editForm.firstName);
      formData.append("MiddleName", editForm.middleName);
      formData.append("LastName", editForm.lastName);
      formData.append("DateOfBirth", editForm.dateOfBirth);
      formData.append("PpsNumber", editForm.ppsNumber);
      formData.append("EmailAddress", editForm.emailAddress);
      formData.append("PhoneNumber", editForm.phoneNumber);
      formData.append("Address", editForm.address);
      formData.append("County", editForm.county);
      formData.append("City", editForm.city);
      formData.append("Eircode", editForm.eircode);
      formData.append("RiskRating", editForm.riskRating);
      formData.append("IsPEP", String(editForm.isPEP));

      if (replacementFiles.pscFront) {
        formData.append("PscFront", replacementFiles.pscFront);
      }

      if (replacementFiles.pscBack) {
        formData.append("PscBack", replacementFiles.pscBack);
      }

      const response = await updateFailedKycRecord(id, formData);
      setActionVariant("success");
      setActionMessage(response.data.message || "Failed record updated successfully.");
      setEditMode(false);
      await loadRecord();
    } catch (err) {
      console.error(err);
      setActionVariant("danger");
      setActionMessage(err?.response?.data || "Failed to save corrections.");
    } finally {
      setIsSaving(false);
    }
  };

  const handleRestart = async () => {
    try {
      setIsRestarting(true);
      setActionMessage("");
      const response = await restartFailedKycAutomation(id);
      setActionVariant("success");
      setActionMessage(
        response.data.message ||
          "Automation restarted successfully. Refresh after a short wait to see the latest status."
      );
      await loadRecord();
    } catch (err) {
      console.error(err);
      setActionVariant("danger");
      setActionMessage(err?.response?.data?.message || "Failed to restart automation.");
      await loadRecord();
    } finally {
      setIsRestarting(false);
    }
  };

  return (
    <Container fluid className="p-4">
      <div className="d-flex justify-content-between align-items-center mb-4 flex-wrap gap-2">
        <div>
          <h2 className="mb-1">KYC Details</h2>
          <div className="text-muted">Request Number: {requestRef}</div>
        </div>

        <div className="d-flex gap-2 flex-wrap">
          {canRecover && !editMode && (
            <Button variant="outline-primary" onClick={() => setEditMode(true)}>
              Edit Failed Record
            </Button>
          )}
          {canRecover && !editMode && (
            <Button variant="warning" onClick={handleRestart} disabled={isRestarting}>
              {isRestarting ? "Restarting..." : "Restart Processing"}
            </Button>
          )}
          <Button variant="secondary" onClick={() => navigate("/kyc-records")}>
            Back to KYC Records
          </Button>
          <Button variant="outline-secondary" onClick={() => navigate("/")}>
            Back to Dashboard
          </Button>
        </div>
      </div>

      {actionMessage && (
        <Alert variant={actionVariant} className="mb-4">
          {actionMessage}
        </Alert>
      )}

      {canRecover && (
        <Alert variant="warning" className="mb-4">
          This record is in a permanent failed state. Correct the record details or replace the PSC
          documents, save the changes, and then use <strong>Restart Processing</strong> to rerun
          automation from the beginning.
        </Alert>
      )}

      <Card className="mb-4 shadow-sm">
        <Card.Body>
          <Card.Title className="mb-3">Customer Details</Card.Title>

          <Row>
            <Col md={4}>
              {editMode ? (
                <EditableField
                  label="First Name"
                  name="firstName"
                  value={editForm.firstName}
                  onChange={handleFieldChange}
                />
              ) : (
                <ReadOnlyField label="First Name" value={record.firstName} />
              )}
            </Col>
            <Col md={4}>
              {editMode ? (
                <EditableField
                  label="Middle Name"
                  name="middleName"
                  value={editForm.middleName}
                  onChange={handleFieldChange}
                />
              ) : (
                <ReadOnlyField label="Middle Name" value={record.middleName} />
              )}
            </Col>
            <Col md={4}>
              {editMode ? (
                <EditableField
                  label="Last Name"
                  name="lastName"
                  value={editForm.lastName}
                  onChange={handleFieldChange}
                />
              ) : (
                <ReadOnlyField label="Last Name" value={record.lastName} />
              )}
            </Col>
          </Row>

          <Row>
            <Col md={4}>
              {editMode ? (
                <EditableField
                  label="Date of Birth"
                  name="dateOfBirth"
                  type="date"
                  value={editForm.dateOfBirth}
                  onChange={handleFieldChange}
                />
              ) : (
                <ReadOnlyField label="Date of Birth" value={record.dateOfBirth} />
              )}
            </Col>
            <Col md={4}>
              {editMode ? (
                <EditableField
                  label="PPS Number"
                  name="ppsNumber"
                  value={editForm.ppsNumber}
                  onChange={handleFieldChange}
                />
              ) : (
                <ReadOnlyField label="PPS Number" value={record.ppsNumber} />
              )}
            </Col>
            <Col md={4}>
              <Form.Group className="mb-3">
                <Form.Label>Status</Form.Label>
                <div>{getStatusBadge(record.status)}</div>
              </Form.Group>
            </Col>
          </Row>

          <Row>
            <Col md={6}>
              {editMode ? (
                <EditableField
                  label="Email Address"
                  name="emailAddress"
                  type="email"
                  value={editForm.emailAddress}
                  onChange={handleFieldChange}
                />
              ) : (
                <ReadOnlyField label="Email Address" value={record.emailAddress} />
              )}
            </Col>
            <Col md={6}>
              {editMode ? (
                <EditableField
                  label="Phone Number"
                  name="phoneNumber"
                  value={editForm.phoneNumber}
                  onChange={handleFieldChange}
                />
              ) : (
                <ReadOnlyField label="Phone Number" value={record.phoneNumber} />
              )}
            </Col>
          </Row>

          <Row>
            <Col md={12}>
              {editMode ? (
                <EditableField
                  label="Address"
                  name="address"
                  value={editForm.address}
                  onChange={handleFieldChange}
                />
              ) : (
                <ReadOnlyField label="Address" value={record.address} />
              )}
            </Col>
          </Row>

          <Row>
            <Col md={4}>
              {editMode ? (
                <EditableField
                  label="County"
                  name="county"
                  value={editForm.county}
                  onChange={handleFieldChange}
                />
              ) : (
                <ReadOnlyField label="County" value={record.county} />
              )}
            </Col>
            <Col md={4}>
              {editMode ? (
                <EditableField
                  label="City"
                  name="city"
                  value={editForm.city}
                  onChange={handleFieldChange}
                />
              ) : (
                <ReadOnlyField label="City" value={record.city} />
              )}
            </Col>
            <Col md={4}>
              {editMode ? (
                <EditableField
                  label="Eircode"
                  name="eircode"
                  value={editForm.eircode}
                  onChange={handleFieldChange}
                />
              ) : (
                <ReadOnlyField label="Eircode" value={record.eircode} />
              )}
            </Col>
          </Row>

          <Row>
            <Col md={4}>
              {editMode ? (
                <Form.Group className="mb-3">
                  <Form.Label>Risk Rating</Form.Label>
                  <Form.Select name="riskRating" value={editForm.riskRating} onChange={handleFieldChange}>
                    <option value="Low">Low</option>
                    <option value="Medium">Medium</option>
                    <option value="High">High</option>
                  </Form.Select>
                </Form.Group>
              ) : (
                <ReadOnlyField label="Risk Rating" value={record.riskRating} />
              )}
            </Col>
            <Col md={4}>
              {editMode ? (
                <Form.Group className="mb-3">
                  <Form.Label>PEP</Form.Label>
                  <div className="pt-2">
                    <Form.Check
                      type="checkbox"
                      name="isPEP"
                      label="Is Politically Exposed Person"
                      checked={editForm.isPEP}
                      onChange={handleFieldChange}
                    />
                  </div>
                </Form.Group>
              ) : (
                <ReadOnlyField label="PEP" value={record.isPEP ? "Yes" : "No"} />
              )}
            </Col>
          </Row>

          {editMode && (
            <div className="d-flex gap-2 flex-wrap">
              <Button onClick={handleSaveCorrections} disabled={isSaving}>
                {isSaving ? "Saving..." : "Save Corrections"}
              </Button>
              <Button variant="outline-secondary" onClick={handleCancelEdit} disabled={isSaving}>
                Cancel
              </Button>
            </div>
          )}
        </Card.Body>
      </Card>

      <Card className="mb-4 shadow-sm">
        <Card.Body>
          <Card.Title className="mb-3">Automation Status</Card.Title>

          <Row>
            <Col md={4}>
              <Form.Group className="mb-3">
                <Form.Label>Automation</Form.Label>
                <div>{getAutomationBadge(record.automationStatus)}</div>
              </Form.Group>
            </Col>
            <Col md={4}>
              <ReadOnlyField
                label="Retry Attempts"
                value={`${record.retryAttemptCount}/${record.maxRetryAttempts}`}
              />
            </Col>
            <Col md={4}>
              <ReadOnlyField
                label="Next Retry"
                value={formatDateTime(record.nextRetryAtUtc)}
              />
            </Col>
          </Row>

          <Row>
            <Col md={6}>
              <ReadOnlyField label="Last Failed Step" value={record.lastFailedStep} />
            </Col>
            <Col md={6}>
              <ReadOnlyField label="Last Automation Error" value={record.lastAutomationError} />
            </Col>
          </Row>
        </Card.Body>
      </Card>

      <Card className="mb-4 shadow-sm">
        <Card.Body>
          <Card.Title className="mb-3">Workflow Execution Log</Card.Title>

          <Table hover responsive>
            <thead>
              <tr>
                <th>Step</th>
                <th>Status</th>
                <th>Message</th>
                <th>Error Details</th>
                <th>Started At</th>
                <th>Completed At</th>
              </tr>
            </thead>
            <tbody>
              {record.workflowLogs && record.workflowLogs.length > 0 ? (
                record.workflowLogs.map((log, index) => (
                  <tr key={`${log.stepName}-${log.startedAtUtc}-${index}`}>
                    <td>{log.stepName}</td>
                    <td>{getWorkflowStepBadge(log.status)}</td>
                    <td>{log.message || "-"}</td>
                    <td>{log.errorDetails || "-"}</td>
                    <td>{formatDateTime(log.startedAtUtc)}</td>
                    <td>{formatDateTime(log.completedAtUtc)}</td>
                  </tr>
                ))
              ) : (
                <tr>
                  <td colSpan="6" className="text-center">
                    No workflow logs available
                  </td>
                </tr>
              )}
            </tbody>
          </Table>
        </Card.Body>
      </Card>

      <h4 className="mb-3">Images</h4>
      <Row className="mb-4">
        <Col md={6}>
          <Card className="h-100 shadow-sm">
            <Card.Body>
              <Card.Title>PSC Front</Card.Title>
              <div
                style={{
                  height: "180px",
                  border: "1px solid #ddd",
                  borderRadius: "8px",
                  marginBottom: "12px",
                  backgroundColor: "#f8f9fa",
                  overflow: "hidden",
                  display: "flex",
                  alignItems: "center",
                  justifyContent: "center",
                }}
              >
                {record.pscFrontImageUrl ? (
                  <img
                    src={`${API_BASE_URL}${record.pscFrontImageUrl}`}
                    alt="PSC Front"
                    style={{
                      maxWidth: "100%",
                      maxHeight: "100%",
                      objectFit: "contain",
                    }}
                  />
                ) : (
                  "No preview available"
                )}
              </div>
              <div>{record.pscFrontFileName}</div>
              {editMode && (
                <Form.Group className="mt-3">
                  <Form.Label>Replace PSC Front</Form.Label>
                  <Form.Control type="file" name="pscFront" onChange={handleFileChange} />
                  {replacementFiles.pscFront && (
                    <div className="text-muted small mt-2">
                      Selected: {replacementFiles.pscFront.name}
                    </div>
                  )}
                </Form.Group>
              )}
            </Card.Body>
          </Card>
        </Col>

        <Col md={6}>
          <Card className="h-100 shadow-sm">
            <Card.Body>
              <Card.Title>PSC Back</Card.Title>
              <div
                style={{
                  height: "180px",
                  border: "1px solid #ddd",
                  borderRadius: "8px",
                  marginBottom: "12px",
                  backgroundColor: "#f8f9fa",
                  overflow: "hidden",
                  display: "flex",
                  alignItems: "center",
                  justifyContent: "center",
                }}
              >
                {record.pscBackImageUrl ? (
                  <img
                    src={`${API_BASE_URL}${record.pscBackImageUrl}`}
                    alt="PSC Back"
                    style={{
                      maxWidth: "100%",
                      maxHeight: "100%",
                      objectFit: "contain",
                    }}
                  />
                ) : (
                  "No preview available"
                )}
              </div>
              <div>{record.pscBackFileName}</div>
              {editMode && (
                <Form.Group className="mt-3">
                  <Form.Label>Replace PSC Back</Form.Label>
                  <Form.Control type="file" name="pscBack" onChange={handleFileChange} />
                  {replacementFiles.pscBack && (
                    <div className="text-muted small mt-2">
                      Selected: {replacementFiles.pscBack.name}
                    </div>
                  )}
                </Form.Group>
              )}
            </Card.Body>
          </Card>
        </Col>
      </Row>

      {editMode && hasReplacementFiles && (
        <Alert variant="info" className="mb-4">
          Replacement PSC files have been selected. Save corrections first, then restart processing.
        </Alert>
      )}

      <FileSection
        title="Search Response"
        files={record.searchResponses}
        recordId={record.id}
      />

      {record.searchFound === true && (
        <FileSection
          title="Download Response"
          files={record.downloadResponses}
          recordId={record.id}
        />
      )}

      {record.zipFiles && record.zipFiles.length > 0 && (
        <FileSection
          title="ZIP File"
          files={record.zipFiles}
          recordId={record.id}
        />
      )}

      {record.searchFound === false && (
        <FileSection
          title="CKYC Processing Status"
          files={record.processingStatusFiles}
          recordId={record.id}
        />
      )}
    </Container>
  );
}
