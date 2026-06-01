const canvas = document.getElementById('map');
const ctx = canvas.getContext('2d');
const statusEl = document.getElementById('status');
const popup = document.getElementById('popup');
const searchInput = document.getElementById('search');
const searchBtn = document.getElementById('searchBtn');
const geojsonFile = document.getElementById('geojsonFile');
const exportGeoBtn = document.getElementById('exportGeo');
const exportJSONBtn = document.getElementById('exportJSON');
const exportMode = document.getElementById('exportMode');
const exportPrecision = document.getElementById('exportPrecision');
const exportName = document.getElementById('exportName');

const DPR = Math.max(1, window.devicePixelRatio || 1);
function setCanvasSize(){
  canvas.width = Math.floor(window.innerWidth * DPR);
  canvas.height = Math.floor(window.innerHeight * DPR);
  canvas.style.width = window.innerWidth + 'px';
  canvas.style.height = window.innerHeight + 'px';
}
window.addEventListener('resize', ()=>{ setCanvasSize(); draw(); });
setCanvasSize();

const tileSize = 256;
let zoomFloat = 8;
let centerLat = 42.563, centerLon = 25.422;
let centerPx = lonToX(centerLon, zoomFloat), centerPy = latToY(centerLat, zoomFloat);

const tileCache = new Map();

const markers = [ {lat:42.563, lon:25.422, label:'Origin (Bulgaria)'} ];

const localPOIs = [
  {name:'sofia', lat:42.6977, lon:23.3219},
  {name:'plovdiv', lat:42.1354, lon:24.7453},
  {name:'varna', lat:43.2141, lon:27.9147},
  {name:'burgas', lat:42.5048, lon:27.4626},
  {name:'ruse', lat:43.8563, lon:25.9704}
];

function lonToX(lon, z){ const n = Math.pow(2,z); return ((lon + 180) / 360) * n * tileSize; }
function latToY(lat, z){ const latRad = lat * Math.PI/180; const n = Math.pow(2,z); return (1 - Math.log(Math.tan(latRad) + 1/Math.cos(latRad)) / Math.PI) / 2 * n * tileSize; }
function xToLon(x, z){ const n = Math.pow(2,z); return x / (n * tileSize) * 360 - 180; }
function yToLat(y, z){ const n = Math.pow(2,z); const yNorm = y / (n * tileSize); const latRad = Math.atan(Math.sinh(Math.PI * (1 - 2*yNorm))); return latRad * 180/Math.PI; }

