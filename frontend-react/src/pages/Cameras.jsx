import { useEffect, useState } from 'react';
import { FiPlus, FiTrash2, FiEdit2, FiActivity } from 'react-icons/fi';
import { getCameras, createCamera, updateCamera, deleteCamera } from '../services/api';

export default function Cameras() {
  const [cameras, setCameras] = useState([]);
  const [loading, setLoading] = useState(true);
  const [showModal, setShowModal] = useState(false);
  const [editingCamera, setEditingCamera] = useState(null);
  const [form, setForm] = useState({
    cameraIdentifier: '',
    name: '',
    description: '',
    latitude: 0,
    longitude: 0,
    isActive: true
  });

  const load = () => {
    getCameras()
      .then((res) => setCameras(res.data))
      .catch(console.error)
      .finally(() => setLoading(false));
  };

  useEffect(() => { load(); }, []);

  const openCreateModal = () => {
    setEditingCamera(null);
    setForm({ cameraIdentifier: '', name: '', description: '', latitude: 0, longitude: 0, isActive: true });
    setShowModal(true);
  };

  const openEditModal = (camera) => {
    setEditingCamera(camera);
    setForm({
      cameraIdentifier: camera.cameraIdentifier,
      name: camera.name,
      description: camera.description || '',
      latitude: camera.latitude,
      longitude: camera.longitude,
      isActive: camera.isActive
    });
    setShowModal(true);
  };

  const closeModal = () => {
    setShowModal(false);
    setEditingCamera(null);
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    try {
      if (editingCamera) {
        await updateCamera(editingCamera.id, { id: editingCamera.id, ...form });
      } else {
        await createCamera(form);
      }
      closeModal();
      load();
    } catch (err) {
      alert(err.response?.data || 'Error saving camera');
    }
  };

  const handleDelete = async (id) => {
    if (!confirm('Are you sure you want to delete this camera? It will be removed from all routes.')) return;
    try {
      await deleteCamera(id);
      load();
    } catch (err) {
      alert(err.response?.data || 'Error deleting camera');
    }
  };

  if (loading) {
    return <div className="loading"><div className="spinner" /> Loading cameras...</div>;
  }

  return (
    <div className="fade-in">
      <div className="page-header">
        <h1 className="page-title">Physical Cameras</h1>
        <p className="page-subtitle">Manage deployed hardware checkpoints and IP cameras</p>
      </div>

      <div className="page-actions">
        <span style={{ color: 'var(--text-muted)', fontSize: '14px' }}>
          {cameras.length} camera{cameras.length !== 1 ? 's' : ''} deployed
        </span>
        <button className="btn btn-primary" onClick={openCreateModal}>
          <FiPlus /> Register Camera
        </button>
      </div>

      <div style={{ display: 'grid', gap: '16px', gridTemplateColumns: 'repeat(auto-fill, minmax(320px, 1fr))' }}>
        {cameras.length === 0 ? (
          <div className="empty-state card" style={{ gridColumn: '1 / -1' }}>
            <div className="icon">📹</div>
            <p>No cameras registered</p>
            <div className="sub">Register your first physical camera to start building routes</div>
          </div>
        ) : (
          cameras.map((camera) => (
            <div key={camera.id} className="card" style={{ display: 'flex', flexDirection: 'column' }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'start', marginBottom: '16px' }}>
                <div>
                  <h3 style={{ fontSize: '16px', fontWeight: 700, color: 'var(--text-primary)', marginBottom: '4px' }}>
                    {camera.name}
                  </h3>
                  <div style={{ fontSize: '13px', color: 'var(--text-muted)', fontFamily: 'monospace' }}>
                    ID: {camera.cameraIdentifier}
                  </div>
                </div>
                <span className={`badge ${camera.isActive ? 'active' : 'inactive'}`}>
                  {camera.isActive ? 'Operational' : 'Offline'}
                </span>
              </div>
              
              <p style={{ fontSize: '13px', color: 'var(--text-muted)', flex: 1, marginBottom: '16px' }}>
                {camera.description || 'No description provided.'}
              </p>

              <div style={{ borderTop: '1px solid var(--border)', paddingTop: '16px', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                <div style={{ fontSize: '12px', color: 'var(--text-muted)' }}>
                  Loc: {camera.latitude.toFixed(4)}, {camera.longitude.toFixed(4)}
                </div>
                <div style={{ display: 'flex', gap: '8px' }}>
                  <button className="btn btn-warning btn-sm" onClick={() => openEditModal(camera)}>
                    <FiEdit2 />
                  </button>
                  <button className="btn btn-danger btn-sm" onClick={() => handleDelete(camera.id)}>
                    <FiTrash2 />
                  </button>
                </div>
              </div>
            </div>
          ))
        )}
      </div>

      {showModal && (
        <div className="modal-overlay" onClick={closeModal}>
          <div className="modal" onClick={(e) => e.stopPropagation()}>
            <h3 className="modal-title">{editingCamera ? 'Edit Camera' : 'Register Camera'}</h3>
            <form onSubmit={handleSubmit}>
              <div className="form-group">
                <label className="form-label">Camera Public Identifier</label>
                <input className="form-input" placeholder="e.g. cam-north-1" value={form.cameraIdentifier}
                  onChange={(e) => setForm({ ...form, cameraIdentifier: e.target.value })} required />
              </div>
              <div className="form-group">
                <label className="form-label">Friendly Name</label>
                <input className="form-input" placeholder="e.g. North Gate Entrance" value={form.name}
                  onChange={(e) => setForm({ ...form, name: e.target.value })} required />
              </div>
              <div className="form-group">
                <label className="form-label">Description</label>
                <input className="form-input" placeholder="Location details or hardware specs" value={form.description}
                  onChange={(e) => setForm({ ...form, description: e.target.value })} />
              </div>
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px' }}>
                <div className="form-group">
                  <label className="form-label">Latitude</label>
                  <input type="number" step="any" className="form-input" value={form.latitude}
                    onChange={(e) => setForm({ ...form, latitude: parseFloat(e.target.value) || 0 })} required />
                </div>
                <div className="form-group">
                  <label className="form-label">Longitude</label>
                  <input type="number" step="any" className="form-input" value={form.longitude}
                    onChange={(e) => setForm({ ...form, longitude: parseFloat(e.target.value) || 0 })} required />
                </div>
              </div>
              <div className="form-group" style={{ display: 'flex', alignItems: 'center', gap: '8px', marginTop: '8px' }}>
                <input type="checkbox" id="isActive" checked={form.isActive}
                  onChange={(e) => setForm({ ...form, isActive: e.target.checked })} />
                <label htmlFor="isActive" style={{ margin: 0, fontWeight: 500, fontSize: '14px', cursor: 'pointer' }}>Camera is Active and Operational</label>
              </div>

              <div className="modal-actions" style={{ marginTop: '24px' }}>
                <button type="button" className="btn btn-ghost" onClick={closeModal}>Cancel</button>
                <button type="submit" className="btn btn-primary">
                  {editingCamera ? 'Save Changes' : 'Register Camera'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
