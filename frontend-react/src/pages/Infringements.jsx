import { useEffect, useState } from 'react';
import { FiSearch } from 'react-icons/fi';
import { getInfringements } from '../services/api';

export default function Infringements() {
  const [infringements, setInfringements] = useState([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState('');

  const load = (plate) => {
    setLoading(true);
    getInfringements(plate ? { plate } : {})
      .then((res) => setInfringements(res.data))
      .catch(console.error)
      .finally(() => setLoading(false));
  };

  useEffect(() => { load(); }, []);

  const handleSearch = (e) => {
    e.preventDefault();
    load(search);
  };

  const formatTime = (dateStr) => {
    const d = new Date(dateStr);
    return d.toLocaleString();
  };

  return (
    <div className="fade-in">
      <div className="page-header">
        <h1 className="page-title">Infringements</h1>
        <p className="page-subtitle">Route deviation violations</p>
      </div>

      <div className="page-actions">
        <form onSubmit={handleSearch} style={{ display: 'flex', gap: '8px' }}>
          <div style={{ position: 'relative' }}>
            <FiSearch style={{
              position: 'absolute', left: '12px', top: '50%', transform: 'translateY(-50%)',
              color: 'var(--text-muted)', fontSize: '14px',
            }} />
            <input className="form-input" placeholder="Search by plate..." value={search}
              onChange={(e) => setSearch(e.target.value)}
              style={{ paddingLeft: '36px', width: '260px', margin: 0 }} />
          </div>
          <button type="submit" className="btn btn-ghost">Search</button>
        </form>
        <span style={{ color: 'var(--text-muted)', fontSize: '14px' }}>
          {infringements.length} infringement{infringements.length !== 1 ? 's' : ''}
        </span>
      </div>

      {loading ? (
        <div className="loading"><div className="spinner" /> Loading infringements...</div>
      ) : infringements.length === 0 ? (
        <div className="empty-state card">
          <div className="icon">🛡️</div>
          <p>No infringements found</p>
          <div className="sub">All vehicles are following their assigned routes</div>
        </div>
      ) : (
        <div className="table-container">
          <table>
            <thead>
              <tr>
                <th>ID</th>
                <th>Plate</th>
                <th>Owner</th>
                <th>Route</th>
                <th>Expected</th>
                <th>Actual</th>
                <th>Description</th>
                <th>Date</th>
              </tr>
            </thead>
            <tbody>
              {infringements.map((inf) => (
                <tr key={inf.id}>
                  <td>{inf.id}</td>
                  <td style={{ fontWeight: 700, color: 'var(--text-primary)' }}>{inf.plateText}</td>
                  <td>{inf.carOwner}</td>
                  <td><span className="badge active">{inf.routeName}</span></td>
                  <td style={{ color: 'var(--success)' }}>{inf.expectedCheckpoint}</td>
                  <td style={{ color: 'var(--danger)' }}>{inf.actualCheckpoint}</td>
                  <td style={{ maxWidth: '260px', fontSize: '12px', color: 'var(--text-muted)' }}>
                    {inf.description}
                  </td>
                  <td style={{ whiteSpace: 'nowrap' }}>{formatTime(inf.detectedAt)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