function draw(){
  ctx.setTransform(1,0,0,1,0,0);
  ctx.clearRect(0,0,canvas.width,canvas.height);
  const viewW = canvas.width / DPR, viewH = canvas.height / DPR;
  const topLeftX = centerPx - viewW/2;
  const topLeftY = centerPy - viewH/2;

  const zf = zoomFloat;
  const zInt = Math.floor(zf);
  const scale = Math.pow(2, zf - zInt);
  const n = Math.pow(2, zInt);

  const startTileX = Math.floor(topLeftX / (tileSize * scale));
  const startTileY = Math.floor(topLeftY / (tileSize * scale));
  const endTileX = Math.floor((topLeftX + viewW) / (tileSize * scale));
  const endTileY = Math.floor((topLeftY + viewH) / (tileSize * scale));

  for(let tx = startTileX; tx <= endTileX; tx++){
    for(let ty = startTileY; ty <= endTileY; ty++){
      const wrappedX = ((tx % n) + n) % n;
      if(ty < 0 || ty >= n) continue;
      const key = `${zInt}/${wrappedX}/${ty}`;
      let img = tileCache.get(key);
      if(!img){
        img = new Image(); img.crossOrigin = 'Anonymous';
        const subs = ['a','b','c','d'];
        const s = subs[Math.abs(wrappedX + ty) % subs.length];
        img.src = `https://${s}.basemaps.cartocdn.com/dark_all/${zInt}/${wrappedX}/${ty}.png`;
        img.onload = ()=>{ draw(); };
        img.onerror = ()=>{ img.src = `https://tile.openstreetmap.org/${zInt}/${wrappedX}/${ty}.png`; };
        tileCache.set(key, img);
      }
      const px = (tx * tileSize * scale - topLeftX) * DPR;
      const py = (ty * tileSize * scale - topLeftY) * DPR;
      if(img.complete) ctx.drawImage(img, px, py, tileSize * scale * DPR, tileSize * scale * DPR);
    }
  }

  try{
    applyBlueShift();
  }catch(err){ }

  const now = (performance && performance.now) ? performance.now() / 1000 : Date.now() / 1000;
  markers.forEach(m => {
    const mx = lonToX(m.lon, zoomFloat);
    const my = latToY(m.lat, zoomFloat);
    const sx = (mx - topLeftX) * DPR;
    const sy = (my - topLeftY) * DPR;
    const pulse = 1 + 0.25 * Math.sin(now * 3 + (m.lon + m.lat));
    const r = 6 * DPR * pulse;
    ctx.beginPath(); ctx.fillStyle = 'rgba(50,150,255,0.06)'; ctx.arc(sx, sy, r*4, 0, Math.PI*2); ctx.fill();
    ctx.beginPath(); ctx.fillStyle = '#66ccff'; ctx.strokeStyle = 'rgba(0,0,0,0.9)'; ctx.lineWidth = 2*DPR; ctx.arc(sx, sy, r, 0, Math.PI*2); ctx.fill(); ctx.stroke();
    ctx.beginPath(); ctx.fillStyle = '#000000'; ctx.arc(sx, sy, r*0.4, 0, Math.PI*2); ctx.fill();
    ctx.fillStyle = '#cfeeff'; ctx.font = `${12*DPR}px Arial`; ctx.fillText(m.label || `${m.lat.toFixed(4)},${m.lon.toFixed(4)}`, sx + 10*DPR, sy - 6*DPR);
  });

  ctx.save(); ctx.globalCompositeOperation = 'screen'; ctx.fillStyle = 'rgba(6,20,60,0.08)'; ctx.fillRect(0,0,canvas.width,canvas.height); ctx.restore();

  const coordsEl = document.getElementById('coords');
  if(coordsEl){ coordsEl.textContent = `${centerLat.toFixed(5)}, ${centerLon.toFixed(5)} (z=${zoomFloat.toFixed(2)})`; }
}

function rgbToHsl(r,g,b){ r/=255; g/=255; b/=255; const max=Math.max(r,g,b), min=Math.min(r,g,b); let h=0,s=0,l=(max+min)/2; if(max!==min){ const d=max-min; s = l>0.5? d/(2-max-min) : d/(max+min); switch(max){ case r: h = (g-b)/d + (g<b?6:0); break; case g: h = (b-r)/d + 2; break; case b: h = (r-g)/d + 4; break; } h /= 6; } return [h,s,l]; }
function hslToRgb(h,s,l){ let r,g,b; if(s===0){ r=g=b=l; } else { const hue2rgb = (p,q,t)=>{ if(t<0) t+=1; if(t>1) t-=1; if(t<1/6) return p + (q-p)*6*t; if(t<1/2) return q; if(t<2/3) return p + (q-p)*(2/3 - t)*6; return p; }; const q = l<0.5? l*(1+s) : l + s - l*s; const p = 2*l - q; r = hue2rgb(p,q,h+1/3); g = hue2rgb(p,q,h); b = hue2rgb(p,q,h-1/3); } return [Math.round(r*255), Math.round(g*255), Math.round(b*255)]; }

