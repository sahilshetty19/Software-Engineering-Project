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

const summaryCardThemes = [
  {
    key: "total",
    title: "Total Records",
    color: "#0f4c81",
    tint: "linear-gradient(135deg, #e8f3ff 0%, #d8ebff 100%)",
    shadow: "0 18px 34px rgba(15, 76, 129, 0.14)",
  },
  {
    key: "pending",
    title: "Pending",
    color: "#b26a00",
    tint: "linear-gradient(135deg, #fff5dd 0%, #ffe8b2 100%)",
    shadow: "0 18px 34px rgba(178, 106, 0, 0.16)",
  },
  {
    key: "processing",
    title: "Processing",
    color: "#005f73",
    tint: "linear-gradient(135deg, #ddf7fb 0%, #c5ecf5 100%)",
    shadow: "0 18px 34px rgba(0, 95, 115, 0.16)",
  },
  {
    key: "completed",
    title: "Completed",
    color: "#166534",
    tint: "linear-gradient(135deg, #e4f8eb 0%, #c9efd7 100%)",
    shadow: "0 18px 34px rgba(22, 101, 52, 0.16)",
  },
  {
    key: "failed",
    title: "Failed",
    color: "#9f1239",
    tint: "linear-gradient(135deg, #ffe3ea 0%, #ffcfdc 100%)",
    shadow: "0 18px 34px rgba(159, 18, 57, 0.16)",
  },
];

const chartPalette = {
  status: ["#f59e0b", "#0ea5e9", "#22c55e", "#ef4444"],
  county: ["#0f4c81", "#0ea5e9", "#14b8a6", "#22c55e", "#f59e0b", "#ef4444"],
  transaction: ["#166534", "#b91c1c", "#b26a00"],
};

const sectionCardStyle = {
  border: "1px solid rgba(15, 23, 42, 0.08)",
  borderRadius: "20px",
  boxShadow: "0 22px 42px rgba(15, 23, 42, 0.08)",
};

