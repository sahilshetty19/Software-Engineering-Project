import { useEffect, useState } from "react";
import { Container, Row, Col, Card, Form, Button, Alert } from "react-bootstrap";
import { useNavigate } from "react-router-dom";
import { createKycRecord } from "../../services/kycService";

export default function NewKycUpload() {
  const navigate = useNavigate();

  const [formData, setFormData] = useState({
    firstName: "",
    middleName: "",
    lastName: "",
    dateOfBirth: "",
    ppsNumber: "",
    emailAddress: "",
    phoneNumber: "",
    address: "",
    county: "",
    city: "",
    eircode: "",
  });

  const [files, setFiles] = useState({
    pscFront: null,
    pscBack: null,
  });

  const [previewUrls, setPreviewUrls] = useState({
    pscFront: "",
    pscBack: "",
  });

  const [errors, setErrors] = useState({});
  const [submitMessage, setSubmitMessage] = useState("");

  useEffect(() => {
    const frontUrl = files.pscFront ? URL.createObjectURL(files.pscFront) : "";
    const backUrl = files.pscBack ? URL.createObjectURL(files.pscBack) : "";

    setPreviewUrls({
      pscFront: frontUrl,
      pscBack: backUrl,
    });

    return () => {
      if (frontUrl) URL.revokeObjectURL(frontUrl);
      if (backUrl) URL.revokeObjectURL(backUrl);
    };
  }, [files.pscFront, files.pscBack]);

  const handleChange = (e) => {
    const { name, value } = e.target;
    setFormData((prev) => ({
      ...prev,
      [name]: value,
    }));
  };

  const handleFileChange = (e) => {
    const { name, files: selectedFiles } = e.target;
    setFiles((prev) => ({
      ...prev,
      [name]: selectedFiles && selectedFiles.length > 0 ? selectedFiles[0] : null,
    }));
  };

  const validateForm = () => {
    const newErrors = {};

    if (!formData.firstName.trim()) newErrors.firstName = "First Name is required";
    if (!formData.lastName.trim()) newErrors.lastName = "Last Name is required";
    if (!formData.dateOfBirth) newErrors.dateOfBirth = "Date of Birth is required";
    if (!formData.ppsNumber.trim()) newErrors.ppsNumber = "PPS Number is required";
    if (!formData.emailAddress.trim()) newErrors.emailAddress = "Email Address is required";
    if (!formData.phoneNumber.trim()) newErrors.phoneNumber = "Phone Number is required";
    if (!formData.address.trim()) newErrors.address = "Address is required";
    if (!formData.county.trim()) newErrors.county = "County is required";
    if (!formData.city.trim()) newErrors.city = "City is required";
    if (!formData.eircode.trim()) newErrors.eircode = "Eircode is required";

    if (!files.pscFront) newErrors.pscFront = "PSC Front file is required";
    if (!files.pscBack) newErrors.pscBack = "PSC Back file is required";

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSave = async () => {
  setSubmitMessage("");

  const isValid = validateForm();
  if (!isValid) return;

  try {
    const payload = new FormData();

    payload.append("FirstName", formData.firstName);
    payload.append("MiddleName", formData.middleName);
    payload.append("LastName", formData.lastName);
    payload.append("DateOfBirth", formData.dateOfBirth);
    payload.append("PpsNumber", formData.ppsNumber);
    payload.append("EmailAddress", formData.emailAddress);
    payload.append("PhoneNumber", formData.phoneNumber);
    payload.append("Address", formData.address);
    payload.append("County", formData.county);
    payload.append("City", formData.city);
    payload.append("Eircode", formData.eircode);

    if (files.pscFront) {
      payload.append("PscFront", files.pscFront);
    }

    if (files.pscBack) {
      payload.append("PscBack", files.pscBack);
    }

    const response = await createKycRecord(payload);

    setSubmitMessage(response.data.message || "KYC record created successfully");

    if (response.data.id) {
      navigate(`/kyc/${response.data.id}`);
    }
  } catch (error) {
    console.error(error);
    setSubmitMessage("Failed to create KYC record.");
  }
};

  return (
    <Container fluid className="p-4">
      <div className="d-flex justify-content-between align-items-center mb-4 flex-wrap gap-2">
        <div>
          <h2 className="mb-1">New KYC Upload</h2>
        </div>

        <Button variant="outline-secondary" onClick={() => navigate("/")}>
          Back to Dashboard
        </Button>
      </div>

      <Card className="mb-4 shadow-sm">
        <Card.Body>
          <Card.Title className="mb-3">Customer Details</Card.Title>

          <Row>
            <Col md={4}>
              <Form.Group className="mb-3">
                <Form.Label>First Name</Form.Label>
                <Form.Control
                  type="text"
                  name="firstName"
                  value={formData.firstName}
                  onChange={handleChange}
                  isInvalid={!!errors.firstName}
                />
                <Form.Control.Feedback type="invalid">
                  {errors.firstName}
                </Form.Control.Feedback>
              </Form.Group>
            </Col>

            <Col md={4}>
              <Form.Group className="mb-3">
                <Form.Label>Middle Name</Form.Label>
                <Form.Control
                  type="text"
                  name="middleName"
                  value={formData.middleName}
                  onChange={handleChange}
                />
              </Form.Group>
            </Col>

            <Col md={4}>
              <Form.Group className="mb-3">
                <Form.Label>Last Name</Form.Label>
                <Form.Control
                  type="text"
                  name="lastName"
                  value={formData.lastName}
                  onChange={handleChange}
                  isInvalid={!!errors.lastName}
                />
                <Form.Control.Feedback type="invalid">
                  {errors.lastName}
                </Form.Control.Feedback>
              </Form.Group>
            </Col>
          </Row>

          <Row>
            <Col md={4}>
              <Form.Group className="mb-3">
                <Form.Label>Date of Birth</Form.Label>
                <Form.Control
                  type="date"
                  name="dateOfBirth"
                  value={formData.dateOfBirth}
                  onChange={handleChange}
                  isInvalid={!!errors.dateOfBirth}
                />
                <Form.Control.Feedback type="invalid">
                  {errors.dateOfBirth}
                </Form.Control.Feedback>
              </Form.Group>
            </Col>

            <Col md={4}>
              <Form.Group className="mb-3">
                <Form.Label>PPS Number</Form.Label>
                <Form.Control
                  type="text"
                  name="ppsNumber"
                  value={formData.ppsNumber}
                  onChange={handleChange}
                  isInvalid={!!errors.ppsNumber}
                />
                <Form.Control.Feedback type="invalid">
                  {errors.ppsNumber}
                </Form.Control.Feedback>
              </Form.Group>
            </Col>

            <Col md={4}>
              <Form.Group className="mb-3">
                <Form.Label>Email Address</Form.Label>
                <Form.Control
                  type="email"
                  name="emailAddress"
                  value={formData.emailAddress}
                  onChange={handleChange}
                  isInvalid={!!errors.emailAddress}
                />
                <Form.Control.Feedback type="invalid">
                  {errors.emailAddress}
                </Form.Control.Feedback>
              </Form.Group>
            </Col>
          </Row>

          <Row>
            <Col md={6}>
              <Form.Group className="mb-3">
                <Form.Label>Phone Number</Form.Label>
                <Form.Control
                  type="text"
                  name="phoneNumber"
                  value={formData.phoneNumber}
                  onChange={handleChange}
                  isInvalid={!!errors.phoneNumber}
                />
                <Form.Control.Feedback type="invalid">
                  {errors.phoneNumber}
                </Form.Control.Feedback>
              </Form.Group>
            </Col>

            <Col md={6}>
              <Form.Group className="mb-3">
                <Form.Label>Address</Form.Label>
                <Form.Control
                  type="text"
                  name="address"
                  value={formData.address}
                  onChange={handleChange}
                  isInvalid={!!errors.address}
                />
                <Form.Control.Feedback type="invalid">
                  {errors.address}
                </Form.Control.Feedback>
              </Form.Group>
            </Col>
          </Row>

          <Row>
            <Col md={4}>
              <Form.Group className="mb-3">
                <Form.Label>County</Form.Label>
                <Form.Control
                  type="text"
                  name="county"
                  value={formData.county}
                  onChange={handleChange}
                  isInvalid={!!errors.county}
                />
                <Form.Control.Feedback type="invalid">
                  {errors.county}
                </Form.Control.Feedback>
              </Form.Group>
            </Col>

            <Col md={4}>
              <Form.Group className="mb-3">
                <Form.Label>City</Form.Label>
                <Form.Control
                  type="text"
                  name="city"
                  value={formData.city}
                  onChange={handleChange}
                  isInvalid={!!errors.city}
                />
                <Form.Control.Feedback type="invalid">
                  {errors.city}
                </Form.Control.Feedback>
              </Form.Group>
            </Col>

            <Col md={4}>
              <Form.Group className="mb-3">
                <Form.Label>Eircode</Form.Label>
                <Form.Control
                  type="text"
                  name="eircode"
                  value={formData.eircode}
                  onChange={handleChange}
                  isInvalid={!!errors.eircode}
                />
                <Form.Control.Feedback type="invalid">
                  {errors.eircode}
                </Form.Control.Feedback>
              </Form.Group>
            </Col>
          </Row>
        </Card.Body>
      </Card>

      <Row className="g-4 mb-4">
        <Col md={6}>
          <Card className="shadow-sm h-100">
            <Card.Body>
              <Card.Title>PSC Front</Card.Title>
              <Form.Group className="mb-3">
                <Form.Control
                  type="file"
                  name="pscFront"
                  accept="image/*"
                  onChange={handleFileChange}
                  isInvalid={!!errors.pscFront}
                />
                <Form.Control.Feedback type="invalid">
                  {errors.pscFront}
                </Form.Control.Feedback>
              </Form.Group>

              <div
                style={{
                  height: "180px",
                  border: "1px dashed #ccc",
                  borderRadius: "8px",
                  display: "flex",
                  alignItems: "center",
                  justifyContent: "center",
                  backgroundColor: "#f8f9fa",
                  marginBottom: "12px",
                  overflow: "hidden",
                }}
              >
                {previewUrls.pscFront ? (
                  <img
                    src={previewUrls.pscFront}
                    alt="PSC Front Preview"
                    style={{ maxWidth: "100%", maxHeight: "100%", objectFit: "contain" }}
                  />
                ) : (
                  "PSC Front Preview Placeholder"
                )}
              </div>

              <div>
                <strong>Selected File:</strong>{" "}
                {files.pscFront ? files.pscFront.name : "No file selected"}
              </div>
            </Card.Body>
          </Card>
        </Col>

        <Col md={6}>
          <Card className="shadow-sm h-100">
            <Card.Body>
              <Card.Title>PSC Back</Card.Title>
              <Form.Group className="mb-3">
                <Form.Control
                  type="file"
                  name="pscBack"
                  accept="image/*"
                  onChange={handleFileChange}
                  isInvalid={!!errors.pscBack}
                />
                <Form.Control.Feedback type="invalid">
                  {errors.pscBack}
                </Form.Control.Feedback>
              </Form.Group>

              <div
                style={{
                  height: "180px",
                  border: "1px dashed #ccc",
                  borderRadius: "8px",
                  display: "flex",
                  alignItems: "center",
                  justifyContent: "center",
                  backgroundColor: "#f8f9fa",
                  marginBottom: "12px",
                  overflow: "hidden",
                }}
              >
                {previewUrls.pscBack ? (
                  <img
                    src={previewUrls.pscBack}
                    alt="PSC Back Preview"
                    style={{ maxWidth: "100%", maxHeight: "100%", objectFit: "contain" }}
                  />
                ) : (
                  "PSC Back Preview Placeholder"
                )}
              </div>

              <div>
                <strong>Selected File:</strong>{" "}
                {files.pscBack ? files.pscBack.name : "No file selected"}
              </div>
            </Card.Body>
          </Card>
        </Col>
      </Row>

      {Object.keys(errors).length > 0 && (
        <Alert variant="danger">
          Please correct the highlighted fields before saving.
        </Alert>
      )}

      {submitMessage && (
        <Alert variant="info" className="mb-4">
          {submitMessage}
        </Alert>
      )}


      <div className="d-flex gap-2">
        <Button variant="primary" onClick={handleSave}>
          Save KYC Record
        </Button>
        <Button variant="secondary" onClick={() => navigate("/kyc-records")}>
          Cancel
        </Button>
      </div>
    </Container>
  );
}