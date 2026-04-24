import { useEffect, useState } from 'react';
import { FiPlus, FiTrash2, FiMapPin } from 'react-icons/fi';
import { getVehicles, createVehicle, deleteVehicle, assignRoute, getRoutes } from '../services/api';

export default function Vehicles() {
  const [vehicles, setVehicles] = useState([]);
  const [routes, setRoutes] = useState([]);
  const [loading, setLoading] = useState(true);
  const [showModal, setShowModal] = useState(false);
  const [showAssignModal, setShowAssignModal] = useState(null);
  const [form, setForm] = useState({ licensePlate: '', ownerName: '', licenseExpiration: '' });
  const [selectedRoute, setSelectedRoute] = useState('');

  const load = () => {
    Promise.all([getVehicles(), getRoutes()])
      .then(([vRes, rRes]) => {
        setVehicles(vRes.data);
        setRoutes(rRes.data);
      })
      .catch(console.error)
      .finally(() => setLoading(false));
  };

  useEffect(() => { load(); }, []);

  const handleCreate = async (e) => {
    e.preventDefault();
    try {
      await createVehicle({
        licensePlate: form.licensePlate,
        ownerName: form.ownerName,
        licenseExpiration: form.licenseExpiration || new Date().toISOString(),
      });
      setShowModal(false);
      setForm({ licensePlate: '', ownerName: '', licenseExpiration: '' });
      load();
    } catch (err) {
      alert(err.response?.data || 'Error creating vehicle');
    }
  };

  const handleDelete = async (id) => {
    if (!confirm('Delete this vehicle?')) return;
    await deleteVehicle(id);
    load();
  };

  const openAssignModal = (vehicle) => {
    setShowAssignModal(vehicle.id);
    setSelectedRoute(vehicle.activeRouteId ? String(vehicle.activeRouteId) : '');
  };

  const handleAssign = async () => {
    if (!selectedRoute) return;
    try {
      await assignRoute(showAssignModal, parseInt(selectedRoute));
      setShowAssignModal(null);
      setSelectedRoute('');
      load();
    } catch (err) {
      alert(err.response?.data || 'Error assigning route');
    }
  };

  if (loading) {
    return <div className="loading"><div className="spinner" /> Loading vehicles...</div>;
  }

  return (
    <div className="fade-in">
      <div className="page-header">
        <h1 className="page-title">Vehicles</h1>
        <p className="page-subtitle">Manage registered vehicles and route assignments</p>
      </div>

      <div className="page-actions">
        <span style={{ color: 'var(--text-muted)', fontSize: '14px' }}>
          {vehicles.length} vehicle{vehicles.length !== 1 ? 's' : ''} registered
        </span>
        <button className="btn btn-primary" onClick={() => setShowModal(true)}>
          <FiPlus /> Add Vehicle
        </button>
      </div>

      {vehicles.length === 0 ? (
        <div className="empty-state card">
          <div className="icon">🚗</div>
          <p>No vehicles registered</p>
          <div className="sub">Add a vehicle to get started</div>
        </div>
      ) : (
        <div className="table-container">
          <table>
            <thead>
              <tr>
                <th>ID</th>
                <th>License Plate</th>
                <th>Owner</th>
                <th>License Expiry</th>
                <th>Active Route</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {vehicles.map((v) => (
                <tr key={v.id}>
                  <td>{v.id}</td>
                  <td style={{ fontWeight: 700, color: 'var(--text-primary)' }}>{v.licensePlate}</td>
                  <td>{v.ownerName}</td>
                  <td>{new Date(v.licenseExpiration).toLocaleDateString()}</td>
                  <td>
                    {v.activeRouteName ? (
                      <span className="badge active">{v.activeRouteName}</span>
                    ) : (
                      <span style={{ color: 'var(--text-muted)' }}>— Not assigned</span>
                    )}
                  </td>
                  <td style={{ display: 'flex', gap: '8px' }}>
                    <button className="btn btn-ghost btn-sm" onClick={() => openAssignModal(v)}>
                      <FiMapPin /> {v.activeRouteName ? 'Change Route' : 'Assign Route'}
                    </button>
                    <button className="btn btn-danger btn-sm" onClick={() => handleDelete(v.id)}>
                      <FiTrash2 />
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {/* Create Vehicle Modal */}
      {showModal && (
        <div className="modal-overlay" onClick={() => setShowModal(false)}>
          <div className="modal" onClick={(e) => e.stopPropagation()}>
            <h3 className="modal-title">Register Vehicle</h3>
            <form onSubmit={handleCreate}>
              <div className="form-group">
                <label className="form-label">License Plate</label>
                <input className="form-input" placeholder="e.g. أ ب ج 123" value={form.licensePlate}
                  onChange={(e) => setForm({ ...form, licensePlate: e.target.value })} required />
              </div>
              <div className="form-group">
                <label className="form-label">Owner Name</label>
                <input className="form-input" placeholder="Vehicle owner" value={form.ownerName}
                  onChange={(e) => setForm({ ...form, ownerName: e.target.value })} required />
              </div>
              <div className="form-group">
                <label className="form-label">License Expiration</label>
                <input className="form-input" type="date" value={form.licenseExpiration}
                  onChange={(e) => setForm({ ...form, licenseExpiration: e.target.value })} required />
              </div>
              <div className="modal-actions">
                <button type="button" className="btn btn-ghost" onClick={() => setShowModal(false)}>Cancel</button>
                <button type="submit" className="btn btn-primary">Register</button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Assign Route Modal */}
      {showAssignModal && (
        <div className="modal-overlay" onClick={() => setShowAssignModal(null)}>
          <div className="modal" onClick={(e) => e.stopPropagation()}>
            <h3 className="modal-title">Assign Route to Vehicle</h3>
            {routes.length === 0 ? (
              <div style={{ padding: '20px', textAlign: 'center', color: 'var(--text-muted)' }}>
                <p>No routes available. Create a route first.</p>
              </div>
            ) : (
              <>
                <div className="form-group">
                  <label className="form-label">Select Route</label>
                  <select className="form-input" value={selectedRoute} onChange={(e) => setSelectedRoute(e.target.value)}>
                    <option value="">-- Select a route --</option>
                    {routes.map((r) => (
                      <option key={r.id} value={r.id}>
                        {r.name} ({r.checkpoints.length} checkpoints)
                      </option>
                    ))}
                  </select>
                </div>
                {selectedRoute && (
                  <div style={{
                    padding: '12px 16px', background: 'var(--bg-primary)',
                    borderRadius: 'var(--radius-sm)', border: '1px solid var(--border)',
                    marginBottom: '8px',
                  }}>
                    <div style={{ fontSize: '12px', color: 'var(--text-muted)', marginBottom: '8px', fontWeight: 600 }}>
                      ROUTE CHECKPOINTS
                    </div>
                    <div style={{ display: 'flex', gap: '6px', flexWrap: 'wrap' }}>
                      {routes.find(r => r.id === parseInt(selectedRoute))?.checkpoints.map((cp, i, arr) => (
                        <span key={cp.id} style={{ fontSize: '13px', color: 'var(--text-secondary)' }}>
                          <strong>{cp.name}</strong>
                          {i < arr.length - 1 && <span style={{ color: 'var(--accent)', margin: '0 4px' }}>→</span>}
                        </span>
                      ))}
                    </div>
                  </div>
                )}
              </>
            )}
            <div className="modal-actions">
              <button className="btn btn-ghost" onClick={() => setShowAssignModal(null)}>Cancel</button>
              <button className="btn btn-primary" onClick={handleAssign} disabled={!selectedRoute || routes.length === 0}>
                Assign Route
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
