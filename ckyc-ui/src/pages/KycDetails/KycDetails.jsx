import { useEffect, useState, useCallback } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { Container, Row, Col, Card, ListGroup, Form, Button, Badge, Table } from "react-bootstrap";
import {
  getKycById,
  API_BASE_URL,
  getDownloadUrl,
} from "../../services/kycService";

function ReadOnlyField({ label, value }) {
  return (
    <Form.Group className="mb-3">
      <Form.Label>{label}</Form.Label>
      <Form.Control value={value || ""} readOnly />
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

function formatDateTime(value) {
  if (!value) return "-";
  const dt = new Date(value);
  if (Number.isNaN(dt.getTime())) return value;
  return dt.toLocaleString();
}

export default function KycDetails() {
  const { id } = useParams();
  const navigate = useNavigate();

  const [record, setRecord] = useState(null);
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(true);

  const loadRecord = useCallback(async () => {
    try {
      setLoading(true);
      const res = await getKycById(id);
      setRecord(res.data);
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
  if (!record) return <h2 className="p-4">No record found.</h2>;

  return (
    <Container fluid className="p-4">
      <div className="d-flex justify-content-between align-items-center mb-4 flex-wrap gap-2">
        <div>
          <h2 className="mb-1">KYC Details</h2>
          <div className="text-muted">Request Number: {record.kycId}</div>
        </div>

        <div className="d-flex gap-2">
          <Button variant="secondary" onClick={() => navigate("/kyc-records")}>
            Back to KYC Records
          </Button>
          <Button variant="outline-secondary" onClick={() => navigate("/")}>
            Back to Dashboard
          </Button>
        </div>
      </div>

      <Card className="mb-4 shadow-sm">
        <Card.Body>
          <Card.Title className="mb-3">Customer Details</Card.Title>

          <Row>
            <Col md={4}>
              <ReadOnlyField label="First Name" value={record.firstName} />
            </Col>
            <Col md={4}>
              <ReadOnlyField label="Middle Name" value={record.middleName} />
            </Col>
            <Col md={4}>
              <ReadOnlyField label="Last Name" value={record.lastName} />
            </Col>
          </Row>

          <Row>
            <Col md={4}>
              <ReadOnlyField label="Date of Birth" value={record.dateOfBirth} />
            </Col>
            <Col md={4}>
              <ReadOnlyField label="PPS Number" value={record.ppsNumber} />
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
              <ReadOnlyField label="Email Address" value={record.emailAddress} />
            </Col>
            <Col md={6}>
              <ReadOnlyField label="Phone Number" value={record.phoneNumber} />
            </Col>
          </Row>

          <Row>
            <Col md={12}>
              <ReadOnlyField label="Address" value={record.address} />
            </Col>
          </Row>

          <Row>
            <Col md={4}>
              <ReadOnlyField label="County" value={record.county} />
            </Col>
            <Col md={4}>
              <ReadOnlyField label="City" value={record.city} />
            </Col>
            <Col md={4}>
              <ReadOnlyField label="Eircode" value={record.eircode} />
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
            </Card.Body>
          </Card>
        </Col>
      </Row>

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