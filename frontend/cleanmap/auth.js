/**
 * Authentication and session management helper for Nexora
 */
import { showToast } from './utils.js';

export const auth = {
  get token() {
    return localStorage.getItem('cm_access_token');
  },
  set token(val) {
    if (val) {
      localStorage.setItem('cm_access_token', val);
    } else {
      localStorage.removeItem('cm_access_token');
    }
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
  const savedToken = auth.token;
  if (!auth.userId && savedToken && apiBase) {
    try {
      const res = await fetch(`${apiBase}/api/auth/me`, {
        headers: { Authorization: `Bearer ${savedToken}` },
        signal: AbortSignal.timeout(6000)
      });
      if (res.ok) {
        const user = await res.json();
        if (user?.id) {
          auth.user = user;
        }
      }
    } catch {
      // ignore restore failures
    }
  }
}