function SummaryCard({ title, count, color, tint, shadow, onClick }) {
  return (
    <Card
      className="h-100 border-0"
      onClick={onClick}
      style={{
        cursor: "pointer",
        background: tint,
        borderRadius: "20px",
        boxShadow: shadow,
      }}
    >
      <Card.Body>
        <div
          style={{
            width: "56px",
            height: "6px",
            borderRadius: "999px",
            backgroundColor: color,
            marginBottom: "18px",
          }}
        />
        <div
          style={{
            fontSize: "0.82rem",
            fontWeight: 700,
            letterSpacing: "0.04em",
            textTransform: "uppercase",
            color,
            marginBottom: "10px",
          }}
        >
          {title}
        </div>
        <h3 className="mb-0" style={{ color: "#102a43", fontWeight: 700 }}>
          {count}
        </h3>
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

  const summaryCards = summaryCardThemes.map((theme) => ({
    ...theme,
    count: data.summary[theme.key] ?? 0,
    onClick:
      theme.key === "total"
        ? () => navigate("/kyc-records")
        : () =>
            navigate(`/kyc-records?status=${theme.title === "Failed" ? "Failed" : theme.title}`),
  }));

  const statusChartData = {
    labels: data.statusDistribution.map((item) => item.label),
    datasets: [
      {
        data: data.statusDistribution.map((item) => item.count),
        backgroundColor: chartPalette.status,
        borderColor: "#ffffff",
        borderWidth: 3,
        hoverOffset: 10,
      },
    ],
  };

  const uploadTrendData = {
    labels: data.uploadTrend.map((item) => item.label),
    datasets: [
      {
        label: "Uploads",
        data: data.uploadTrend.map((item) => item.count),
        borderColor: "#0f4c81",
        backgroundColor: "rgba(15, 76, 129, 0.16)",
        pointBackgroundColor: "#0f4c81",
        pointBorderColor: "#ffffff",
        pointRadius: 4,
        pointHoverRadius: 6,
        tension: 0.35,
        fill: true,
      },
    ],
  };

  const countyChartData = {
    labels: data.countyDistribution.map((item) => item.label),
    datasets: [
      {
        data: data.countyDistribution.map((item) => item.count),
        backgroundColor: data.countyDistribution.map(
          (_, index) => chartPalette.county[index % chartPalette.county.length]
        ),
        borderRadius: 12,
        borderSkipped: false,
        maxBarThickness: 56,
        hoverBorderColor: "#ffffff",
        hoverBorderWidth: 2,
      },
    ],
  };

  const transactionChartData = {
    labels: data.transactionStats.map((item) => item.label),
    datasets: [
      {
        data: data.transactionStats.map((item) => item.count),
        backgroundColor: chartPalette.transaction,
        borderColor: "#ffffff",
        borderWidth: 3,
        hoverOffset: 10,
      },
    ],
  };

  const handleTransactionClick = (label) => {
    navigate(`/kyc-records?transactionStatus=${label}`);
  };

  return (
    <Container
      fluid
      className="p-4"
      style={{
        minHeight: "100vh",
        background:
          "radial-gradient(circle at top left, rgba(224, 242, 254, 0.8), transparent 32%), radial-gradient(circle at top right, rgba(220, 252, 231, 0.72), transparent 28%), linear-gradient(180deg, #f8fbff 0%, #eef4f8 100%)",
      }}
    >
      <div className="d-flex justify-content-between align-items-center mb-4 flex-wrap gap-3">
        <div>
          <h2 className="mb-1" style={{ color: "#102a43", fontWeight: 700 }}>
            Dashboard
          </h2>
          <div style={{ color: "#486581" }}>
            Live KYC pipeline overview with status, volume, county spread, and recent activity.
          </div>
        </div>
        <div className="d-flex gap-2">
          <Button variant="outline-secondary" onClick={() => navigate("/kyc-records")}>
            View KYC Records
          </Button>
          <Button onClick={() => navigate("/kyc/new")}>+ New KYC Upload</Button>
        </div>
      </div>

      <Row className="g-3 mb-4">
        {summaryCards.map((card) => (
          <Col md={6} xl={2} key={card.key}>
            <SummaryCard {...card} />
          </Col>
        ))}
      </Row>

      <Row className="g-4 mb-4">
        <Col md={6}>
          <Card className="border-0 h-100" style={sectionCardStyle}>
            <Card.Body>
              <Card.Title style={{ color: "#102a43", fontWeight: 700 }}>
                KYC Status Distribution
              </Card.Title>
              <div style={{ color: "#486581", marginBottom: "16px" }}>
                Clear view of where current records sit in the workflow.
              </div>
              <div style={{ maxWidth: "420px" }}>
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
          <Card className="border-0 h-100" style={sectionCardStyle}>
            <Card.Body>
              <Card.Title style={{ color: "#102a43", fontWeight: 700 }}>
                KYC Upload Trend
              </Card.Title>
              <div style={{ color: "#486581", marginBottom: "16px" }}>
                Upload momentum over time with emphasis on recent activity.
              </div>
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
                  scales: {
                    y: {
                      beginAtZero: true,
                      ticks: {
                        precision: 0,
                      },
                    },
                  },
                }}
              />
            </Card.Body>
          </Card>
        </Col>

        <Col md={6}>
          <Card className="border-0 h-100" style={sectionCardStyle}>
            <Card.Body>
              <Card.Title style={{ color: "#102a43", fontWeight: 700 }}>
                County-wise KYC Distribution
              </Card.Title>
              <div style={{ color: "#486581", marginBottom: "16px" }}>
                Regional mix of KYC intake across supported counties.
              </div>
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
                      display: false,
                    },
                    tooltip: {
                      backgroundColor: "#102a43",
                      titleColor: "#f8fafc",
                      bodyColor: "#f8fafc",
                      padding: 12,
                      displayColors: false,
                      callbacks: {
                        title: (items) => items[0]?.label ?? "County",
                        label: (context) => `Records: ${context.parsed.y}`,
                      },
                    },
                  },
                  scales: {
                    x: {
                      grid: {
                        display: false,
                      },
                      ticks: {
                        color: "#486581",
                        font: {
                          weight: 600,
                        },
                      },
                    },
                    y: {
                      beginAtZero: true,
                      grace: "8%",
                      grid: {
                        color: "rgba(72, 101, 129, 0.12)",
                      },
                      ticks: {
                        precision: 0,
                        color: "#486581",
                      },
                    },
                  },
                }}
              />
            </Card.Body>
          </Card>
        </Col>

        <Col md={6}>
          <Card className="border-0 h-100" style={sectionCardStyle}>
            <Card.Body>
              <Card.Title style={{ color: "#102a43", fontWeight: 700 }}>
                CKYC Transaction Status
              </Card.Title>
              <div style={{ color: "#486581", marginBottom: "16px" }}>
                Operational mix of successful, failed, and pending CKYC interactions.
              </div>
              <div style={{ maxWidth: "420px" }}>
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
          <Card className="border-0" style={sectionCardStyle}>
            <Card.Body>
              <Card.Title style={{ color: "#102a43", fontWeight: 700 }}>
                Recent KYC Uploads
              </Card.Title>
              <div style={{ color: "#486581", marginBottom: "12px" }}>
                Most recently created KYC records for quick inspection.
              </div>
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
                      <td>{record.requestRef || record.kycId}</td>
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
          <Card
            className="border-0 h-100"
            style={{
              ...sectionCardStyle,
              background: "linear-gradient(180deg, #fff6e5 0%, #fffdf7 100%)",
            }}
          >
            <Card.Body>
              <Card.Title style={{ color: "#8d4f00", fontWeight: 700 }}>
                Alerts
              </Card.Title>
              <div style={{ color: "#8d4f00", marginBottom: "12px" }}>
                Items that may need operational follow-up.
              </div>
              <ListGroup variant="flush">
                {data.alerts && data.alerts.length > 0 ? (
                  data.alerts.map((alert, index) => (
                    <ListGroup.Item
                      key={index}
                      style={{
                        background: "transparent",
                        borderColor: "rgba(141, 79, 0, 0.16)",
                        color: "#7c4a03",
                      }}
                    >
                      {alert}
                    </ListGroup.Item>
                  ))
                ) : (
                  <ListGroup.Item
                    style={{
                      background: "transparent",
                      borderColor: "rgba(141, 79, 0, 0.16)",
                      color: "#7c4a03",
                    }}
                  >
                    No alerts available
                  </ListGroup.Item>
                )}
              </ListGroup>
            </Card.Body>
          </Card>
        </Col>
      </Row>
    </Container>
  );
}
