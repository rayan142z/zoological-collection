import { HttpErrorResponse, HttpInterceptorFn, HttpRequest, HttpResponse } from '@angular/common/http';
import { Observable, catchError, delay, from, of, switchMap, throwError } from 'rxjs';
import { environment } from '../../environments/environment';
import { DemoDb, DemoSpecimen, DemoUser, DEMO_PASSWORD, createSeed } from './demo-data';

// In-browser replacement for the ASP.NET backend, used only by the demo build.
// It follows the same routes, response shapes and permission rules as the real
// controllers, and keeps all changes in this visitor's localStorage.

const STORAGE_KEY = 'zoolog_demo_db_v1';
const LATENCY_MS = 150;

type Result = { status: number; body?: unknown } | Promise<{ status: number; body?: unknown }>;
interface Ctx {
  req: HttpRequest<unknown>;
  db: DemoDb;
  user: DemoUser | null;
  params: string[];
  query: URLSearchParams;
}
type Handler = (ctx: Ctx) => Result;

export function loadDemoDb(): DemoDb {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (raw) return JSON.parse(raw) as DemoDb;
  } catch {
    // Storage unavailable or corrupt: fall back to a fresh seed.
  }
  return createSeed();
}

function saveDb(db: DemoDb): void {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(db));
  } catch {
    // Quota exceeded or storage blocked: changes live only until reload.
  }
}

/** Restores the original sample data (used by the demo banner). */
export function resetDemoDb(): void {
  try {
    localStorage.removeItem(STORAGE_KEY);
  } catch {
    // ignore
  }
}

// ── Fake JWT ────────────────────────────────────────────────────────────────
// Auth.isTokenValid() only decodes the payload's exp, so an unsigned token is enough.
function b64url(value: object): string {
  return btoa(JSON.stringify(value)).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
}

function makeToken(user: DemoUser): string {
  const exp = Math.floor(Date.now() / 1000) + 60 * 60 * 8;
  return `${b64url({ alg: 'none', typ: 'JWT' })}.${b64url({ sub: String(user.id), role: user.userRole, exp })}.demo`;
}

function userFromRequest(req: HttpRequest<unknown>, db: DemoDb): DemoUser | null {
  const header = req.headers.get('Authorization');
  if (!header?.startsWith('Bearer ')) return null;
  try {
    const payload = JSON.parse(atob(header.slice(7).split('.')[1].replace(/-/g, '+').replace(/_/g, '/')));
    if (payload.exp * 1000 < Date.now()) return null;
    return db.users.find((u) => u.id === Number(payload.sub) && u.status === 'active') ?? null;
  } catch {
    return null;
  }
}

// ── Helpers ─────────────────────────────────────────────────────────────────
const ok = (body?: unknown) => ({ status: 200, body });
const created = (body?: unknown) => ({ status: 201, body });
const noContent = () => ({ status: 204 });
const fail = (status: number, message: string) => ({ status, body: { message } });

const isPrivileged = (u: DemoUser | null) => u?.userRole === 'admin' || u?.userRole === 'moderator';
const nextId = (rows: { id: number }[]) => rows.reduce((max, r) => Math.max(max, r.id), 0) + 1;
const today = () => new Date().toISOString().split('T')[0];

function requireUser(ctx: Ctx): DemoUser {
  if (!ctx.user) throw fail(401, 'Bitte melde dich an.');
  return ctx.user;
}

function requireRole(ctx: Ctx, ...roles: string[]): DemoUser {
  const user = requireUser(ctx);
  if (!roles.includes(user.userRole)) throw fail(403, 'Keine Berechtigung.');
  return user;
}

function creatorOf(db: DemoDb, id: number) {
  const u = db.users.find((x) => x.id === id);
  return u
    ? { id: u.id, username: u.username, email: u.email, userRole: u.userRole, status: u.status, description: u.description, job: u.job }
    : null;
}

