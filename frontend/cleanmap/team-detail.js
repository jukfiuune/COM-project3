import { showToast, esc } from './utils.js';
import { auth, getAuthHeaders, restoreUserState, clearSession } from './auth.js';
import { getApiBase, detectApiBase } from './api.js';

const params = new URLSearchParams(window.location.search);
const teamId = params.get('id');
if (!teamId) window.location.href = 'teams.html';

let currentTeam = null;

async function detectApiAndLoad() {
  const base = await detectApiBase();
  if (!base) {
    showToast('Could not connect to API.', true);
    return;
  }

  await restoreUserState(getApiBase());
  loadTeam();
}

function updateUserLabel() {
  const navLabel = document.getElementById('navUserLabel');
  if (navLabel) {
    navLabel.textContent = auth.userId ? `You: ${auth.username || auth.userId}` : '';
  }
}

async function loadTeam() {
  const loader = document.getElementById('pageLoader');
  const content = document.getElementById('teamContent');
  if (loader) loader.style.display = 'flex';
  
  try {
    const res = await fetch(`${getApiBase()}/api/teams/${encodeURIComponent(teamId)}`);
    if (!res.ok) { window.location.href = 'teams.html'; return; }

    currentTeam = await res.json();
    renderHeader(currentTeam);
    renderMembers(currentTeam);
    await loadImages();

    document.title = `${currentTeam.name} — Nexora`;
    const navTeamName = document.getElementById('navTeamName');
    if (navTeamName) navTeamName.textContent = currentTeam.name;
    if (loader) loader.style.display = 'none';
    if (content) content.style.display = 'block';
  } catch {
    showToast('Could not load team.', true);
    if (loader) loader.style.display = 'none';
  }
}

function renderHeader(team) {
  const el = document.getElementById('teamHeader');
  if (el) {
    el.innerHTML = `
      <h1>${esc(team.name)}</h1>
      <p class="teams-subtitle">${esc(team.description ?? 'No description.')}</p>`;
  }
}

function renderMembers(team) {
  const isOwner = team.createdBy === auth.userId;
  const list = document.getElementById('membersList');
  if (!list) return;

  list.innerHTML = team.members.map(m => {
    const initials = String(m.userId).substring(0, 2).toUpperCase();
    const joined = new Date(m.joinedAt).toLocaleDateString();
    const canRemove = isOwner && m.userId !== auth.userId;
    const memberName = m.userId === auth.userId
      ? `You${auth.username ? `: ${auth.username}` : `: ${m.userId}`}`
      : m.userId;

    return `
      <div class="member-row">
        <div class="member-info">
          <div class="avatar">${initials}</div>
          <div>
            <div class="member-name">${esc(memberName)}</div>
            <div class="member-joined">Joined ${joined}</div>
          </div>
        </div>
        <div style="display:flex;align-items:center;gap:8px">
          <span class="role-badge role-${m.role}">${m.role}</span>
          ${canRemove ? `<button class="btn-danger" data-user-id="${esc(m.userId)}">Remove</button>` : ''}
        </div>
      </div>`;
  }).join('');

  const addMemberCard = document.getElementById('addMemberCard');
  if (addMemberCard) {
    addMemberCard.style.display = isOwner ? 'flex' : 'none';
  }
}

async function addMember() {
  const userId = document.getElementById('newMemberId').value.trim();
  if (!userId) { showToast('Enter a user ID.', true); return; }
  if (!auth.userId) { showToast('Set your user ID first.', true); return; }

  const authHeaders = getAuthHeaders();
  if (!authHeaders.Authorization) {
    showToast('Please login again.', true);
    return;
  }

  try {
    const res = await fetch(
      `${getApiBase()}/api/teams/${encodeURIComponent(teamId)}/members`,
      {
        method: 'POST',
        headers: { ...authHeaders, 'Content-Type': 'application/json' },
        body: JSON.stringify({ userId }),
      }
    );

    const data = await res.json().catch(() => ({}));
    if (!res.ok) { showToast(data.error ?? 'Failed to add member.', true); return; }

    document.getElementById('newMemberId').value = '';
    showToast('Member added.');
    loadTeam();
  } catch {
    showToast('Could not connect to API.', true);
  }
}

