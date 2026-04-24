import { useEffect, useState } from 'react';
import { FiPlus, FiTrash2, FiEdit2, FiX } from 'react-icons/fi';
import { getRoutes, createRoute, updateRoute, deleteRoute, getCameras } from '../services/api';

export default function Routes() {
  const [routes, setRoutes] = useState([]);
  const [availableCameras, setAvailableCameras] = useState([]);
  const [loading, setLoading] = useState(true);
  const [showModal, setShowModal] = useState(false);
  const [editingRoute, setEditingRoute] = useState(null);
  const [form, setForm] = useState({ name: '', description: '' });
  const [cameraIds, setCameraIds] = useState(['']);

  const load = () => {
    Promise.all([getRoutes(), getCameras()])
      .then(([routesRes, camerasRes]) => {
        setRoutes(routesRes.data);
        setAvailableCameras(camerasRes.data.filter(c => c.isActive));
      })
      .catch(console.error)
      .finally(() => setLoading(false));
  };

  useEffect(() => { load(); }, []);

  const openCreateModal = () => {
    if (availableCameras.length === 0) {
      alert("No active cameras found! Please register a camera first in the Cameras page.");
      return;
    }
    setEditingRoute(null);
    setForm({ name: '', description: '' });
    setCameraIds(['']);
    setShowModal(true);
  };

  const openEditModal = (route) => {
    setEditingRoute(route);
    setForm({ name: route.name, description: route.description });
    const ordered = [...route.checkpoints].sort((a,b) => a.sequenceOrder - b.sequenceOrder);
    setCameraIds(ordered.map(cp => cp.cameraId));
    setShowModal(true);
  };

  const closeModal = () => {
    setShowModal(false);
    setEditingRoute(null);
  };

  const addCheckpoint = () => {
    setCameraIds([...cameraIds, '']);
  };

  const removeCheckpoint = (index) => {
    if (cameraIds.length <= 1) return;
    const updated = cameraIds.filter((_, i) => i !== index);
    setCameraIds(updated);
  };

  const updateCheckpoint = (index, value) => {
    const updated = [...cameraIds];
    updated[index] = value;
    setCameraIds(updated);
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    try {
      const validCameraIds = cameraIds
        .map(id => parseInt(id, 10))
        .filter(id => !isNaN(id));

      if (validCameraIds.length === 0) {
        alert("Please select at least one valid camera.");
        return;
      }

      const payload = { ...form, cameraIds: validCameraIds };

      if (editingRoute) {
        await updateRoute(editingRoute.id, payload);
      } else {
        await createRoute(payload);
      }
      closeModal();
      load();
    } catch (err) {
      alert(err.response?.data || 'Error saving route');
    }
  };

  const handleDelete = async (id) => {
    if (!confirm('Delete this route entirely?')) return;
    await deleteRoute(id);
    load();
  };

  if (loading) {
    return <div className="loading"><div className="spinner" /> Loading routes...</div>;
  }

  return (
    <div className="fade-in">
      <div className="page-header">
        <h1 className="page-title">Routes</h1>
        <p className="page-subtitle">Define routes by mapping physical cameras into a sequence</p>
      </div>

      <div className="page-actions">
        <span style={{ color: 'var(--text-muted)', fontSize: '14px' }}>
          {routes.length} route{routes.length !== 1 ? 's' : ''} defined
        </span>
        <button className="btn btn-primary" onClick={openCreateModal}>
          <FiPlus /> Create Route
        </button>
      </div>

      {routes.length === 0 ? (
        <div className="empty-state card">
          <div className="icon">🛣️</div>
          <p>No routes defined</p>
          <div className="sub">Create a route with checkpoints to start tracking</div>
        </div>
      ) : (
        <div style={{ display: 'grid', gap: '16px' }}>
          {routes.map((route) => (
            <div key={route.id} className="card">
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'start', marginBottom: '16px' }}>
                <div>
                  <h3 style={{ fontSize: '18px', fontWeight: 700, color: 'var(--text-primary)', marginBottom: '4px' }}>
                    {route.name}
                  </h3>
                  <p style={{ fontSize: '13px', color: 'var(--text-muted)' }}>{route.description}</p>
                </div>
                <div style={{ display: 'flex', gap: '8px', alignItems: 'center' }}>
                  <span className="badge active">{route.vehicleCount} vehicles</span>
                  <button className="btn btn-warning btn-sm" onClick={() => openEditModal(route)}>
                    <FiEdit2 /> Edit
                  </button>
                  <button className="btn btn-danger btn-sm" onClick={() => handleDelete(route.id)}>
                    <FiTrash2 />
                  </button>
                </div>
              </div>

              <div style={{ display: 'flex', gap: '8px', flexWrap: 'wrap' }}>
                {route.checkpoints.map((cp, i) => (
                  <div key={cp.id} style={{
                    display: 'flex', alignItems: 'center', gap: '8px',
                    padding: '8px 14px', background: 'var(--bg-primary)',
                    borderRadius: 'var(--radius-sm)', border: '1px solid var(--border)',
                    fontSize: '13px',
                  }}>
                    <span style={{
                      width: '22px', height: '22px', borderRadius: '50%',
                      background: 'linear-gradient(135deg, #16a34a, #22c55e)',
                      color: 'white', display: 'flex', alignItems: 'center', justifyContent: 'center',
                      fontSize: '11px', fontWeight: 700, flexShrink: 0,
                    }}>
                      {cp.sequenceOrder}
                    </span>
                    <span style={{ fontWeight: 600, color: 'var(--text-primary)' }}>{cp.name}</span>
                    <span style={{ color: 'var(--text-muted)', fontSize: '11px' }}>({cp.cameraIdentifier})</span>
                    {i < route.checkpoints.length - 1 && (
                      <span style={{ color: 'var(--accent)', marginLeft: '4px', fontWeight: 700 }}>→</span>
                    )}
                  </div>
                ))}
              </div>
            </div>
          ))}
        </div>
      )}

      {showModal && (
        <div className="modal-overlay" onClick={closeModal}>
          <div className="modal" onClick={(e) => e.stopPropagation()} style={{ maxWidth: '640px' }}>
            <h3 className="modal-title">{editingRoute ? 'Edit Route' : 'Create Route'}</h3>
            <form onSubmit={handleSubmit}>
              <div className="form-group">
                <label className="form-label">Route Name</label>
                <input className="form-input" placeholder="e.g. Downtown Line A" value={form.name}
                  onChange={(e) => setForm({ ...form, name: e.target.value })} required />
              </div>
              <div className="form-group">
                <label className="form-label">Description</label>
                <input className="form-input" placeholder="Optional description" value={form.description}
                  onChange={(e) => setForm({ ...form, description: e.target.value })} />
              </div>

              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '12px' }}>
                <label className="form-label" style={{ margin: 0 }}>Checkpoint Sequence</label>
                <button type="button" className="btn btn-ghost btn-sm" onClick={addCheckpoint}>
                  <FiPlus /> Add
                </button>
              </div>

              <div className="checkpoint-list" style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
                {cameraIds.map((camId, i) => (
                  <div key={i} style={{ display: 'flex', gap: '12px', alignItems: 'center' }}>
                    <div style={{
                        width: '28px', height: '28px', borderRadius: '50%', background: 'var(--bg-secondary)',
                        color: 'var(--text-secondary)', display: 'flex', alignItems: 'center', justifyContent: 'center', border: '1px solid var(--border)'
                    }}>
                        {i + 1}
                    </div>
                    <select
                        className="form-input"
                        value={camId}
                        onChange={(e) => updateCheckpoint(i, e.target.value)}
                        required
                        style={{ flex: 1 }}
                    >
                        <option value="" disabled>-- Select a camera --</option>
                        {availableCameras.map(c => (
                            <option key={c.id} value={c.id}>{c.name} ({c.cameraIdentifier})</option>
                        ))}
                    </select>
                    
                    {cameraIds.length > 1 && (
                      <button type="button" className="btn btn-danger btn-sm" onClick={() => removeCheckpoint(i)}
                        style={{ flexShrink: 0, padding: '8px' }}>
                        <FiX />
                      </button>
                    )}
                  </div>
                ))}
              </div>

              <div className="modal-actions" style={{ marginTop: '24px' }}>
                <button type="button" className="btn btn-ghost" onClick={closeModal}>Cancel</button>
                <button type="submit" className="btn btn-primary">
                  {editingRoute ? 'Save Changes' : 'Create Route'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
