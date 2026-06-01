const storageKey = 'cleanmap_v1';

const state = {
  db: { reports: [], lastCenter: null, lastZoom: 13 },
  map: null,
  markers: new Map(),
  currentStream: null,
  reportPhoto: null,
  cleanPhoto: null,
  currentLocation: null,
  activeReportId: null,
  userMarker: null,
  apiBase: null,
  apiEnabled: false
};

const elements = {
  reportBtn: document.getElementById('reportBtn'),
  reportBtnBottom: document.getElementById('reportBtnBottom'),
  reviewBtn: document.getElementById('reviewBtn'),
  locateBtn: document.getElementById('locateBtn'),
  progressText: document.getElementById('progressText'),
  progressFill: document.getElementById('progressFill'),
  progressFillBottom: document.getElementById('progressFillBottom'),
  totalCount: document.getElementById('totalCount'),
  cleanedCount: document.getElementById('cleanedCount'),
  dirtyCount: document.getElementById('dirtyCount'),
  reportModal: document.getElementById('reportModal'),
  cleanModal: document.getElementById('cleanModal'),
  detailModal: document.getElementById('detailModal'),
  coordText: document.getElementById('coordText'),
  addressInput: document.getElementById('addressInput'),
  addressBtn: document.getElementById('addressBtn'),
  notesInput: document.getElementById('notesInput'),
  reportVideo: document.getElementById('reportVideo'),
  reportPreview: document.getElementById('reportPreview'),
  captureReport: document.getElementById('captureReport'),
  retakeReport: document.getElementById('retakeReport'),
  submitReport: document.getElementById('submitReport'),
  cancelReport: document.getElementById('cancelReport'),
  closeReport: document.getElementById('closeReport'),
  detailTitle: document.getElementById('detailTitle'),
  detailStatus: document.getElementById('detailStatus'),
  detailBefore: document.getElementById('detailBefore'),
  detailAfter: document.getElementById('detailAfter'),
  detailCoords: document.getElementById('detailCoords'),
  detailAddress: document.getElementById('detailAddress'),
  detailTags: document.getElementById('detailTags'),
  detailDate: document.getElementById('detailDate'),
  detailCleaned: document.getElementById('detailCleaned'),
  detailCleanedDate: document.getElementById('detailCleanedDate'),
  afterBlock: document.getElementById('afterBlock'),
  markClean: document.getElementById('markClean'),
  closeDetail: document.getElementById('closeDetail'),
  cleanVideo: document.getElementById('cleanVideo'),
  cleanPreview: document.getElementById('cleanPreview'),
  captureClean: document.getElementById('captureClean'),
  retakeClean: document.getElementById('retakeClean'),
  submitClean: document.getElementById('submitClean'),
  cancelClean: document.getElementById('cancelClean'),
  closeClean: document.getElementById('closeClean'),
  toast: document.getElementById('toast')
};

function loadDB() {
  const raw = localStorage.getItem(storageKey);
  if (!raw) return { reports: [], lastCenter: null, lastZoom: 13 };
  try {
    const parsed = JSON.parse(raw);
    if (!parsed.reports) parsed.reports = [];
    return parsed;
  } catch (err) {
    return { reports: [], lastCenter: null, lastZoom: 13 };
  }
}

function saveDB() {
  localStorage.setItem(storageKey, JSON.stringify(state.db));
}

function showToast(message) {
  elements.toast.textContent = message;
  elements.toast.classList.remove('hidden');
  setTimeout(() => elements.toast.classList.add('hidden'), 2400);
}

function buildApiCandidates() {
  const stored = localStorage.getItem(`${storageKey}_api`);
  const candidates = [
    stored,
    'http://localhost:5210',
    'https://localhost:7210',
    'http://localhost:5000',
    'https://localhost:5001'
  ];
  return Array.from(new Set(candidates.filter(Boolean)));
}

async function fetchJson(url, options = {}, timeoutMs = 4000) {
  const controller = new AbortController();
  const timeoutId = setTimeout(() => controller.abort(), timeoutMs);
  try {
    const response = await fetch(url, { ...options, signal: controller.signal });
    if (!response.ok) {
      throw new Error(`HTTP ${response.status}`);
    }
    const contentType = response.headers.get('content-type') || '';
    if (contentType.includes('application/json')) {
      return await response.json();
    }
    return null;
  } finally {
    clearTimeout(timeoutId);
  }
}

async function detectApiBase() {
  const candidates = buildApiCandidates();
  for (const base of candidates) {
    try {
      await fetchJson(`${base}/api/cleanmap/health`, {}, 2000);
      state.apiBase = base;
      state.apiEnabled = true;
      return true;
    } catch (err) {
      continue;
    }
  }
  state.apiEnabled = false;
  state.apiBase = null;
  return false;
}