function collectionView(db: DemoDb, c: DemoDb['collections'][number]) {
  return { id: c.id, name: c.name, description: c.description, isPublic: c.isPublic, createdBy: c.createdBy, creator: creatorOf(db, c.createdBy), createdAt: c.createdAt };
}

function visibleSpecimens(ctx: Ctx): DemoSpecimen[] {
  const { db, user } = ctx;
  return db.specimens.filter((s) => {
    const col = db.collections.find((c) => c.id === s.collectionId);
    const tax = db.taxonomies.find((t) => t.id === s.taxonomyId);
    const own = !!user && s.addedBy === user.id;
    return (col?.isPublic || own) && (tax?.validated || own || isPrivileged(user));
  });
}

function specimenView(db: DemoDb, s: DemoSpecimen) {
  const col = db.collections.find((c) => c.id === s.collectionId);
  const tax = db.taxonomies.find((t) => t.id === s.taxonomyId);
  return { ...s, createdBy: col?.createdBy, taxonomyValidated: !!tax?.validated };
}

function formValue(body: unknown, key: string): string | null {
  if (body instanceof FormData) {
    const v = body.get(key);
    return typeof v === 'string' ? v : null;
  }
  return null;
}

const num = (v: string | null) => (v === null || v.trim() === '' || isNaN(Number(v)) ? null : Number(v));

function readFileAsDataUrl(file: File): Promise<string> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = () => resolve(String(reader.result));
    reader.onerror = () => reject(reader.error);
    reader.readAsDataURL(file);
  });
}

/** Downscales uploaded photos so they fit comfortably in localStorage. */
async function compressImage(file: File): Promise<string> {
  const dataUrl = await readFileAsDataUrl(file);
  try {
    const img = new Image();
    img.src = dataUrl;
    await img.decode();
    const scale = Math.min(1, 640 / Math.max(img.width, img.height));
    const canvas = document.createElement('canvas');
    canvas.width = Math.round(img.width * scale);
    canvas.height = Math.round(img.height * scale);
    canvas.getContext('2d')!.drawImage(img, 0, 0, canvas.width, canvas.height);
    return canvas.toDataURL('image/jpeg', 0.75);
  } catch {
    return dataUrl;
  }
}

function resolveLocation(db: DemoDb, body: unknown): number | null {
  const locationId = num(formValue(body, 'LocationId'));
  if (locationId && db.locations.some((l) => l.id === locationId)) return locationId;
  const lat = num(formValue(body, 'Latitude'));
  const lng = num(formValue(body, 'Longitude'));
  if (lat === null || lng === null) return null;
  const id = nextId(db.locations);
  db.locations.push({ id, name: `Fundort (${lat.toFixed(4)}, ${lng.toFixed(4)})`, latitude: lat, longitude: lng, country: null, region: null });
  return id;
}

function resolveTaxonomy(db: DemoDb, body: unknown): number | null {
  const taxonomyId = num(formValue(body, 'TaxonomyId'));
  if (taxonomyId && db.taxonomies.some((t) => t.id === taxonomyId)) return taxonomyId;
  const genus = formValue(body, 'Genus')?.trim();
  const species = formValue(body, 'Species')?.trim();
  if (!genus || !species) return null;
  const id = nextId(db.taxonomies);
  db.taxonomies.push({
    id,
    kingdom: formValue(body, 'Kingdom') || 'Unbekannt',
    phylum: formValue(body, 'Phylum') || 'Unbekannt',
    class: formValue(body, 'Class') || 'Unbekannt',
    orders: formValue(body, 'Orders') || 'Unbekannt',
    family: formValue(body, 'Family') || 'Unbekannt',
    genus,
    species,
    validated: false,
  });
  return id;
}

function distanceKm(lat1: number, lon1: number, lat2: number, lon2: number): number {
  const rad = (d: number) => (d * Math.PI) / 180;
  const a = Math.sin(rad(lat2 - lat1) / 2) ** 2 + Math.cos(rad(lat1)) * Math.cos(rad(lat2)) * Math.sin(rad(lon2 - lon1) / 2) ** 2;
  return 6371 * 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
}

