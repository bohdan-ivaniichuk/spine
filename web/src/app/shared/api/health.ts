/** Shape of `GET /api/health` (nginx maps it to the API's `/health/ready`). */
export interface HealthResponse {
  status: 'Healthy' | 'Degraded' | 'Unhealthy';
}