async function syncFromApi() {
  const detected = await detectApiBase();
  if (!detected) return;
  try {
    const reports = await fetchJson(`${state.apiBase}/api/cleanmap/reports`);
    if (Array.isArray(reports)) {
      state.db.reports = reports;
      saveDB();
      renderMarkers();
      updateProgress();
      showToast('Connected to CleanMap server.');
    }
  } catch (err) {
    state.apiEnabled = false;
    state.apiBase = null;
  }
}

async function createReportApi(report) {
  return fetchJson(`${state.apiBase}/api/cleanmap/reports`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(report)
  });
}

async function markCleanApi(reportId, payload) {
  return fetchJson(`${state.apiBase}/api/cleanmap/reports/${encodeURIComponent(reportId)}/clean`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload)
  });
}

function initMap() {
  const defaultCenter = state.db.lastCenter || [42.6977, 23.3219];
  state.map = L.map('map', {
    zoomControl: false,
    minZoom: 3,
    maxZoom: 19
  }).setView(defaultCenter, state.db.lastZoom || 13);

  L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
    attribution: '&copy; OpenStreetMap contributors',
    subdomains: 'abc'
  }).addTo(state.map);

  L.control.zoom({ position: 'bottomright' }).addTo(state.map);

  state.map.on('moveend', () => {
    const center = state.map.getCenter();
    state.db.lastCenter = [center.lat, center.lng];
    state.db.lastZoom = state.map.getZoom();
    saveDB();
  });
}

function makeIcon(status) {
  const cls = status === 'cleaned' ? 'marker-clean' : 'marker-dirty';
  return L.divIcon({
    className: 'cleanmap-marker',
    html: `<div class="marker ${cls}"><span class="marker-badge"></span></div>`,
    iconSize: [28, 38],
    iconAnchor: [14, 28]
  });
}

function addMarker(report) {
  const marker = L.marker([report.lat, report.lng], { icon: makeIcon(report.status) }).addTo(state.map);
  marker.on('click', () => openDetail(report.id));
  state.markers.set(report.id, marker);
}

function renderMarkers() {
  state.markers.forEach(marker => marker.remove());
  state.markers.clear();
  state.db.reports.forEach(addMarker);
}

function updateProgress() {
  const total = state.db.reports.length;
  const cleaned = state.db.reports.filter(report => report.status === 'cleaned').length;
  const dirty = total - cleaned;
  const ratio = total === 0 ? 0 : cleaned / total;

  elements.progressText.textContent = `${cleaned}/${total}`;
  elements.progressFill.style.width = `${ratio * 100}%`;
  elements.progressFillBottom.style.width = `${ratio * 100}%`;
  elements.totalCount.textContent = total;
  elements.cleanedCount.textContent = cleaned;
  elements.dirtyCount.textContent = dirty;
}

function setLocation(lat, lng, panMap) {
  state.currentLocation = { lat, lng };
  elements.coordText.textContent = `${lat.toFixed(5)}, ${lng.toFixed(5)}`;
  if (panMap && state.map) state.map.setView([lat, lng], Math.max(state.map.getZoom(), 15));
  if (!state.userMarker) {
    state.userMarker = L.circleMarker([lat, lng], {
      radius: 6,
      color: '#40c9ff',
      fillColor: '#40c9ff',
      fillOpacity: 0.7
    }).addTo(state.map);
  } else {
    state.userMarker.setLatLng([lat, lng]);
  }
}

function locateUser(panMap) {
  if (!navigator.geolocation) {
    showToast('Geolocation is not supported. Using map center.');
    if (state.map) {
      const center = state.map.getCenter();
      setLocation(center.lat, center.lng, false);
    }
    return;
  }

  navigator.geolocation.getCurrentPosition(
    position => {
      setLocation(position.coords.latitude, position.coords.longitude, panMap);
    },
    () => {
      showToast('Unable to fetch location. Using map center.');
      if (state.map) {
        const center = state.map.getCenter();
        setLocation(center.lat, center.lng, false);
      }
    },
    { enableHighAccuracy: true, timeout: 8000, maximumAge: 30000 }
  );
}

function startCamera(videoEl) {
  stopCamera();
  if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
    showToast('Camera requires https or localhost.');
    return Promise.resolve();
  }
  return navigator.mediaDevices.getUserMedia({
    video: { facingMode: { ideal: 'environment' } },
    audio: false
  }).then(stream => {
    state.currentStream = stream;
    videoEl.srcObject = stream;
    return videoEl.play();
  }).catch(() => {
    showToast('Camera unavailable.');
  });
}

function stopCamera() {
  if (state.currentStream) {
    state.currentStream.getTracks().forEach(track => track.stop());
    state.currentStream = null;
  }
}