async function removeMember(targetUserId) {
  if (!confirm('Remove this member from the team?')) return;

  const authHeaders = getAuthHeaders();
  if (!authHeaders.Authorization) {
    showToast('Please login again.', true);
    return;
  }

  try {
    const res = await fetch(
      `${getApiBase()}/api/teams/${encodeURIComponent(teamId)}/members/${encodeURIComponent(targetUserId)}`,
      { method: 'DELETE', headers: authHeaders }
    );

    if (!res.ok) {
      const data = await res.json().catch(() => ({}));
      showToast(data.error ?? 'Failed to remove member.', true);
      return;
    }

    showToast('Member removed.');
    loadTeam();
  } catch {
    showToast('Could not connect to API.', true);
  }
}

async function loadImages() {
  try {
    const res = await fetch(`${getApiBase()}/api/teams/${encodeURIComponent(teamId)}/images`);
    const images = await res.json();
    renderImages(images);
  } catch {
    // silently handle image load issues
  }
}

function renderImages(images) {
  const grid = document.getElementById('imagesGrid');
  if (!grid) return;
  if (!images.length) {
    grid.innerHTML = `<p style="color:var(--text-muted);font-size:0.85rem;grid-column:1/-1">No images yet.</p>`;
    return;
  }
  grid.innerHTML = images.map(img => `
    <div class="image-card">
      <img src="${getApiBase()}${esc(img.imageUrl)}" alt="Team image" loading="lazy" />
      <div class="image-card-info">
        <p title="${esc(img.notes ?? '')}">${esc(img.notes ?? 'No notes')}</p>
        <span>By ${esc(img.uploadedBy)} · ${new Date(img.uploadedAt).toLocaleDateString()}</span>
      </div>
    </div>`).join('');
}

async function uploadImage() {
  if (!auth.userId) { showToast('Set your user ID first.', true); return; }

  const fileInput = document.getElementById('imageFile');
  const notes = document.getElementById('imageNotes').value.trim();

  if (!fileInput.files.length) { showToast('Select a file first.', true); return; }

  const formData = new FormData();
  formData.append('file', fileInput.files[0]);
  if (notes) formData.append('notes', notes);

  try {
    const params = new URLSearchParams({ userId: auth.userId });
    if (notes) params.append('notes', notes);

    const res = await fetch(
      `${getApiBase()}/api/teams/${encodeURIComponent(teamId)}/images?${params.toString()}`,
      { method: 'POST', body: formData }
    );

    if (!res.ok) {
      const data = await res.json().catch(() => ({}));
      showToast(data.error ?? 'Upload failed.', true);
      return;
    }

    fileInput.value = '';
    document.getElementById('imageNotes').value = '';
    showToast('Image uploaded!');
    loadImages();
  } catch {
    showToast('Could not connect to API.', true);
  }
}

function attachAuthButton() {
  const authBtn = document.getElementById('authBtn');
  if (!authBtn) return;

  authBtn.textContent = auth.userId ? 'Logout' : 'Login';
  authBtn.addEventListener('click', () => {
    if (auth.userId) {
      clearSession();
      const userLabel = document.getElementById('navUserLabel');
      if (userLabel) userLabel.textContent = '';
      showToast('Logged out.');
      window.location.href = 'teams.html';
    } else {
      const nextUrl = encodeURIComponent(`team-detail.html?id=${teamId}`);
      window.location.href = `login.html?next=${nextUrl}`;
    }
  });
}

function init() {
  // Bind static UI actions
  const btnAddMember = document.getElementById('btnAddMember');
  if (btnAddMember) btnAddMember.addEventListener('click', addMember);

  const btnUploadImage = document.getElementById('btnUploadImage');
  if (btnUploadImage) btnUploadImage.addEventListener('click', uploadImage);

  // Event delegation for dynamic member remove buttons
  const membersList = document.getElementById('membersList');
  if (membersList) {
    membersList.addEventListener('click', (e) => {
      const removeBtn = e.target.closest('.btn-danger');
      if (removeBtn) {
        const userId = removeBtn.dataset.userId;
        removeMember(userId);
      }
    });
  }

  updateUserLabel();
  attachAuthButton();
  void detectApiAndLoad();
}

document.addEventListener('DOMContentLoaded', init);
