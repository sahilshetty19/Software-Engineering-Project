import { BrowserRouter, Route, Routes } from 'react-router-dom';
import './App.css';
import NewKycUpload from './createkycrecord/createkyc';
import DetailsPage from './detailspage/detailspage';
function App() {
  return (
    <div className="App">
      <BrowserRouter>
      <Routes>
      <Route path="/kycupload" element={<NewKycUpload />} />
      <Route path="/details" element={<DetailsPage />} />
      </Routes>
      </BrowserRouter>
      
    </div>
  );
}

export default App;