const sizeNumber = (size: string | null) => {
  const n = parseFloat((size ?? '').replace(',', '.').replace(/[^\d.]/g, ''));
  return isNaN(n) ? null : n;
};

// ── Routes ──────────────────────────────────────────────────────────────────
const routes: [string, RegExp, Handler][] = [
  // Auth & users
  ['POST', /^auth\/login$/, ({ req, db }) => {
    const { usernameOrEmail, password } = req.body as { usernameOrEmail: string; password: string };
    const user = db.users.find((u) => u.username === usernameOrEmail || u.email === usernameOrEmail);
    const passwords = JSON.parse(localStorage.getItem('zoolog_demo_passwords') ?? '{}') as Record<string, string>;
    if (!user || password !== (passwords[user.id] ?? DEMO_PASSWORD)) return fail(401, 'Ungültige Anmeldedaten.');
    if (user.status !== 'active') return fail(401, 'Dieser Nutzer ist gesperrt oder inaktiv.');
    return ok({
      message: 'Login erfolgreich',
      token: makeToken(user),
      expiresInMinutes: 480,
      user: { id: user.id, username: user.username, email: user.email, role: user.userRole, status: user.status, job: user.job, description: user.description },
    });
  }],
  ['GET', /^users$/, (ctx) => {
    requireUser(ctx);
    return ok(ctx.db.users.map(({ id, username, email, userRole, status, createdAt, description, job }) => ({ id, username, email, userRole, status, createdAt, description, job })));
  }],
  ['POST', /^users$/, ({ req, db }) => {
    const { username, email, pass } = req.body as { username: string; email: string; pass: string };
    if (!username || username.length < 3) return fail(400, 'Benutzername muss zwischen 3 und 50 Zeichen lang sein.');
    if (!pass || pass.length < 6) return fail(400, 'Passwort muss mindestens 6 Zeichen lang sein.');
    if (db.users.some((u) => u.username === username || u.email === email)) return fail(409, 'Benutzername oder E-Mail wird bereits verwendet.');
    const user: DemoUser = { id: nextId(db.users), username, email, userRole: 'user', status: 'active', createdAt: new Date().toISOString(), description: null, job: null };
    db.users.push(user);
    const passwords = JSON.parse(localStorage.getItem('zoolog_demo_passwords') ?? '{}');
    passwords[user.id] = pass;
    localStorage.setItem('zoolog_demo_passwords', JSON.stringify(passwords));
    return created({ id: user.id, username, email, userRole: user.userRole, status: user.status, createdAt: user.createdAt });
  }],
  ['PUT', /^users\/(\d+)$/, (ctx) => {
    const me = requireUser(ctx);
    const user = ctx.db.users.find((u) => u.id === Number(ctx.params[0]));
    if (!user) return fail(404, 'Nutzer nicht gefunden.');
    if (me.id !== user.id && me.userRole !== 'admin') return fail(403, 'Keine Berechtigung.');
    const body = ctx.req.body as Partial<DemoUser> & { pass?: string };
    if (ctx.db.users.some((u) => u.id !== user.id && (u.username === body.username || u.email === body.email))) {
      return fail(409, 'Benutzername oder E-Mail wird bereits verwendet.');
    }
    Object.assign(user, { username: body.username ?? user.username, email: body.email ?? user.email, description: body.description ?? null, job: body.job ?? null });
    if (me.userRole === 'admin') Object.assign(user, { userRole: body.userRole ?? user.userRole, status: body.status ?? user.status });
    return noContent();
  }],
  ['DELETE', /^users\/(\d+)$/, (ctx) => {
    requireRole(ctx, 'admin');
    const id = Number(ctx.params[0]);
    ctx.db.users = ctx.db.users.filter((u) => u.id !== id);
    return noContent();
  }],

  // Collections
  ['GET', /^collections$/, (ctx) => ok(ctx.db.collections.filter((c) => c.isPublic || c.createdBy === ctx.user?.id).map((c) => collectionView(ctx.db, c)))],
  ['GET', /^collections\/public-stats$/, ({ db }) => ok({ totalCollections: db.collections.filter((c) => c.isPublic).length, totalObjects: db.specimens.length, totalUsers: db.users.length })],
  ['GET', /^collections\/search-public$/, ({ db, query }) => {
    const q = (query.get('query') ?? '').trim().toLowerCase();
    return ok(db.collections
      .filter((c) => c.isPublic && c.name.toLowerCase().includes(q))
      .map((c) => ({ ...collectionView(db, c), specimenCount: db.specimens.filter((s) => s.collectionId === c.id).length })));
  }],
  ['GET', /^collections\/favorites\/user\/(\d+)$/, (ctx) => {
    const me = requireUser(ctx);
    const userId = Number(ctx.params[0]);
    if (me.id !== userId && me.userRole !== 'admin') return fail(403, 'Keine Berechtigung.');
    return ok(ctx.db.favorites.filter((f) => f.userId === userId).map((f) => f.collectionId));
  }],
  ['POST', /^collections\/(\d+)\/favorite$/, (ctx) => {
    const me = requireUser(ctx);
    const collectionId = Number(ctx.params[0]);
    const userId = Number(ctx.req.body);
    if (me.id !== userId && me.userRole !== 'admin') return fail(403, 'Keine Berechtigung.');
    if (ctx.db.favorites.some((f) => f.userId === userId && f.collectionId === collectionId)) return fail(400, 'Sammlung ist bereits als Favorit markiert.');
    ctx.db.favorites.push({ userId, collectionId });
    return ok({ message: 'Favorit erfolgreich hinzugefügt.' });
  }],
  ['DELETE', /^collections\/(\d+)\/favorite\/user\/(\d+)$/, (ctx) => {
    const me = requireUser(ctx);
    const [collectionId, userId] = ctx.params.map(Number);
    if (me.id !== userId && me.userRole !== 'admin') return fail(403, 'Keine Berechtigung.');
    ctx.db.favorites = ctx.db.favorites.filter((f) => !(f.userId === userId && f.collectionId === collectionId));
    return ok({ message: 'Favorit erfolgreich entfernt.' });
  }],
  ['GET', /^collections\/(\d+)$/, (ctx) => {
    const c = ctx.db.collections.find((x) => x.id === Number(ctx.params[0]) && (x.isPublic || x.createdBy === ctx.user?.id));
    return c ? ok(collectionView(ctx.db, c)) : fail(404, 'Sammlung nicht gefunden.');
  }],
  ['POST', /^collections$/, (ctx) => {
    const me = requireUser(ctx);
    const body = ctx.req.body as { name: string; description?: string; isPublic?: boolean };
    if (!body.name?.trim()) return fail(400, 'Name ist erforderlich.');
    const c = { id: nextId(ctx.db.collections), name: body.name.trim(), description: body.description ?? null, isPublic: !!body.isPublic, createdBy: me.id, createdAt: new Date().toISOString() };
    ctx.db.collections.push(c);
    return created(c);
  }],
  ['PUT', /^collections\/(\d+)$/, (ctx) => {
    const me = requireUser(ctx);
    const c = ctx.db.collections.find((x) => x.id === Number(ctx.params[0]));
    if (!c) return fail(404, 'Sammlung nicht gefunden.');
    if (c.createdBy !== me.id && me.userRole !== 'admin' && !(me.userRole === 'moderator' && c.isPublic)) return fail(403, 'Keine Berechtigung.');
    const body = ctx.req.body as { name: string; description?: string };
    c.name = body.name ?? c.name;
    c.description = body.description ?? c.description;
    return noContent();
  }],
  ['DELETE', /^collections\/(\d+)$/, (ctx) => {
    const me = requireUser(ctx);
    const id = Number(ctx.params[0]);
    const c = ctx.db.collections.find((x) => x.id === id);
    if (!c) return fail(404, 'Sammlung mit dieser ID nicht gefunden');
    if (c.createdBy !== me.id && !isPrivileged(me)) return fail(403, 'Keine Berechtigung.');
    const specimenIds = new Set(ctx.db.specimens.filter((s) => s.collectionId === id).map((s) => s.id));
    if (ctx.db.loans.some((l) => specimenIds.has(l.specimenId) && l.status !== 'returned')) {
      return fail(400, 'Die Sammlung kann nicht gelöscht werden, da sich darin Exemplare befinden, die aktuell verliehen sind.');
    }
    ctx.db.loans = ctx.db.loans.filter((l) => !specimenIds.has(l.specimenId));
    ctx.db.specimens = ctx.db.specimens.filter((s) => s.collectionId !== id);
    ctx.db.favorites = ctx.db.favorites.filter((f) => f.collectionId !== id);
    ctx.db.collections = ctx.db.collections.filter((x) => x.id !== id);
    return noContent();
  }],

  // Specimens
  ['GET', /^specimen$/, (ctx) => ok(visibleSpecimens(ctx).map((s) => specimenView(ctx.db, s)))],
  ['GET', /^specimen\/search-public$/, (ctx) => {
    const { db, query } = ctx;
    const like = (value: string | null | undefined, term: string) => (value ?? '').toLowerCase().includes(term.toLowerCase());
    const q = query.get('query')?.trim();
    const filters = { className: query.get('className'), species: query.get('species'), genus: query.get('genus'), family: query.get('family'), order: query.get('order') };
    const minW = num(query.get('minWeight')), maxW = num(query.get('maxWeight'));
    const minS = num(query.get('minSize')), maxS = num(query.get('maxSize'));
    const lat = num(query.get('lat')), lng = num(query.get('lng')), radius = num(query.get('radius'));

    const result = visibleSpecimens(ctx).filter((s) => {
      const t = db.taxonomies.find((x) => x.id === s.taxonomyId);
      const loc = db.locations.find((x) => x.id === s.locationId);
      if (q && ![s.name, s.description, t?.species, t?.genus, t?.family, t?.orders, t?.class].some((v) => like(v, q))) return false;
      if (filters.className && t?.class.toLowerCase() !== filters.className.toLowerCase()) return false;
      if (filters.species && !like(t?.species, filters.species)) return false;
      if (filters.genus && !like(t?.genus, filters.genus)) return false;
      if (filters.family && !like(t?.family, filters.family)) return false;
      if (filters.order && !like(t?.orders, filters.order)) return false;
      if (minW !== null && (s.weight === null || s.weight < minW)) return false;
      if (maxW !== null && (s.weight === null || s.weight > maxW)) return false;
      const size = sizeNumber(s.size);
      if (minS !== null && (size === null || size < minS)) return false;
      if (maxS !== null && (size === null || size > maxS)) return false;
      if (lat !== null && lng !== null && radius !== null && (!loc || distanceKm(lat, lng, loc.latitude, loc.longitude) > radius)) return false;
      return true;
    });

    return ok(result.map((s) => {
      const t = db.taxonomies.find((x) => x.id === s.taxonomyId);
      const loc = db.locations.find((x) => x.id === s.locationId);
      return { ...specimenView(db, s), class: t?.class, species: t?.species, genus: t?.genus, family: t?.family, orders: t?.orders, latitude: loc?.latitude, longitude: loc?.longitude };
    }));
  }],
  ['GET', /^specimen\/export-csv\/(\d+)$/, (ctx) => {
    requireUser(ctx);
    const { db } = ctx;
    const rows = db.specimens.filter((s) => s.collectionId === Number(ctx.params[0])).map((s) => {
      const t = db.taxonomies.find((x) => x.id === s.taxonomyId);
      const l = db.locations.find((x) => x.id === s.locationId);
      return [s.name, t?.species, l?.name, t?.kingdom, t?.phylum, t?.class, t?.orders, t?.family, t?.genus,
        (s.description ?? '').replace(/;/g, ','), s.status, (s.size ?? '').replace(/;/g, ','), s.weight ?? '', s.birthYear ?? '', l?.latitude ?? '', l?.longitude ?? ''].join(';');
    });
    const csv = ['Name;Species;Location;Kingdom;Phylum;Class;Orders;Family;Genus;Description;Status;Size;Weight;BirthYear;Latitude;Longitude', ...rows].join('\n');
    return ok(new Blob([csv], { type: 'text/csv' }));
  }],
  ['POST', /^specimen\/import-csv\/(\d+)$/, async (ctx) => {
    const me = requireUser(ctx);
    const collectionId = Number(ctx.params[0]);
    if (!ctx.db.collections.some((c) => c.id === collectionId)) return fail(404, 'Sammlung nicht gefunden.');
    const file = (ctx.req.body as FormData).get('file');
    if (!(file instanceof File)) return fail(400, 'Keine Datei hochgeladen.');
    const lines = (await file.text()).split(/\r?\n/).slice(1).filter((l) => l.trim());
    let count = 0;
    for (const [i, line] of lines.entries()) {
      const p = line.split(';').map((x) => x.trim());
      if (p.length < 16) return fail(400, `Fehler in Zeile ${i + 2}: Die Zeile enthält nur ${p.length} statt der 16 erforderlichen Spalten.`);
      const [name, species, locationName, kingdom, phylum, cls, orders, family, genus, description, status, size, weight, birthYear, lat, lng] = p;
      let tax = ctx.db.taxonomies.find((t) => t.genus === genus && t.species === species);
      if (!tax) {
        tax = { id: nextId(ctx.db.taxonomies), kingdom, phylum, class: cls, orders, family, genus, species, validated: false };
        ctx.db.taxonomies.push(tax);
      }
      const location = { id: nextId(ctx.db.locations), name: locationName || 'Importierter Fundort', latitude: Number(lat.replace(',', '.')) || 0, longitude: Number(lng.replace(',', '.')) || 0, country: null, region: null };
      ctx.db.locations.push(location);
      ctx.db.specimens.push({
        id: nextId(ctx.db.specimens), name, description, dateCollected: today(),
        status: ['available', 'on loan', 'lost', 'destroyed'].includes(status) ? status : 'available',
        size: size || null, weight: num(weight.replace(',', '.')), birthYear: num(birthYear), photoPath: null,
        locationId: location.id, taxonomyId: tax.id, collectionId, addedBy: me.id, createdAt: new Date().toISOString(),
      });
      count++;
    }
    return ok({ message: `${count} Exemplare erfolgreich importiert.` });
  }],
  ['GET', /^specimen\/(\d+)$/, (ctx) => {
    const s = ctx.db.specimens.find((x) => x.id === Number(ctx.params[0]));
    const col = s && ctx.db.collections.find((c) => c.id === s.collectionId);
    if (!s || !(col?.isPublic || s.addedBy === ctx.user?.id)) return fail(404, 'Objekt nicht gefunden.');
    const loc = ctx.db.locations.find((l) => l.id === s.locationId);
    return ok({ ...s, latitude: loc?.latitude ?? null, longitude: loc?.longitude ?? null });
  }],
  ['POST', /^specimen$/, async (ctx) => {
    const me = requireUser(ctx);
    const body = ctx.req.body;
    const collectionId = num(formValue(body, 'CollectionId'));
    const col = ctx.db.collections.find((c) => c.id === collectionId);
    if (!col) return fail(400, 'Die Sammlung existiert nicht.');
    if (!isPrivileged(me) && col.createdBy !== me.id) return fail(403, 'Keine Berechtigung.');
    const name = formValue(body, 'Name')?.trim();
    if (!name) return fail(400, 'Name ist erforderlich.');
    const taxonomyId = resolveTaxonomy(ctx.db, body);
    if (!taxonomyId) return fail(400, 'TaxonomyId oder manuelle Gattung/Art erforderlich.');
    const locationId = resolveLocation(ctx.db, body);
    if (!locationId) return fail(400, 'LocationId oder Koordinaten erforderlich.');
    const image = body instanceof FormData ? body.get('imageFile') : null;
    const specimen: DemoSpecimen = {
      id: nextId(ctx.db.specimens), name,
      description: formValue(body, 'Description') || null,
      dateCollected: formValue(body, 'DateCollected') || today(),
      status: formValue(body, 'Status') || 'available',
      size: formValue(body, 'Size') || null,
      weight: num(formValue(body, 'Weight')),
      birthYear: num(formValue(body, 'BirthYear')),
      photoPath: image instanceof File ? await compressImage(image) : null,
      locationId, taxonomyId, collectionId: col.id, addedBy: me.id, createdAt: new Date().toISOString(),
    };
    ctx.db.specimens.push(specimen);
    return created(specimen);
  }],
  ['PUT', /^specimen\/(\d+)$/, async (ctx) => {
    const me = requireUser(ctx);
    const s = ctx.db.specimens.find((x) => x.id === Number(ctx.params[0]));
    if (!s) return fail(404, 'Objekt nicht gefunden.');
    if (!isPrivileged(me) && s.addedBy !== me.id) return fail(403, 'Keine Berechtigung.');
    const body = ctx.req.body;
    s.name = formValue(body, 'Name') || s.name;
    s.description = formValue(body, 'Description') ?? s.description;
    s.status = formValue(body, 'Status') || s.status;
    s.dateCollected = formValue(body, 'DateCollected') || s.dateCollected;
    s.size = formValue(body, 'Size') || null;
    s.weight = num(formValue(body, 'Weight'));
    s.birthYear = num(formValue(body, 'BirthYear'));
    const image = body instanceof FormData ? body.get('imageFile') : null;
    if (image instanceof File) s.photoPath = await compressImage(image);
    return ok(s);
  }],
  ['DELETE', /^specimen\/(\d+)$/, (ctx) => {
    const me = requireUser(ctx);
    const id = Number(ctx.params[0]);
    const s = ctx.db.specimens.find((x) => x.id === id);
    if (!s) return fail(404, 'Objekt nicht gefunden.');
    if (!isPrivileged(me) && s.addedBy !== me.id) return fail(403, 'Keine Berechtigung.');
    ctx.db.loans = ctx.db.loans.filter((l) => l.specimenId !== id);
    ctx.db.specimens = ctx.db.specimens.filter((x) => x.id !== id);
    return noContent();
  }],

  // Taxonomy
  ['GET', /^taxonomy$/, ({ db }) => ok(db.taxonomies)],
  ['GET', /^taxonomy\/unvalidated$/, (ctx) => {
    requireRole(ctx, 'admin', 'moderator');
    return ok(ctx.db.taxonomies.filter((t) => !t.validated));
  }],
  ['PUT', /^taxonomy\/(\d+)\/validate$/, (ctx) => {
    requireRole(ctx, 'admin', 'moderator');
    const t = ctx.db.taxonomies.find((x) => x.id === Number(ctx.params[0]));
    if (!t) return fail(404, 'Taxonomie nicht gefunden.');
    t.validated = true;
    return ok({ message: 'Taxonomie erfolgreich validiert.' });
  }],
  ['DELETE', /^taxonomy\/(\d+)\/reject$/, (ctx) => {
    requireRole(ctx, 'admin', 'moderator');
    const id = Number(ctx.params[0]);
    if (!ctx.db.taxonomies.some((t) => t.id === id)) return fail(404, 'Taxonomie nicht gefunden.');
    ctx.db.specimens = ctx.db.specimens.filter((s) => s.taxonomyId !== id);
    ctx.db.taxonomies = ctx.db.taxonomies.filter((t) => t.id !== id);
    return ok({ message: 'Taxonomie abgelehnt und entfernt.' });
  }],
  ['GET', /^taxonomy\/(\d+)$/, (ctx) => {
    const t = ctx.db.taxonomies.find((x) => x.id === Number(ctx.params[0]));
    return t ? ok(t) : fail(404, 'Taxonomie nicht gefunden.');
  }],

  // Locations
  ['GET', /^location$/, ({ db }) => ok(db.locations)],
  ['GET', /^location\/(\d+)$/, (ctx) => {
    const l = ctx.db.locations.find((x) => x.id === Number(ctx.params[0]));
    return l ? ok(l) : fail(404, 'Fundort nicht gefunden.');
  }],

  // Loans
  ['GET', /^loan$/, (ctx) => {
    requireUser(ctx);
    return ok(ctx.db.loans.map((l) => ({ ...l, specimen: ctx.db.specimens.find((s) => s.id === l.specimenId) ?? null })));
  }],
  ['POST', /^loan$/, (ctx) => {
    const me = requireUser(ctx);
    const body = ctx.req.body as Partial<DemoDb['loans'][number]>;
    const s = ctx.db.specimens.find((x) => x.id === body.specimenId);
    if (!s) return fail(400, 'Specimen not found.');
    if (s.status !== 'available') return fail(400, `Specimen is not available (current status: ${s.status}).`);
    s.status = 'on loan';
    const loan = {
      id: nextId(ctx.db.loans), specimenId: s.id, loanedTo: Number(body.loanedTo), loanedFrom: body.loanedFrom ?? me.id,
      loanDate: today(), returnDate: body.returnDate ?? null, status: 'active', notes: body.notes ?? null, fromCollection: s.collectionId,
    };
    ctx.db.loans.push(loan);
    return created(loan);
  }],
  ['PUT', /^loan\/(\d+)\/return$/, (ctx) => {
    requireUser(ctx);
    const loan = ctx.db.loans.find((l) => l.id === Number(ctx.params[0]));
    if (!loan) return fail(404, 'Leihvorgang nicht gefunden.');
    if (loan.status === 'returned') return fail(400, 'This loan has already been returned.');
    loan.status = 'returned';
    loan.returnDate = today();
    const s = ctx.db.specimens.find((x) => x.id === loan.specimenId);
    if (s) s.status = 'available';
    return ok(loan);
  }],
];

