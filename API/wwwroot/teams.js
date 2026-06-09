import { showToast, esc } from './utils.js';
import { auth, getAuthHeaders, restoreUserState, clearSession } from './auth.js';
import { getApiBase, detectApiBase } from './api.js';

async function detectApiAndLoad() {
  const base = await detectApiBase();
  if (!base) {
    document.getElementById('teamsContainer').innerHTML =
      `<div class="empty-state"><p>Could not connect to API. Is the server running?</p></div>`;
    return;
  }

  await restoreUserState(getApiBase());
  updateUserLabel();

  if (auth.userId) {
    loadTeams();
  } else {
    window.location.href = 'login.html?next=teams.html';
  }
}

function updateUserLabel() {
  const el = document.getElementById('currentUserLabel');
  if (!el) return;
  const userDisplay = auth.username || auth.userId;
  el.textContent = userDisplay ? `Signed in as: ${userDisplay}` : '';
  const authBtn = document.getElementById('authBtn');
  if (authBtn) {
    authBtn.textContent = auth.userId ? 'Logout' : 'Login';
  }
}

function attachAuthButton() {
  const authBtn = document.getElementById('authBtn');
  if (!authBtn) return;

  authBtn.addEventListener('click', () => {
    if (auth.userId) {
      clearSession();
      updateUserLabel();
      showToast('Logged out.');
      document.getElementById('teamsContainer').innerHTML =
        `<div class="empty-state"><p>Login to see your teams.</p></div>`;
    } else {
      window.location.href = `login.html?next=${encodeURIComponent('teams.html')}`;
    }
  });
}

async function loadTeams() {
  if (!auth.userId) return;
  document.getElementById('teamsContainer').innerHTML =
    '<div class="teams-loading"><div class="spinner"></div></div>';

  try {
    const res = await fetch(`${getApiBase()}/api/teams/my`, {
      headers: getAuthHeaders()
    });
    const teams = await res.json();
    renderTeams(teams);
  } catch {
    document.getElementById('teamsContainer').innerHTML =
      `<div class="empty-state"><p>Could not load teams.</p></div>`;
  }
}

function renderTeams(teams) {
  const container = document.getElementById('teamsContainer');
  if (!container) return;

  if (!teams.length) {
    container.innerHTML = `
      <div class="empty-state">
        <svg width="48" height="48" fill="none" stroke="currentColor" stroke-width="1.5" viewBox="0 0 24 24">
          <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"/>
          <circle cx="9" cy="7" r="4"/>
          <path d="M23 21v-2a4 4 0 0 0-3-3.87"/>
          <path d="M16 3.13a4 4 0 0 1 0 7.75"/>
        </svg>
        <p>You are not part of any team yet.</p>
      </div>`;
    return;
  }

  container.innerHTML = `<div class="teams-grid">${teams.map(teamCard).join('')}</div>`;
}

function teamCard(team) {
  const isOwner = team.createdBy === auth.userId;
  const role = isOwner ? 'owner' : 'member';
  const count = team.members.length;

  return `
    <div class="team-card" data-team-id="${esc(team.id)}">
      <h3>${esc(team.name)}</h3>
      <p>${esc(team.description ?? 'No description.')}</p>
      <div class="team-card-footer">
        <span class="member-count">${count} member${count !== 1 ? 's' : ''}</span>
        <span class="role-badge role-${role}">${role}</span>
        ${isOwner ? `<button class="btn danger btn-sm btn-delete-team" data-team-id="${esc(team.id)}" data-team-name="${esc(team.name)}">&times; Delete</button>` : ''}
      </div>
    </div>`;
}

function goToTeam(id) {
  window.location.href = `team-detail.html?id=${encodeURIComponent(id)}`;
}

function openCreateModal() {
  if (!auth.userId) { showToast('Set your user ID first.', true); return; }
  document.getElementById('createModal').classList.add('open');
  document.getElementById('teamName').focus();
}

function closeCreateModal() {
  document.getElementById('createModal').classList.remove('open');
  document.getElementById('teamName').value = '';
  document.getElementById('teamDesc').value = '';
}

async function createTeam() {
  const name = document.getElementById('teamName').value.trim();
  const description = document.getElementById('teamDesc').value.trim();

  if (!name) { showToast('Team name is required.', true); return; }

  try {
    const res = await fetch(`${getApiBase()}/api/teams`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', ...getAuthHeaders() },
      body: JSON.stringify({ name, description }),
    });

    if (!res.ok) {
      const data = await res.json().catch(() => ({}));
      showToast(data.error ?? 'Failed to create team.', true);
      return;
    }

    closeCreateModal();
    showToast('Team created!');
    loadTeams();
  } catch {
    showToast('Could not connect to API.', true);
  }
}

async function deleteTeam(teamId, teamName) {
  if (!confirm(`Delete team "${teamName}"? This cannot be undone.`)) return;

  try {
    const res = await fetch(
      `${getApiBase()}/api/teams/${encodeURIComponent(teamId)}`,
      { method: 'DELETE', headers: getAuthHeaders() }
    );

    if (!res.ok) {
      const data = await res.json().catch(() => ({}));
      showToast(data.error ?? 'Failed to delete team.', true);
      return;
    }

    showToast('Team deleted.');
    loadTeams();
  } catch {
    showToast('Could not connect to API.', true);
  }
}

function init() {
  // Bind static UI listeners
  document.getElementById('btnOpenCreateModal').addEventListener('click', openCreateModal);
  document.getElementById('btnCloseCreateModal').addEventListener('click', closeCreateModal);
  document.getElementById('btnCancelCreateModal').addEventListener('click', closeCreateModal);
  document.getElementById('btnConfirmCreateTeam').addEventListener('click', createTeam);
  
  // Overlay click to close
  document.getElementById('createModal').addEventListener('click', (e) => {
    if (e.target === e.currentTarget) closeCreateModal();
  });

  // Event delegation for dynamic team cards
  document.getElementById('teamsContainer').addEventListener('click', (e) => {
    const deleteBtn = e.target.closest('.btn-delete-team');
    if (deleteBtn) {
      e.stopPropagation();
      const teamId = deleteBtn.dataset.teamId;
      const teamName = deleteBtn.dataset.teamName;
      deleteTeam(teamId, teamName);
      return;
    }
    const card = e.target.closest('.team-card');
    if (card) {
      const teamId = card.dataset.teamId;
      goToTeam(teamId);
    }
  });

  updateUserLabel();
  attachAuthButton();

  if (!auth.userId) {
    showToast('Tap Login to sign in or register.', false);
  }

  void detectApiAndLoad();
}

document.addEventListener('DOMContentLoaded', init);
