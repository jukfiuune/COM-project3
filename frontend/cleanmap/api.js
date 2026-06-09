/**
 * Connection and API utilities for Nexora
 */
import { fetchJson } from './utils.js';
import { getAuthHeaders, saveSession, clearSession, redirectToLogin } from './auth.js';

function getCsrfToken() {
  const match = document.cookie.match(new RegExp('(^| )csrfToken=([^;]+)'));
  return match ? match[2] : '';
}

let apiBase = '';
let apiEnabled = true;

/**
 * Get current API Base URL.
 * @returns {string}
 */
export function getApiBase() {
  return apiBase;
}

/**
 * Check if the API connection is active.
 * @returns {boolean}
 */
export function isApiEnabled() {
  return apiEnabled;
}

/**
 * Manually override api connection state.
 * @param {boolean} val
 */
export function setApiEnabled(val) {
  apiEnabled = val;
}

/**
 * Scans candidate endpoints to find one that responds.
 * @returns {Promise<string|null>} Active URL or null.
 */
export async function detectApiBase() {
  try {
    await fetchJson(`/api/cleanmap/ping`, {}, 6000);
    apiEnabled = true;
    return '';
  } catch {
    apiEnabled = false;
    return null;
  }
}

/**
 * Authenticated wrapper for fetchJson.
 * @param {string} url - Target URL.
 * @param {RequestInit} [options={}] - Options.
 * @param {number} [timeoutMs=30000] - Timeout.
 */
export async function authFetch(url, options = {}, timeoutMs = 30000) {
  const method = (options.method || 'GET').toUpperCase();
  const headers = {
    'Content-Type': 'application/json',
    ...getAuthHeaders(),
    ...(options.headers || {})
  };

  if (['POST', 'PUT', 'DELETE'].includes(method)) {
    headers['X-CSRF-Token'] = getCsrfToken();
  }

  try {
    return await fetchJson(url, { ...options, headers }, timeoutMs);
  } catch (err) {
    if (err.message && err.message.includes('401')) {
      // try to refresh
      try {
        const res = await fetch(`${getApiBase()}/api/auth/refresh`, { 
          method: 'POST', 
          credentials: 'include',
          headers: { 'X-CSRF-Token': getCsrfToken() }
        });
        if (res.ok) {
          const data = await res.json();
          saveSession(data);
          // Retry original request
          headers.Authorization = getAuthHeaders().Authorization;
          return await fetchJson(url, { ...options, headers }, timeoutMs);
        }
      } catch (refreshErr) {
        // Refresh failed
      }
      clearSession();
      redirectToLogin(window.location.pathname);
    }
    throw err;
  }
}

/**
 * Create a cleanmap pollution report.
 * @param {object} report - Report data.
 */
export async function createReportApi(report) {
  return authFetch(`${getApiBase()}/api/cleanmap/reports`, {
    method: 'POST',
    body: JSON.stringify(report)
  }, 30000);
}

/**
 * Mark a pollution report as cleaned.
 * @param {string} reportId - Target ID.
 * @param {object} payload - Proof photo body.
 */
export async function markCleanApi(reportId, payload) {
  return authFetch(`${getApiBase()}/api/cleanmap/reports/${encodeURIComponent(reportId)}/clean`, {
    method: 'POST',
    body: JSON.stringify(payload)
  }, 30000);
}

export async function logoutApi() {
  try {
    await authFetch(`${getApiBase()}/api/auth/logout`, { method: 'POST' });
  } catch (e) {}
  clearSession();
}
