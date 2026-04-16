import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { Container, Row, Col, Card, Button, Table, ListGroup, Badge } from "react-bootstrap";
import { Doughnut, Line, Bar } from "react-chartjs-2";
import {
  Chart as ChartJS,
  ArcElement,
  Tooltip,
  Legend,
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  BarElement,
} from "chart.js";
import { getDashboard } from "../../services/kycService";

ChartJS.register(
  ArcElement,
  Tooltip,
  Legend,
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  BarElement
);

function SummaryCard({ title, count, onClick }) {
  return (
    <Card
      className="h-100 shadow-sm"
      onClick={onClick}
      style={{ cursor: "pointer" }}
    >
      <Card.Body>
        <Card.Title>{title}</Card.Title>
        <h3 className="mb-0">{count}</h3>
      </Card.Body>
    </Card>
  );
}

function getStatusBadge(status) {
  switch ((status || "").toLowerCase()) {
    case "completed":
      return <Badge bg="success">Completed</Badge>;
    case "pending":
      return <Badge bg="warning" text="dark">Pending</Badge>;
    case "processing":
      return <Badge bg="primary">Processing</Badge>;
    case "failed":
      return <Badge bg="danger">Failed</Badge>;
    default:
      return <Badge bg="secondary">{status || "Unknown"}</Badge>;
  }
}