function applyBlueShift(){
  const w = canvas.width, h = canvas.height;
  const img = ctx.getImageData(0,0,w,h);
  const d = img.data;
  const target = 0.58;
  const lum = new Float32Array(w*h);
  const magA = new Float32Array(w*h);

  const centerSkipFraction = 0.0;
  const centerLeft = Math.floor(w * (1 - centerSkipFraction) / 2);
  const centerRight = Math.floor(w - (w * (1 - centerSkipFraction) / 2));

  for(let i=0, p=0;i<d.length;i+=4,p++){
    const x = p % w;
    const r0 = d[i], g0 = d[i+1], b0 = d[i+2];
    if(d[i+3] < 16){ lum[p] = 0; magA[p] = 0; continue; }
    lum[p] = 0.2126*r0 + 0.7152*g0 + 0.0722*b0;
    magA[p] = 0;
    if(x < centerLeft || x >= centerRight){
      let [hue,sat,light] = rgbToHsl(r0,g0,b0);
      const weight = 0.65;
      hue = hue + (target - hue) * weight;
      sat = Math.min(1, sat * (0.8 + 0.9 * (1 - Math.abs(hue - target))));
      light = Math.max(0, Math.min(1, light * 0.94 + 0.02));
      const [nr,ng,nb] = hslToRgb(hue,sat,light);
      d[i] = nr; d[i+1] = ng; d[i+2] = nb;
      lum[p] = 0.2126*nr + 0.7152*ng + 0.0722*nb;
    }
  }

  for(let y=1;y<h-1;y++){
    for(let x=1;x<w-1;x++){
      const a = lum[(y-1)*w + (x-1)]; const b = lum[(y-1)*w + x]; const c = lum[(y-1)*w + (x+1)];
      const d0 = lum[y*w + (x-1)]; const e = lum[y*w + x]; const f = lum[y*w + (x+1)];
      const g = lum[(y+1)*w + (x-1)]; const h0 = lum[(y+1)*w + x]; const i0 = lum[(y+1)*w + (x+1)];
      const gx = -a + c -2*d0 + 2*f - g + i0;
      const gy = -a -2*b - c + g + 2*h0 + i0;
      const mag = Math.sqrt(gx*gx + gy*gy);
      magA[y*w + x] = mag;
    }
  }

  const sea = new Uint8Array(w*h);
  for(let p=0;p<w*h;p++){
    const i = p*4; const r = d[i], g = d[i+1], b = d[i+2];
    const [hue,sat,light] = rgbToHsl(r,g,b);
    if(hue > 0.48 && hue < 0.74 && sat < 0.5 && light < 0.62){ sea[p]=1; }
  }
  for(let iter=0;iter<2;iter++){
    const newSea = sea.slice();
    for(let y=1;y<h-1;y++){
      for(let x=1;x<w-1;x++){
        const p = y*w + x; if(sea[p]) continue;
        let cnt=0; for(let oy=-1;oy<=1;oy++){ for(let ox=-1;ox<=1;ox++){ if(ox===0&&oy===0) continue; if(sea[(y+oy)*w + (x+ox)]) cnt++; }}
        if(cnt >= 4) newSea[p]=1;
      }
    }
    sea.set(newSea);
  }

  const edgeThreshold = 14;
  for(let y=1;y<h-1;y++){
    for(let x=1;x<w-1;x++){
      const p = y*w + x; const i = p*4; const mag = magA[p];
      if(mag <= edgeThreshold) continue;
      let isMax = true; for(let oy=-1;oy<=1;oy++){ for(let ox=-1;ox<=1;ox++){ if(ox===0&&oy===0) continue; if(magA[(y+oy)*w + (x+ox)] > mag*0.9) { isMax = false; break; } } if(!isMax) break; }
      if(!isMax) continue;
      if(sea[p]) continue;
      const intensity = Math.min(1, (mag - edgeThreshold) / 120);
      const br = 80, bg = 170, bb = 255;
      const alpha = 0.9 * intensity;
      d[i] = Math.round(d[i]*(1-alpha) + br*alpha);
      d[i+1] = Math.round(d[i+1]*(1-alpha) + bg*alpha);
      d[i+2] = Math.round(d[i+2]*(1-alpha) + bb*alpha);
    }
  }
  for(let p=0;p<w*h;p++){
    if(sea[p]){ const i = p*4; d[i]=6; d[i+1]=18; d[i+2]=40; }
  }

  ctx.putImageData(img,0,0);
  ctx.save(); ctx.globalCompositeOperation = 'screen'; ctx.fillStyle = 'rgba(6,20,60,0.06)'; ctx.fillRect(0,0,w,h); ctx.restore();
  const post = ctx.getImageData(0,0,w,h);
  const pd = post.data;
  for(let p=0;p<w*h;p++){ if(sea[p]){ const i = p*4; pd[i]=6; pd[i+1]=18; pd[i+2]=40; } }
  ctx.putImageData(post,0,0);
}

