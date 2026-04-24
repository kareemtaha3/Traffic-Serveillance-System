import { useEffect, useState } from 'react';
import { FiRefreshCw } from 'react-icons/fi';
import { getDashboardStats } from '../services/api';

export default function Monitoring() {
  const [logs, setLogs] = useState([]);
  const [loading, setLoading] = useState(true);

  const load = () => {
    setLoading(true);
    getDashboardStats()
      .then((res) => setLogs(res.data.recentLogs || []))
      .catch(console.error)
      .finally(() => setLoading(false));
  };

  useEffect(() => { load(); }, []);

  const formatTime = (dateStr) => {
    const d = new Date(dateStr);
    return d.toLocaleString();
  };

  return (
    <div className="fade-in">
      <div className="page-header">
        <h1 className="page-title">Monitoring</h1>
        <p className="page-subtitle">Real-time checkpoint detection feed</p>
      </div>

      <div className="page-actions">
        <span style={{ color: 'var(--text-muted)', fontSize: '14px' }}>
          Showing latest {logs.length} detections
        </span>
        <button className="btn btn-ghost" onClick={load} disabled={loading}>
          <FiRefreshCw style={{ animation: loading ? 'spin 1s linear infinite' : 'none' }} /> Refresh
        </button>
      </div>

      {logs.length === 0 ? (
        <div className="empty-state card">
          <div className="icon">📡</div>
          <p>No checkpoint detections yet</p>
          <div className="sub">Detections will appear here when vehicles pass checkpoint cameras</div>
        </div>
      ) : (
        <div className="feed-list">
          {logs.map((log) => (
            <div key={log.id} className="feed-item">
              <div className={`feed-status ${log.isValid ? 'valid' : 'invalid'}`} />
              <div className="feed-info">
                <div className="feed-plate">{log.plateText}</div>
                <div className="feed-detail">
                  📍 {log.checkpointName} • 🛣️ {log.routeName} • 📷 {log.cameraId}
                </div>
              </div>
              <span className={`badge ${log.isValid ? 'valid' : 'invalid'}`}>
                {log.isValid ? '✓ Valid' : '✕ Invalid'}
              </span>
              <div className="feed-time">{formatTime(log.detectedAt)}</div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
