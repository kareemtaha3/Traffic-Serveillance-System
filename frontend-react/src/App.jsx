import { BrowserRouter, Routes, Route } from 'react-router-dom';
import Layout from './components/Layout';
import Dashboard from './pages/Dashboard';
import Cameras from './pages/Cameras';
import Vehicles from './pages/Vehicles';
import RoutesPage from './pages/Routes';
import Monitoring from './pages/Monitoring';
import Infringements from './pages/Infringements';

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route element={<Layout />}>
          <Route path="/" element={<Dashboard />} />
          <Route path="/cameras" element={<Cameras />} />
          <Route path="/vehicles" element={<Vehicles />} />
          <Route path="/routes" element={<RoutesPage />} />
          <Route path="/monitoring" element={<Monitoring />} />
          <Route path="/infringements" element={<Infringements />} />
        </Route>
      </Routes>
    </BrowserRouter>
  );
}

export default App;
