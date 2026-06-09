/**
 * Authentication and session management helper for Nexora
 */
import { showToast } from './utils.js';

let _accessToken = null;

export const auth = {
  get token() {
    return _accessToken;
  },
  set token(val) {
    _accessToken = val;
  },
  get user() {
    try {
      return JSON.parse(localStorage.getItem('cm_user') || 'null');
    } catch {
      return null;
    }
  },
  set user(val) {
    if (val) {
      localStorage.setItem('cm_user', JSON.stringify(val));
      localStorage.setItem('cm_user_id', val.id || val.userId || '');
    } else {
      localStorage.removeItem('cm_user');
      localStorage.removeItem('cm_user_id');
    }
  },
  get userId() {
    return localStorage.getItem('cm_user_id') || '';
  },
  get username() {
    return this.user?.username || '';
  }
};

/**
 * Checks if the user session is active.
 * @returns {boolean}
 */
export function isAuthenticated() {
  return !!auth.token && !!auth.user;
}

/**
 * Save authentication details to storage.
 * @param {object} data - Auth payload with accessToken and user.
 */
export function saveSession(data) {
  auth.token = data.accessToken;
  auth.user = data.user;
}

/**
 * Clear the current user session.
 */
export function clearSession() {
  auth.token = null;
  auth.user = null;
}

/**
 * Generate authorization headers for API requests.
 * @returns {object} Auth headers dictionary.
 */
export function getAuthHeaders() {
  const token = auth.token;
  return token ? { Authorization: `Bearer ${token}` } : {};
}

/**
 * Redirect user to the login page with a return path.
 * @param {string} [next='index.html'] - Page to redirect to after success.
 */
export function redirectToLogin(next = 'index.html') {
  window.location.href = `login.html?next=${encodeURIComponent(next)}`;
}

/**
 * Validate that user is logged in. Redirect to login if not.
 * @param {string} action - Return path if redirect occurs.
 * @returns {boolean} True if authenticated.
 */
export function requireAuth(action) {
  if (!isAuthenticated()) {
    showToast('Login required before continuing.', true);
    setTimeout(() => redirectToLogin(action), 800);
    return false;
  }
  return true;
}

/**
 * Attempts to restore user details from the backend using the stored token.
 * @param {string} apiBase - The active API base URL.
 */
export async function restoreUserState(apiBase) {
  if (!auth.userId && apiBase) {
    try {
      function getCsrfToken() {
        const match = document.cookie.match(new RegExp('(^| )csrfToken=([^;]+)'));
        return match ? match[2] : '';
      }

      const res = await fetch(`${apiBase}/api/auth/refresh`, {
        method: 'POST',
        credentials: 'include',
        headers: { 'X-CSRF-Token': getCsrfToken() },
        signal: AbortSignal.timeout(6000)
      });
      if (res.ok) {
        const data = await res.json();
        saveSession(data);
      }
    } catch {
      // ignore restore failures
    }
  }
}
