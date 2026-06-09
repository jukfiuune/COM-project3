/**
 * Connection and API utilities for Nexora
 */
import { fetchJson } from './utils.js';
import { getAuthHeaders } from './auth.js';

export const API_BASE_DEFAULT = 'https://com-project3.onrender.com';
export const IS_LOCAL = location.hostname === 'localhost' || location.hostname === '127.0.0.1';

let apiBase = localStorage.getItem('cm_api_base') || API_BASE_DEFAULT;
let apiEnabled = false;

// Sanitize initial state
if (!IS_LOCAL && (apiBase.includes('localhost') || apiBase.includes('127.0.0.1'))) {
  apiBase = API_BASE_DEFAULT;
  localStorage.setItem('cm_api_base', apiBase);
}

/**
 * Builds candidate API URLs based on environment.
 * @returns {string[]}
 */
export function buildApiCandidates() {
  const stored = localStorage.getItem('cm_api_base');
  const preferred = API_BASE_DEFAULT;
  const storedIsLocal = stored && (stored.includes('localhost') || stored.includes('127.0.0.1'));
  
  const candidates = [
    storedIsLocal && !IS_LOCAL ? null : stored,
    preferred,
    ...(IS_LOCAL ? [
      'http://localhost:5432',
      'http://localhost:30543',
      'http://localhost:5210',
      'http://localhost:7210',
      'http://localhost:5000',
      'http://localhost:5001'
    ] : [])
  ];
  return Array.from(new Set(candidates.filter(Boolean)));
}

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
  const candidates = buildApiCandidates();
  for (const base of candidates) {
    try {
      await fetchJson(`${base}/api/cleanmap/ping`, {}, 6000);
      apiBase = base;
      localStorage.setItem('cm_api_base', base);
      apiEnabled = true;
      return base;
    } catch {
      continue;
    }
  }
  apiEnabled = false;
  return null;
}

/**
 * Authenticated wrapper for fetchJson.
 * @param {string} url - Target URL.
 * @param {RequestInit} [options={}] - Options.
 * @param {number} [timeoutMs=30000] - Timeout.
 */
export async function authFetch(url, options = {}, timeoutMs = 30000) {
  const headers = {
    'Content-Type': 'application/json',
    ...getAuthHeaders(),
    ...(options.headers || {})
  };
  return fetchJson(url, { ...options, headers }, timeoutMs);
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
