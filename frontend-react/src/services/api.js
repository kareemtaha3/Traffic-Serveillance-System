import axios from 'axios';

const API_BASE = 'http://localhost:5116/api';

const api = axios.create({
  baseURL: API_BASE,
});

export const getDashboardStats = () => api.get('/dashboard/stats');

export const getRoutes = () => api.get('/routes');
export const getRoute = (id) => api.get(`/routes/${id}`);
export const createRoute = (data) => api.post('/routes', data);
export const updateRoute = (id, data) => api.put(`/routes/${id}`, data);
export const deleteRoute = (id) => api.delete(`/routes/${id}`);

export const getVehicles = () => api.get('/vehicles');
export const getVehicle = (id) => api.get(`/vehicles/${id}`);
export const createVehicle = (data) => api.post('/vehicles', data);
export const assignRoute = (vehicleId, routeId) =>
  api.post(`/vehicles/${vehicleId}/assign-route`, { routeId });
export const getCheckpointLogs = (vehicleId) =>
  api.get(`/vehicles/${vehicleId}/checkpoint-logs`);
export const deleteVehicle = (id) => api.delete(`/vehicles/${id}`);

export const getCameras = () => api.get('/cameras');
export const getCamera = (id) => api.get(`/cameras/${id}`);
export const createCamera = (data) => api.post('/cameras', data);
export const updateCamera = (id, data) => api.put(`/cameras/${id}`, data);
export const deleteCamera = (id) => api.delete(`/cameras/${id}`);

export const getInfringements = (params) => api.get('/infringements', { params });
export const getInfringement = (id) => api.get(`/infringements/${id}`);

export default api;