function captureFrame(videoEl) {
  if (!videoEl.videoWidth || !videoEl.videoHeight) {
    showToast('Camera not ready yet.');
    return null;
  }
  const canvas = document.createElement('canvas');
  canvas.width = videoEl.videoWidth || 1280;
  canvas.height = videoEl.videoHeight || 720;
  const ctx = canvas.getContext('2d');
  ctx.drawImage(videoEl, 0, 0, canvas.width, canvas.height);
  return canvas.toDataURL('image/jpeg', 0.85);
}

function openModal(el) {
  el.classList.remove('hidden');
  el.setAttribute('aria-hidden', 'false');
}

function closeModal(el) {
  el.classList.add('hidden');
  el.setAttribute('aria-hidden', 'true');
}

function resetReportForm() {
  state.reportPhoto = null;
  state.currentLocation = null;
  elements.reportPreview.classList.add('hidden');
  elements.reportVideo.classList.remove('hidden');
  elements.retakeReport.classList.add('hidden');
  elements.captureReport.classList.remove('hidden');
  elements.notesInput.value = '';
  elements.addressInput.value = localStorage.getItem(`${storageKey}_address`) || '';

  document.querySelectorAll('.tag-button').forEach(btn => btn.classList.remove('selected'));
  elements.coordText.textContent = 'Locating...';
}

function ensureLocationForReport() {
  if (state.currentLocation) return true;
  if (state.map) {
    const center = state.map.getCenter();
    setLocation(center.lat, center.lng, false);
    return true;
  }
  return false;
}

function openReportModal() {
  resetReportForm();
  openModal(elements.reportModal);
  locateUser(true);
  startCamera(elements.reportVideo);
}

function closeReportModal() {
  stopCamera();
  closeModal(elements.reportModal);
}

function openDetail(reportId) {
  const report = state.db.reports.find(item => item.id === reportId);
  if (!report) return;

  elements.detailTitle.textContent = report.address || 'Pollution Report';
  elements.detailStatus.textContent = `Status: ${report.status === 'cleaned' ? 'Cleaned' : 'Dirty'}`;
  elements.detailBefore.src = report.photoBefore;
  elements.detailCoords.textContent = `${report.lat.toFixed(5)}, ${report.lng.toFixed(5)}`;
  elements.detailAddress.textContent = report.address || 'No address provided';
  elements.detailTags.textContent = report.tags.length ? report.tags.join(', ') : 'None';
  elements.detailDate.textContent = formatDate(report.createdAt);

  if (report.status === 'cleaned' && report.photoAfter) {
    elements.afterBlock.classList.remove('hidden');
    elements.detailAfter.src = report.photoAfter;
    elements.detailCleaned.classList.remove('hidden');
    elements.detailCleanedDate.textContent = formatDate(report.cleanedAt);
    elements.markClean.classList.add('hidden');
  } else {
    elements.afterBlock.classList.add('hidden');
    elements.detailCleaned.classList.add('hidden');
    elements.markClean.classList.remove('hidden');
    elements.markClean.dataset.reportId = report.id;
  }

  openModal(elements.detailModal);
}

function closeDetailModal() {
  closeModal(elements.detailModal);
}

function openCleanModal(reportId) {
  state.activeReportId = reportId;
  state.cleanPhoto = null;
  elements.cleanPreview.classList.add('hidden');
  elements.cleanVideo.classList.remove('hidden');
  elements.retakeClean.classList.add('hidden');
  elements.captureClean.classList.remove('hidden');
  openModal(elements.cleanModal);
  startCamera(elements.cleanVideo);
}

function closeCleanModal() {
  stopCamera();
  closeModal(elements.cleanModal);
  state.activeReportId = null;
}

function formatDate(timestamp) {
  return new Date(timestamp).toLocaleString();
}

async function submitReport() {
  if (!ensureLocationForReport()) {
    showToast('Location not ready yet.');
    return;
  }

  if (!state.reportPhoto) {
    showToast('Capture a photo before submitting.');
    return;
  }

  const tags = Array.from(document.querySelectorAll('.tag-button.selected')).map(btn => btn.dataset.tag);
  if (tags.length === 0) {
    showToast('Select at least one waste type.');
    return;
  }

  const address = elements.addressInput.value.trim();
  const report = {
    id: `rep_${Date.now().toString(36)}_${Math.random().toString(36).slice(2, 7)}`,
    lat: state.currentLocation.lat,
    lng: state.currentLocation.lng,
    address,
    tags,
    notes: elements.notesInput.value.trim(),
    status: 'dirty',
    photoBefore: state.reportPhoto,
    photoAfter: null,
    createdAt: Date.now(),
    cleanedAt: null
  };

  let savedReport = report;
  if (state.apiEnabled) {
    try {
      const apiReport = await createReportApi(report);
      if (apiReport) savedReport = apiReport;
    } catch (err) {
      state.apiEnabled = false;
      state.apiBase = null;
      showToast('Server not reachable. Saved locally.');
    }
  }

  state.db.reports.push(savedReport);
  saveDB();
  addMarker(savedReport);
  updateProgress();
  closeReportModal();
  showToast('Report saved.');
}