export default function Dashboard() {
  const [data, setData] = useState(null);
  const [error, setError] = useState("");
  const navigate = useNavigate();

  useEffect(() => {
    getDashboard()
      .then((res) => setData(res.data))
      .catch((err) => {
        console.error(err);
        setError("Failed to load dashboard data.");
      });
  }, []);

  if (error) return <h2 className="p-4">{error}</h2>;
  if (!data) return <h2 className="p-4">Loading dashboard...</h2>;

  const statusChartData = {
    labels: data.statusDistribution.map((item) => item.label),
    datasets: [
      {
        data: data.statusDistribution.map((item) => item.count),
      },
    ],
  };

  const uploadTrendData = {
    labels: data.uploadTrend.map((item) => item.label),
    datasets: [
      {
        label: "Uploads",
        data: data.uploadTrend.map((item) => item.count),
      },
    ],
  };

  const countyChartData = {
    labels: data.countyDistribution.map((item) => item.label),
    datasets: [
      {
        label: "Records",
        data: data.countyDistribution.map((item) => item.count),
      },
    ],
  };

  const transactionChartData = {
    labels: data.transactionStats.map((item) => item.label),
    datasets: [
      {
        data: data.transactionStats.map((item) => item.count),
      },
    ],
  };

  const handleTransactionClick = (label) => {
    navigate(`/kyc-records?transactionStatus=${label}`);
  };

  return (
    <Container fluid className="p-4">
      <div className="d-flex justify-content-between align-items-center mb-4">
        <div>
          <h2 className="mb-1">Dashboard</h2>
        </div>
        <div className="d-flex gap-2">
          <Button variant="outline-secondary" onClick={() => navigate("/kyc-records")}>
            View KYC Records
          </Button>
          <Button onClick={() => navigate("/kyc/new")}>+ New KYC Upload</Button>
        </div>
      </div>

      <Row className="g-3 mb-4">
        <Col md={2}>
          <SummaryCard
            title="Total Records"
            count={data.summary.total}
            onClick={() => navigate("/kyc-records")}
          />
        </Col>
        <Col md={2}>
          <SummaryCard
            title="Pending"
            count={data.summary.pending}
            onClick={() => navigate("/kyc-records?status=Pending")}
          />
        </Col>
        <Col md={2}>
          <SummaryCard
            title="Processing"
            count={data.summary.processing}
            onClick={() => navigate("/kyc-records?status=Processing")}
          />
        </Col>
        <Col md={2}>
          <SummaryCard
            title="Completed"
            count={data.summary.completed}
            onClick={() => navigate("/kyc-records?status=Completed")}
          />
        </Col>
        <Col md={2}>
          <SummaryCard
            title="Failed"
            count={data.summary.failed}
            onClick={() => navigate("/kyc-records?status=Failed")}
          />
        </Col>
      </Row>

      <Row className="g-4 mb-4">
        <Col md={6}>
          <Card className="shadow-sm">
            <Card.Body>
              <Card.Title>KYC Status Distribution</Card.Title>
              <div style={{ maxWidth: "400px" }}>
                <Doughnut
                  data={statusChartData}
                  options={{
                    onClick: (event, elements) => {
                      if (!elements.length) return;
                      const index = elements[0].index;
                      const selectedStatus = data.statusDistribution[index].label;
                      navigate(`/kyc-records?status=${selectedStatus}`);
                    },
                    plugins: {
                      legend: {
                        position: "top",
                      },
                    },
                  }}
                />
              </div>
            </Card.Body>
          </Card>
        </Col>

        <Col md={6}>
          <Card className="shadow-sm">
            <Card.Body>
              <Card.Title>KYC Upload Trend</Card.Title>
              <Line
                data={uploadTrendData}
                options={{
                  onClick: (event, elements) => {
                    if (!elements.length) return;
                    const index = elements[0].index;
                    const selectedDate = data.uploadTrend[index].label;
                    navigate(`/kyc-records?createdDate=${selectedDate}`);
                  },
                  plugins: {
                    legend: {
                      position: "top",
                    },
                  },
                }}
              />
            </Card.Body>
          </Card>
        </Col>

        <Col md={6}>
          <Card className="shadow-sm">
            <Card.Body>
              <Card.Title>County-wise KYC Distribution</Card.Title>
              <Bar
                data={countyChartData}
                options={{
                  onClick: (event, elements) => {
                    if (!elements.length) return;
                    const index = elements[0].index;
                    const selectedCounty = data.countyDistribution[index].label;
                    navigate(`/kyc-records?county=${selectedCounty}`);
                  },
                  plugins: {
                    legend: {
                      position: "top",
                    },
                  },
                }}
              />
            </Card.Body>
          </Card>
        </Col>

        <Col md={6}>
          <Card className="shadow-sm">
            <Card.Body>
              <Card.Title>CKYC Transaction Status</Card.Title>
              <div style={{ maxWidth: "400px" }}>
                <Doughnut
                  data={transactionChartData}
                  options={{
                    onClick: (event, elements) => {
                      if (!elements.length) return;
                      const index = elements[0].index;
                      const selectedTransaction =
                        data.transactionStats[index].label;
                      handleTransactionClick(selectedTransaction);
                    },
                    plugins: {
                      legend: {
                        position: "top",
                      },
                    },
                  }}
                />
              </div>
            </Card.Body>
          </Card>
        </Col>
      </Row>

      <Row className="g-4">
        <Col md={8}>
          <Card className="shadow-sm">
            <Card.Body>
              <Card.Title>Recent KYC Uploads</Card.Title>
              <Table hover responsive>
                <thead>
                  <tr>
                    <th>KYC ID</th>
                    <th>Customer Name</th>
                    <th>PPS Number</th>
                    <th>County</th>
                    <th>Status</th>
                    <th>Created Date</th>
                  </tr>
                </thead>
                <tbody>
                  {data.recentUploads.map((record) => (
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
                      <td>{new Date(record.createdDate).toLocaleDateString()}</td>
                    </tr>
                  ))}
                </tbody>
              </Table>
            </Card.Body>
          </Card>
        </Col>

        <Col md={4}>
          <Card className="shadow-sm">
            <Card.Body>
              <Card.Title>Alerts</Card.Title>
              <ListGroup variant="flush">
                {data.alerts && data.alerts.length > 0 ? (
                  data.alerts.map((alert, index) => (
                    <ListGroup.Item key={index}>{alert}</ListGroup.Item>
                  ))
                ) : (
                  <ListGroup.Item>No alerts available</ListGroup.Item>
                )}
              </ListGroup>
            </Card.Body>
          </Card>
        </Col>
      </Row>
    </Container>
  );
}