let dragging = false, dragMarker = -1;
let lastPos = {x:0,y:0};
canvas.addEventListener('wheel', e => {
  e.preventDefault();
  const rect = canvas.getBoundingClientRect();
  const sx = e.clientX - rect.left; const sy = e.clientY - rect.top;
  const viewW = canvas.width / DPR, viewH = canvas.height / DPR;
  const topLeftOldX = centerPx - viewW/2; const topLeftOldY = centerPy - viewH/2;

  const isZoomIntent = e.ctrlKey || e.metaKey;

  if(!isZoomIntent){
    centerPx -= e.deltaX / DPR;
    centerPy -= e.deltaY / DPR;
    centerLon = xToLon(centerPx, zoomFloat); centerLat = yToLat(centerPy, zoomFloat);
    draw();
    return;
  }

  const delta = e.deltaY;
  const change = -delta * 0.004;
  const oldZoom = zoomFloat;
  const newZoom = Math.max(2, Math.min(19, zoomFloat + change));
  if(newZoom === oldZoom) return;
  const s = Math.pow(2, newZoom - oldZoom);
  const worldXUnder = topLeftOldX + sx / DPR;
  const worldYUnder = topLeftOldY + sy / DPR;
  centerPx = worldXUnder + (centerPx - worldXUnder) * s;
  centerPy = worldYUnder + (centerPy - worldYUnder) * s;
  zoomFloat = newZoom;
  centerLon = xToLon(centerPx, zoomFloat); centerLat = yToLat(centerPy, zoomFloat);
  draw();
},{passive:false});

let gestureBaseZoom = null;
canvas.addEventListener('gesturestart', e => { e.preventDefault(); gestureBaseZoom = zoomFloat; }, {passive:false});
canvas.addEventListener('gesturechange', e => {
  e.preventDefault(); if(gestureBaseZoom == null) gestureBaseZoom = zoomFloat;
  const newZoom = Math.max(2, Math.min(19, gestureBaseZoom + Math.log2(e.scale)));
  const rect = canvas.getBoundingClientRect();
  const gx = (e.clientX || (rect.left + rect.width/2)) - rect.left;
  const gy = (e.clientY || (rect.top + rect.height/2)) - rect.top;
  const viewW = canvas.width / DPR, viewH = canvas.height / DPR;
  const topLeftOldX = centerPx - viewW/2; const topLeftOldY = centerPy - viewH/2;
  const s = Math.pow(2, newZoom - zoomFloat);
  const worldX = topLeftOldX + gx / DPR; const worldY = topLeftOldY + gy / DPR;
  centerPx = worldX + (centerPx - worldX) * s;
  centerPy = worldY + (centerPy - worldY) * s;
  zoomFloat = newZoom; centerLon = xToLon(centerPx, zoomFloat); centerLat = yToLat(centerPy, zoomFloat);
  draw();
},{passive:false});
canvas.addEventListener('gestureend', e => { gestureBaseZoom = null; }, {passive:false});


canvas.addEventListener('dblclick', e => {
  const rect = canvas.getBoundingClientRect(); const sx = (e.clientX - rect.left); const sy = (e.clientY - rect.top);
  const viewW = canvas.width/DPR, viewH = canvas.height/DPR; const topLeftX = centerPx - viewW/2; const topLeftY = centerPy - viewH/2;
  const worldX = topLeftX + sx / DPR; const worldY = topLeftY + sy / DPR;
  const lat = yToLat(worldY, zoomFloat); const lon = xToLon(worldX, zoomFloat);
  markers.push({lat, lon, label: `${lat.toFixed(4)},${lon.toFixed(4)}`}); draw();
});

canvas.addEventListener('click', e => {
  const rect = canvas.getBoundingClientRect(); const sx = (e.clientX - rect.left); const sy = (e.clientY - rect.top);
  const zf = zoomFloat; const viewW = canvas.width/DPR, viewH = canvas.height/DPR; const topLeftX = centerPx - viewW/2; const topLeftY = centerPy - viewH/2;
  let found = null; let minD = 1e9; let idx = -1;
  for(let i=0;i<markers.length;i++){
    const m = markers[i]; const mx = lonToX(m.lon,zf); const my = latToY(m.lat,zf); const dx = (mx - topLeftX)*DPR - sx; const dy = (my - topLeftY)*DPR - sy; const d = Math.hypot(dx,dy);
    if(d < minD && d < 12*DPR){ minD = d; found = m; idx = i; }
  }
  if(found){ showPopupAt(e.clientX, e.clientY, found, idx); } else hidePopup();
});

