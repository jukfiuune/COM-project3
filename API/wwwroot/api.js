/**
 * Connection and API utilities for Nexora
 */
import { fetchJson } from './utils.js';
import { getAuthHeaders, saveSession, clearSession, redirectToLogin } from './auth.js';

function getCsrfToken() {
  const match = document.cookie.match(new RegExp('(^| )csrfToken=([^;]+)'));
  return match ? match[2] : '';
}

export const API_BASE_DEFAULT = 'https://com-project3.onrender.com';
export const IS_LOCAL = location.hostname === 'localhost' || location.hostname === '127.0.0.1';

let apiBase = localStorage.getItem('cm_api_base') || '';
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
 * @returns {Promise<boolean>} Success status.
 */
export async function detectApiBase() {
  // 1. Try relative path first (preferred, for unified backend serving)
  try {
    await fetchJson(`/api/cleanmap/ping`, {}, 3000);
    apiBase = '';
    localStorage.setItem('cm_api_base', '');
    apiEnabled = true;
    return true;
  } catch (err) {
    // Relative path failed, fallback to candidate URL scanning
  }

  // 2. Try candidate URLs
  const candidates = [
    localStorage.getItem('cm_api_base'),
    API_BASE_DEFAULT,
    ...(IS_LOCAL ? [
      'http://localhost:5432',
      'http://localhost:5000',
      'http://localhost:5001'
    ] : [])
  ].filter(Boolean);

  const uniqueCandidates = Array.from(new Set(candidates));

  for (const base of uniqueCandidates) {
    try {
      await fetchJson(`${base}/api/cleanmap/ping`, {}, 3000);
      apiBase = base;
      localStorage.setItem('cm_api_base', base);
      apiEnabled = true;
      return true;
    } catch {
      continue;
    }
  }

  apiEnabled = false;
  return false;
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
