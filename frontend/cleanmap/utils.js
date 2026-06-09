/**
 * Shared utility functions for Nexora frontend
 */

/**
 * Display a temporary toast notification on the page.
 * @param {string} message - The text to display.
 * @param {boolean} [isError=false] - Whether to style the toast as an error.
 */
export function showToast(message, isError = false) {
  const toast = document.getElementById('toast');
  if (!toast) return;
  toast.textContent = message;
  toast.className = 'toast show' + (isError ? ' error' : '');
  clearTimeout(toast._timer);
  toast._timer = setTimeout(() => toast.classList.remove('show'), 3000);
}

/**
 * Fetch helper that handles timeouts and JSON responses/errors.
 * @param {string} url - The URL to request.
 * @param {RequestInit} [options={}] - Fetch configuration.
 * @param {number} [timeoutMs=8000] - Timeout in milliseconds.
 * @returns {Promise<any>} Response body (parsed JSON or null).
 */
export async function fetchJson(url, options = {}, timeoutMs = 8000) {
  const controller = new AbortController();
  const timeoutId = setTimeout(() => controller.abort(), timeoutMs);
  try {
    const response = await fetch(url, { ...options, signal: controller.signal });
    
    let body = null;
    const contentType = response.headers.get('content-type') || '';
    if (contentType.includes('application/json')) {
      body = await response.json().catch(() => null);
    }
    
    if (!response.ok) {
      throw new Error(body?.error || response.statusText || `HTTP ${response.status}`);
    }
    return body;
  } finally {
    clearTimeout(timeoutId);
  }
}

/**
 * Safely escape strings for insertion into HTML.
 * @param {any} str - The value to escape.
 * @returns {string} Safe HTML string.
 */
export function esc(str) {
  if (str === null || str === undefined) return '';
  const d = document.createElement('div');
  d.textContent = String(str);
  return d.innerHTML;
}

/**
 * Format timestamp to a localized string.
 * @param {number|string} timestamp - Timestamp or ISO date string.
 * @returns {string} Localized date-time string.
 */
export function formatDate(timestamp) {
  if (!timestamp) return '';
  return new Date(timestamp).toLocaleString();
}

/**
 * Compute distance between two coordinates in meters.
 * @param {number} lat1 - Latitude 1.
 * @param {number} lng1 - Longitude 1.
 * @param {number} lat2 - Latitude 2.
 * @param {number} lng2 - Longitude 2.
 * @returns {number} Distance in meters.
 */
export function haversineDistance(lat1, lng1, lat2, lng2) {
  const R = 6371000;
  const dLat = (lat2 - lat1) * Math.PI / 180;
  const dLng = (lng2 - lng1) * Math.PI / 180;
  const a = Math.sin(dLat / 2) ** 2 +
    Math.cos(lat1 * Math.PI / 180) * Math.cos(lat2 * Math.PI / 180) * Math.sin(dLng / 2) ** 2;
  return R * 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
}