function showPopupAt(clientX, clientY, marker, idx){
  popup.classList.remove('hidden'); popup.style.left = (clientX + 8) + 'px'; popup.style.top = (clientY + 8) + 'px';
  popup.innerHTML = `<div><strong>${marker.label || ''}</strong></div><div style="margin-top:6px;display:flex;gap:6px;justify-content:flex-end"><button id="panHere">Pan</button><button id="deleteMarker">Delete</button></div>`;
  document.getElementById('panHere').onclick = ()=>{ centerLat = marker.lat; centerLon = marker.lon; centerPx = lonToX(centerLon, zoomFloat); centerPy = latToY(centerLat, zoomFloat); draw(); hidePopup(); };
  document.getElementById('deleteMarker').onclick = ()=>{ markers.splice(idx,1); hidePopup(); draw(); };
}
function hidePopup(){ popup.classList.add('hidden'); popup.innerHTML=''; }

searchBtn.addEventListener('click', ()=>{
  const q = (searchInput.value||'').trim(); if(!q) return;
  const coord = q.match(/^\s*([+-]?\d+(?:\.\d+)?)\s*,\s*([+-]?\d+(?:\.\d+)?)\s*$/);
  if(coord){ const lat = Number(coord[1]), lon = Number(coord[2]); centerLat = lat; centerLon = lon; centerPx = lonToX(lon, zoomFloat); centerPy = latToY(lat, zoomFloat); draw(); return; }
  const found = localPOIs.find(p=>p.name.toLowerCase()===q.toLowerCase()); if(found){ centerLat = found.lat; centerLon = found.lon; centerPx = lonToX(centerLon, zoomFloat); centerPy = latToY(centerLat, zoomFloat); draw(); return; }
  alert('Not found. Use lat,lon or known POI (e.g., Sofia).');
});

geojsonFile.addEventListener('change', e=>{
  const f = e.target.files && e.target.files[0]; if(!f) return; const reader = new FileReader();
  reader.onload = ()=>{ try{ const json = JSON.parse(reader.result); if(json.type==='FeatureCollection'){ json.features.forEach(feat=>{ if(feat.geometry && feat.geometry.type==='Point' && Array.isArray(feat.geometry.coordinates)){ const [lon,lat] = feat.geometry.coordinates; markers.push({lat,lon,label:(feat.properties && feat.properties.label) || feat.properties && feat.properties.name || ''}); } }); draw(); } else alert('Unsupported GeoJSON'); }catch(err){ alert('Invalid file'); } };
  reader.readAsText(f);
});

exportGeoBtn.addEventListener('click', ()=>{
  const prec = Math.max(0, parseInt(exportPrecision.value||'2',10)); const name = (exportName.value||'markers').replace(/[^a-z0-9-_]/ig,'');
  const features = markers.map(m=>({ type:'Feature', properties:{label:m.label}, geometry:{type:'Point', coordinates:[Number(m.lon.toFixed(prec)), Number(m.lat.toFixed(prec))] } }));
  const fc = { type:'FeatureCollection', features };
  const blob = new Blob([JSON.stringify(fc, null, 2)], {type:'application/geo+json'}); const url = URL.createObjectURL(blob); const a = document.createElement('a'); a.href = url; a.download = `${name}.geojson`; a.click(); URL.revokeObjectURL(url);
});
exportJSONBtn.addEventListener('click', ()=>{ const name = (exportName.value||'markers').replace(/[^a-z0-9-_]/ig,''); const blob = new Blob([JSON.stringify(markers, null, 2)], {type:'application/json'}); const url = URL.createObjectURL(blob); const a = document.createElement('a'); a.href = url; a.download = `${name}.json`; a.click(); URL.revokeObjectURL(url); });

const fallback = document.getElementById('fallbackMap'); if(fallback) fallback.style.display='none';

if(statusEl) statusEl.textContent = 'Map ready (OpenStreetMap)';
draw();