async function submitClean() {
  if (!state.cleanPhoto || !state.activeReportId) {
    showToast('Capture an after photo.');
    return;
  }

  const report = state.db.reports.find(item => item.id === state.activeReportId);
  if (!report) return;

  if (state.apiEnabled) {
    try {
      const updated = await markCleanApi(report.id, { photoAfter: state.cleanPhoto });
      if (updated) {
        report.status = updated.status;
        report.photoAfter = updated.photoAfter;
        report.cleanedAt = updated.cleanedAt;
      }
    } catch (err) {
      state.apiEnabled = false;
      state.apiBase = null;
      showToast('Server not reachable. Saved locally.');
    }
  }

  if (!state.apiEnabled || report.status !== 'cleaned') {
    report.status = 'cleaned';
    report.photoAfter = state.cleanPhoto;
    report.cleanedAt = Date.now();
  }

  saveDB();

  const marker = state.markers.get(report.id);
  if (marker) marker.setIcon(makeIcon('cleaned'));

  updateProgress();
  closeCleanModal();
  closeDetailModal();
  showToast('Marked as cleaned.');
}

function bindUI() {
  elements.reportBtn.addEventListener('click', openReportModal);
  elements.reportBtnBottom.addEventListener('click', openReportModal);
  elements.locateBtn.addEventListener('click', () => locateUser(true));
  elements.reviewBtn.addEventListener('click', () => {
    const latest = state.db.reports[state.db.reports.length - 1];
    if (latest) openDetail(latest.id);
    else showToast('No reports yet.');
  });

  elements.addressBtn.addEventListener('click', () => {
    if (elements.addressInput.value.trim()) {
      localStorage.setItem(`${storageKey}_address`, elements.addressInput.value.trim());
      showToast('Address stored for reuse.');
      return;
    }
    const stored = localStorage.getItem(`${storageKey}_address`) || '';
    const promptValue = window.prompt('Enter your address', stored);
    if (promptValue) {
      elements.addressInput.value = promptValue;
      localStorage.setItem(`${storageKey}_address`, promptValue);
    }
  });

  elements.captureReport.addEventListener('click', () => {
    const captured = captureFrame(elements.reportVideo);
    if (!captured) return;
    state.reportPhoto = captured;
    elements.reportPreview.src = state.reportPhoto;
    elements.reportPreview.classList.remove('hidden');
    elements.reportVideo.classList.add('hidden');
    elements.captureReport.classList.add('hidden');
    elements.retakeReport.classList.remove('hidden');
    stopCamera();
  });

  elements.retakeReport.addEventListener('click', () => {
    state.reportPhoto = null;
    elements.reportPreview.classList.add('hidden');
    elements.reportVideo.classList.remove('hidden');
    elements.captureReport.classList.remove('hidden');
    elements.retakeReport.classList.add('hidden');
    startCamera(elements.reportVideo);
  });

  elements.submitReport.addEventListener('click', () => { void submitReport(); });
  elements.cancelReport.addEventListener('click', closeReportModal);
  elements.closeReport.addEventListener('click', closeReportModal);

  elements.captureClean.addEventListener('click', () => {
    const captured = captureFrame(elements.cleanVideo);
    if (!captured) return;
    state.cleanPhoto = captured;
    elements.cleanPreview.src = state.cleanPhoto;
    elements.cleanPreview.classList.remove('hidden');
    elements.cleanVideo.classList.add('hidden');
    elements.captureClean.classList.add('hidden');
    elements.retakeClean.classList.remove('hidden');
    stopCamera();
  });

  elements.retakeClean.addEventListener('click', () => {
    state.cleanPhoto = null;
    elements.cleanPreview.classList.add('hidden');
    elements.cleanVideo.classList.remove('hidden');
    elements.captureClean.classList.remove('hidden');
    elements.retakeClean.classList.add('hidden');
    startCamera(elements.cleanVideo);
  });

  elements.submitClean.addEventListener('click', () => { void submitClean(); });
  elements.cancelClean.addEventListener('click', closeCleanModal);
  elements.closeClean.addEventListener('click', closeCleanModal);

  elements.closeDetail.addEventListener('click', closeDetailModal);
  elements.markClean.addEventListener('click', () => {
    openCleanModal(elements.markClean.dataset.reportId);
  });

  document.querySelectorAll('.tag-button').forEach(btn => {
    btn.addEventListener('click', () => btn.classList.toggle('selected'));
  });
}

async function init() {
  state.db = loadDB();
  initMap();
  bindUI();
  renderMarkers();
  updateProgress();
  await syncFromApi();
}

document.addEventListener('DOMContentLoaded', init);