function respond(req: HttpRequest<unknown>, path: string, query: URLSearchParams): Observable<HttpResponse<unknown>> {
  const db = loadDemoDb();
  const ctx: Ctx = { req, db, user: userFromRequest(req, db), params: [], query };

  for (const [method, pattern, handler] of routes) {
    const match = method === req.method ? pattern.exec(path) : null;
    if (!match) continue;
    ctx.params = match.slice(1);

    return from(Promise.resolve().then(() => handler(ctx))).pipe(
      switchMap((result) => {
        if (result.status >= 400) return throwError(() => result);
        if (req.method !== 'GET') saveDb(ctx.db);
        return of(new HttpResponse({ status: result.status, body: result.body ?? null, url: req.url }));
      }),
    );
  }

  return throwError(() => fail(404, `Demo-API: ${req.method} /${path} ist nicht verfügbar.`));
}

/** Answers every request to environment.apiUrl in the browser. Registered only in demo builds. */
export const demoBackendInterceptor: HttpInterceptorFn = (req, next) => {
  if (!environment.demo || !req.url.startsWith(environment.apiUrl)) return next(req);

  const url = new URL(req.url, window.location.origin);
  const path = url.pathname.slice(url.pathname.indexOf('/api/') + 5).replace(/\/$/, '');

  return respond(req, path, url.searchParams).pipe(
    delay(LATENCY_MS),
    catchError((e: { status?: number; body?: unknown }) =>
      throwError(() => new HttpErrorResponse({ status: e.status ?? 500, error: e.body ?? e, url: req.url, statusText: 'Demo' })),
    ),
  );
};
