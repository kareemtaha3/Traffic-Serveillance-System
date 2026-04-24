import { useEffect, useState } from 'react';
import { FiTruck, FiMap, FiAlertTriangle, FiActivity, FiCheckCircle } from 'react-icons/fi';
import { getDashboardStats } from '../services/api';

export default function Dashboard() {
  const [stats, setStats] = useState(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    getDashboardStats()
      .then((res) => setStats(res.data))
      .catch(console.error)
      .finally(() => setLoading(false));
  }, []);

  if (loading) {
    return <div className="loading"><div className="spinner" /> Loading dashboard...</div>;
  }

  if (!stats) {
    return <div className="loading">Failed to load dashboard data</div>;
  }

  const formatTime = (dateStr) => {
    const d = new Date(dateStr);
    return d.toLocaleString();
  };

  return (
    <div className="fade-in">
      <div className="page-header">
        <h1 className="page-title">Dashboard</h1>
        <p className="page-subtitle">Traffic surveillance overview</p>
      </div>

      <div className="stats-grid">
        <div className="stat-card">
          <div className="stat-icon blue"><FiTruck /></div>
          <div className="stat-value">{stats.totalVehicles}</div>
          <div className="stat-label">Total Vehicles</div>
        </div>
        <div className="stat-card">
          <div className="stat-icon purple"><FiMap /></div>
          <div className="stat-value">{stats.totalRoutes}</div>
          <div className="stat-label">Total Routes</div>
        </div>
        <div className="stat-card">
          <div className="stat-icon green"><FiCheckCircle /></div>
          <div className="stat-value">{stats.activeVehicles}</div>
          <div className="stat-label">Active Vehicles</div>
        </div>
        <div className="stat-card">
          <div className="stat-icon yellow"><FiActivity /></div>
          <div className="stat-value">{stats.totalCheckpointLogs}</div>
          <div className="stat-label">Checkpoint Logs</div>
        </div>
        <div className="stat-card">
          <div className="stat-icon red"><FiAlertTriangle /></div>
          <div className="stat-value">{stats.totalInfringements}</div>
          <div className="stat-label">Infringements</div>
        </div>
      </div>

      <h2 style={{ fontSize: '18px', fontWeight: 700, marginBottom: '16px', color: 'var(--text-primary)' }}>
        Recent Activity
      </h2>

      {stats.recentLogs.length === 0 ? (
        <div className="empty-state">
          <div className="icon">📡</div>
          <p>No checkpoint activity yet</p>
          <div className="sub">Detections will appear here once the system processes vehicle checkpoints</div>
        </div>
      ) : (
        <div className="feed-list">
          {stats.recentLogs.map((log) => (
            <div key={log.id} className="feed-item">
              <div className={`feed-status ${log.isValid ? 'valid' : 'invalid'}`} />
              <div className="feed-info">
                <div className="feed-plate">{log.plateText}</div>
                <div className="feed-detail">
                  {log.checkpointName} • {log.routeName} • Camera: {log.cameraId}
                </div>
              </div>
              <span className={`badge ${log.isValid ? 'valid' : 'invalid'}`}>
                {log.isValid ? 'Valid' : 'Invalid'}
              </span>
              <div className="feed-time">{formatTime(log.detectedAt)}</div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
