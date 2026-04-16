import { BrowserRouter, Routes, Route } from "react-router-dom";
import Dashboard from "./pages/Dashboard/Dashboard";
import KycRecords from "./pages/KycRecords/KycRecords";
import NewKycUpload from "./pages/NewKycUpload/NewKycUpload";
import KycDetails from "./pages/KycDetails/KycDetails";

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<Dashboard />} />
        <Route path="/kyc-records" element={<KycRecords />} />
        <Route path="/kyc/new" element={<NewKycUpload />} />
        <Route path="/kyc/:id" element={<KycDetails />} />
      </Routes>
    </BrowserRouter>
  );
}

export default App;