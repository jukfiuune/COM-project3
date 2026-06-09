import { showToast, fetchJson } from './utils.js';
import { saveSession } from './auth.js';
import { getApiBase, detectApiBase } from './api.js';

const elements = {
  tabLogin: document.getElementById('tabLogin'),
  tabSignup: document.getElementById('tabSignup'),
  loginForm: document.getElementById('loginForm'),
  signupForm: document.getElementById('signupForm'),
  loginEmail: document.getElementById('loginEmail'),
  loginPassword: document.getElementById('loginPassword'),
  signupUsername: document.getElementById('signupUsername'),
  signupEmail: document.getElementById('signupEmail'),
  signupPassword: document.getElementById('signupPassword'),
  submitLogin: document.getElementById('submitLogin'),
  submitSignup: document.getElementById('submitSignup'),
  loginError: document.getElementById('loginError'),
  signupError: document.getElementById('signupError')
};

const nextPage = (() => {
  const urlParams = new URLSearchParams(window.location.search);
  let next = urlParams.get('next');
  if (next) {
    try {
      next = decodeURIComponent(next);
    } catch {
      // ignore invalid encoding
    }
  }

  if (!next && urlParams.has('id')) {
    next = `team-detail.html?id=${encodeURIComponent(urlParams.get('id') || '')}`;
  }

  return next || 'index.html';
})();

function switchTab(mode) {
  if (mode === 'login') {
    elements.tabLogin.classList.add('active');
    elements.tabSignup.classList.remove('active');
    elements.loginForm.classList.remove('hidden');
    elements.signupForm.classList.add('hidden');
  } else {
    elements.tabSignup.classList.add('active');
    elements.tabLogin.classList.remove('active');
    elements.signupForm.classList.remove('hidden');
    elements.loginForm.classList.add('hidden');
  }
}

async function login() {
  elements.submitLogin.disabled = true;
  elements.loginError.textContent = '';

  try {
    await ensureApi();
    const body = {
      email: elements.loginEmail.value.trim(),
      password: elements.loginPassword.value
    };
    const data = await fetchJson(`${getApiBase()}/api/auth/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body)
    });

    saveSession(data);
    showToast('Login successful. Redirecting...');
    setTimeout(() => window.location.href = nextPage, 700);
  } catch (err) {
    elements.loginError.textContent = err.message || 'Login failed.';
  } finally {
    elements.submitLogin.disabled = false;
  }
}

async function signup() {
  elements.submitSignup.disabled = true;
  elements.signupError.textContent = '';

  try {
    await ensureApi();
    const body = {
      username: elements.signupUsername.value.trim(),
      email: elements.signupEmail.value.trim(),
      password: elements.signupPassword.value
    };
    const data = await fetchJson(`${getApiBase()}/api/auth/signup`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body)
    });

    saveSession(data);
    showToast('Account created. Redirecting...');
    setTimeout(() => window.location.href = nextPage, 700);
  } catch (err) {
    elements.signupError.textContent = err.message || 'Signup failed.';
  } finally {
    elements.submitSignup.disabled = false;
  }
}

async function ensureApi() {
  const connected = await detectApiBase();
  if (!connected) throw new Error('Could not connect to the API.');
}

function init() {
  elements.tabLogin.addEventListener('click', () => switchTab('login'));
  elements.tabSignup.addEventListener('click', () => switchTab('signup'));
  elements.submitLogin.addEventListener('click', () => login());
  elements.submitSignup.addEventListener('click', () => signup());
}

init();
