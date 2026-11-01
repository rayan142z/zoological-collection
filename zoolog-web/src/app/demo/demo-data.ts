// Sample data for the public demo build (environment.demo === true).
// Shapes mirror the JSON returned by the ASP.NET backend (camelCase).

export interface DemoUser {
  id: number;
  username: string;
  email: string;
  userRole: 'user' | 'moderator' | 'admin';
  status: string;
  createdAt: string;
  description: string | null;
  job: string | null;
}

export interface DemoTaxonomy {
  id: number;
  kingdom: string;
  phylum: string;
  class: string;
  orders: string;
  family: string;
  genus: string;
  species: string;
  validated: boolean;
}

export interface DemoLocation {
  id: number;
  name: string;
  latitude: number;
  longitude: number;
  country: string | null;
  region: string | null;
}

export interface DemoCollection {
  id: number;
  name: string;
  description: string | null;
  isPublic: boolean;
  createdBy: number;
  createdAt: string;
}

export interface DemoSpecimen {
  id: number;
  name: string;
  description: string | null;
  dateCollected: string;
  status: string;
  size: string | null;
  weight: number | null;
  birthYear: number | null;
  photoPath: string | null;
  locationId: number;
  taxonomyId: number;
  collectionId: number;
  addedBy: number;
  createdAt: string;
}

export interface DemoLoan {
  id: number;
  specimenId: number;
  loanedTo: number;
  loanedFrom: number;
  loanDate: string;
  returnDate: string | null;
  status: string;
  notes: string | null;
  fromCollection: number;
}

export interface DemoDb {
  users: DemoUser[];
  taxonomies: DemoTaxonomy[];
  locations: DemoLocation[];
  collections: DemoCollection[];
  specimens: DemoSpecimen[];
  loans: DemoLoan[];
  favorites: { userId: number; collectionId: number }[];
}

/** Password accepted for every demo account. */
export const DEMO_PASSWORD = 'Demo123!';

/** Accounts offered as one-click logins on the login page. */
export const DEMO_ACCOUNTS = [
  { username: 'besucher', role: 'user' },
  { username: 'kuratorin', role: 'moderator' },
  { username: 'admin', role: 'admin' },
] as const;

const users: DemoUser[] = [
  { id: 1, username: 'admin', email: 'admin@zoolog.demo', userRole: 'admin', status: 'active', createdAt: '2026-03-02T09:00:00', description: 'Verwaltet Nutzer und Sammlungen des Portals.', job: 'Administrator' },
  { id: 2, username: 'kuratorin', email: 'kuratorin@zoolog.demo', userRole: 'moderator', status: 'active', createdAt: '2026-03-05T10:30:00', description: 'Prüft neue Taxonomie-Einträge und betreut die Vogel- und Säugetiersammlung.', job: 'Kuratorin' },
  { id: 3, username: 'besucher', email: 'besucher@zoolog.demo', userRole: 'user', status: 'active', createdAt: '2026-04-11T14:15:00', description: 'Hobby-Entomologe aus dem Siegerland.', job: 'Student' },
  { id: 4, username: 'museum_bonn', email: 'leihverkehr@museum.demo', userRole: 'user', status: 'active', createdAt: '2026-04-20T08:45:00', description: 'Leihverkehr eines Partnermuseums.', job: 'Museum' },
  { id: 5, username: 'feldforschung', email: 'feld@zoolog.demo', userRole: 'user', status: 'active', createdAt: '2026-05-03T16:00:00', description: 'Arbeitsgruppe Feldökologie.', job: 'Biologin' },
];

type T = [string, string, string, string, string, string, string, boolean];
const taxonomyRows: T[] = [
  ['Animalia', 'Arthropoda', 'Insecta', 'Coleoptera', 'Lucanidae', 'Lucanus', 'cervus', true],
  ['Animalia', 'Arthropoda', 'Insecta', 'Lepidoptera', 'Papilionidae', 'Papilio', 'machaon', true],
  ['Animalia', 'Arthropoda', 'Insecta', 'Coleoptera', 'Carabidae', 'Carabus', 'auratus', true],
  ['Animalia', 'Arthropoda', 'Insecta', 'Lepidoptera', 'Nymphalidae', 'Aglais', 'io', true],
  ['Animalia', 'Arthropoda', 'Insecta', 'Odonata', 'Aeshnidae', 'Anax', 'imperator', true],
  ['Animalia', 'Arthropoda', 'Insecta', 'Hymenoptera', 'Apidae', 'Bombus', 'terrestris', true],
  ['Animalia', 'Arthropoda', 'Insecta', 'Coleoptera', 'Coccinellidae', 'Coccinella', 'septempunctata', true],
  ['Animalia', 'Arthropoda', 'Insecta', 'Orthoptera', 'Tettigoniidae', 'Tettigonia', 'viridissima', true],
  ['Animalia', 'Chordata', 'Aves', 'Passeriformes', 'Paridae', 'Parus', 'major', true],
  ['Animalia', 'Chordata', 'Aves', 'Passeriformes', 'Phylloscopidae', 'Phylloscopus', 'collybita', true],
  ['Animalia', 'Chordata', 'Aves', 'Piciformes', 'Picidae', 'Dendrocopos', 'major', true],
  ['Animalia', 'Chordata', 'Aves', 'Strigiformes', 'Strigidae', 'Strix', 'aluco', true],
  ['Animalia', 'Chordata', 'Aves', 'Coraciiformes', 'Alcedinidae', 'Alcedo', 'atthis', true],
  ['Animalia', 'Chordata', 'Aves', 'Passeriformes', 'Muscicapidae', 'Erithacus', 'rubecula', true],
  ['Animalia', 'Mollusca', 'Gastropoda', 'Stylommatophora', 'Helicidae', 'Helix', 'pomatia', true],
  ['Animalia', 'Mollusca', 'Gastropoda', 'Stylommatophora', 'Helicidae', 'Cepaea', 'nemoralis', true],
  ['Animalia', 'Mollusca', 'Bivalvia', 'Unionida', 'Unionidae', 'Unio', 'pictorum', true],
  ['Animalia', 'Mollusca', 'Gastropoda', 'Stylommatophora', 'Arionidae', 'Arion', 'vulgaris', true],
  ['Animalia', 'Chordata', 'Reptilia', 'Squamata', 'Colubridae', 'Natrix', 'tessellata', true],
  ['Animalia', 'Chordata', 'Reptilia', 'Squamata', 'Lacertidae', 'Lacerta', 'agilis', true],
  ['Animalia', 'Chordata', 'Reptilia', 'Squamata', 'Anguidae', 'Anguis', 'fragilis', true],
  ['Animalia', 'Chordata', 'Amphibia', 'Anura', 'Bufonidae', 'Bufo', 'bufo', true],
  ['Animalia', 'Chordata', 'Amphibia', 'Caudata', 'Salamandridae', 'Salamandra', 'salamandra', true],
  ['Animalia', 'Chordata', 'Mammalia', 'Rodentia', 'Muridae', 'Apodemus', 'sylvaticus', true],
  ['Animalia', 'Chordata', 'Mammalia', 'Eulipotyphla', 'Erinaceidae', 'Erinaceus', 'europaeus', true],
  ['Animalia', 'Chordata', 'Mammalia', 'Chiroptera', 'Vespertilionidae', 'Pipistrellus', 'pipistrellus', true],
  ['Animalia', 'Chordata', 'Mammalia', 'Rodentia', 'Sciuridae', 'Sciurus', 'vulgaris', true],
  ['Animalia', 'Chordata', 'Mammalia', 'Carnivora', 'Mustelidae', 'Mustela', 'erminea', true],
  // Pending review: visible in the moderators' "Anträge" queue
  ['Animalia', 'Arthropoda', 'Insecta', 'Coleoptera', 'Cerambycidae', 'Rosalia', 'alpina', false],
  ['Animalia', 'Arthropoda', 'Insecta', 'Lepidoptera', 'Sphingidae', 'Macroglossum', 'stellatarum', false],
];

const taxonomies: DemoTaxonomy[] = taxonomyRows.map(([kingdom, phylum, cls, orders, family, genus, species, validated], i) => ({
  id: i + 1, kingdom, phylum, class: cls, orders, family, genus, species, validated,
}));

type L = [string, number, number, string];
const locationRows: L[] = [
  ['Siegen, Fischbacherberg', 50.8748, 8.0243, 'Nordrhein-Westfalen'],
  ['Kahler Asten, Rothaargebirge', 51.1804, 8.4891, 'Nordrhein-Westfalen'],
  ['Bonn, Rheinaue', 50.7152, 7.1531, 'Nordrhein-Westfalen'],
  ['Lahntal bei Bad Ems', 50.3346, 7.7138, 'Rheinland-Pfalz'],
  ['Vulkaneifel, Daun', 50.1985, 6.8302, 'Rheinland-Pfalz'],
  ['Kaiserstuhl', 48.1033, 7.6681, 'Baden-Württemberg'],
  ['Lüneburger Heide', 53.1704, 9.9333, 'Niedersachsen'],
  ['Bodensee, Mettnau', 47.7310, 8.9952, 'Baden-Württemberg'],
  ['Harz, Brocken', 51.7991, 10.6156, 'Sachsen-Anhalt'],
  ['Spreewald, Lübbenau', 51.8667, 13.9667, 'Brandenburg'],
  ['Berlin, Tiergarten', 52.5145, 13.3501, 'Berlin'],
  ['Nationalpark Hainich', 51.0820, 10.4310, 'Thüringen'],
  ['Müritz-Nationalpark', 53.4170, 12.7420, 'Mecklenburg-Vorpommern'],
  ['Schwarzwald, Feldberg', 47.8740, 8.0040, 'Baden-Württemberg'],
];

const locations: DemoLocation[] = locationRows.map(([name, latitude, longitude, region], i) => ({
  id: i + 1, name, latitude, longitude, country: 'Deutschland', region,
}));

const collections: DemoCollection[] = [
  { id: 1, name: 'Insekten Mitteleuropas', description: 'Käfer, Schmetterlinge und Libellen aus heimischen Lebensräumen – präpariert und genadelt.', isPublic: true, createdBy: 3, createdAt: '2026-04-12T10:00:00' },
  { id: 2, name: 'Heimische Vögel', description: 'Bälge und Präparate einheimischer Singvögel, Spechte und Eulen.', isPublic: true, createdBy: 2, createdAt: '2026-03-08T09:30:00' },
  { id: 3, name: 'Mollusken & Schnecken', description: 'Gehäuse und Schalen von Land- und Süßwassermollusken.', isPublic: true, createdBy: 3, createdAt: '2026-05-01T15:20:00' },
  { id: 4, name: 'Reptilien & Amphibien', description: 'Alkoholpräparate und Abgüsse heimischer Herpetofauna.', isPublic: true, createdBy: 5, createdAt: '2026-05-04T11:00:00' },
  { id: 5, name: 'Kleinsäuger', description: 'Schädel, Bälge und Präparate kleiner Säugetiere aus Wald und Feld.', isPublic: true, createdBy: 2, createdAt: '2026-03-15T13:45:00' },
  { id: 6, name: 'Feldnotizen 2026 (privat)', description: 'Private Arbeitssammlung – nur für den Ersteller sichtbar.', isPublic: false, createdBy: 3, createdAt: '2026-06-20T18:10:00' },
];

// name, description, date, status, size, weight (g, stored as kg like the backend), birthYear, photo, locationId, taxonomyId, collectionId, addedBy
type S = [string, string, string, string, string | null, number | null, number | null, string | null, number, number, number, number];
const specimenRows: S[] = [
  // 1 Insekten (besucher)
  ['Hirschkäfer ♂', 'Großes Männchen mit voll ausgebildeten Mandibeln, Totfund an einer Eiche.', '2025-06-21', 'available', '7.2 cm', 4.1, 2025, 'demo/hirschkaefer.webp', 1, 1, 1, 3],
  ['Schwalbenschwanz', 'Frisch geschlüpfter Falter, gespannt. Flügelspannweite 8 cm.', '2025-07-14', 'on loan', '8 cm', 0.6, 2025, 'demo/schwalbenschwanz_schmetterling.webp', 6, 2, 1, 3],
  ['Goldlaufkäfer', 'Metallisch grün glänzender Laufkäfer von einem Feldrand.', '2025-05-02', 'available', '2.5 cm', 0.9, null, null, 3, 3, 1, 3],
  ['Tagpfauenauge', 'Überwinterndes Exemplar aus einem Dachboden.', '2025-03-18', 'available', '5.5 cm', 0.4, 2024, null, 1, 4, 1, 3],
  ['Große Königslibelle', 'Weibchen bei der Eiablage beobachtet, später als Totfund gesammelt.', '2025-08-03', 'available', '7.8 cm', 1.1, 2025, null, 8, 5, 1, 3],
  ['Dunkle Erdhummel (Königin)', 'Frühjahrskönigin, Totfund im Garten.', '2025-04-09', 'lost', '2.2 cm', 0.8, 2025, null, 1, 6, 1, 3],
  ['Siebenpunkt-Marienkäfer', 'Serie von fünf Tieren auf einem Brennnesselbestand.', '2025-06-01', 'available', '0.7 cm', 0.05, null, null, 3, 7, 1, 3],
  ['Grünes Heupferd', 'Weibchen mit Legebohrer, Wiese am Waldrand.', '2025-08-27', 'available', '4 cm', 1.9, 2025, null, 2, 8, 1, 3],
  ['Alpenbock', 'Seltener Bockkäfer an Buchentotholz – Bestimmung noch nicht bestätigt.', '2025-07-30', 'available', '3.5 cm', 1.2, 2025, null, 14, 29, 1, 3],
  // 2 Vögel (kuratorin)
  ['Kohlmeise', 'Altvogel, Fensteranflug. Als Balg präpariert.', '2025-01-12', 'available', '14 cm', 18, 2023, 'demo/kohlmeise.webp', 1, 9, 2, 2],
  ['Zilpzalp', 'Durchzügler, Totfund im Frühjahr.', '2025-04-02', 'available', '11 cm', 8, 2024, null, 3, 10, 2, 2],
  ['Buntspecht ♀', 'Weibchen ohne roten Nackenfleck, Verkehrsopfer.', '2024-11-20', 'on loan', '23 cm', 82, 2022, null, 12, 11, 2, 2],
  ['Waldkauz', 'Ganzkörperpräparat in Ruhehaltung.', '2024-02-08', 'available', '40 cm', 480, 2019, null, 9, 12, 2, 2],
  ['Eisvogel', 'Prachtvolles Männchen, Präparat auf Ast montiert.', '2023-09-15', 'available', '17 cm', 40, 2022, null, 10, 13, 2, 2],
  ['Rotkehlchen', 'Jungvogel mit geflecktem Gefieder.', '2025-07-07', 'destroyed', '13 cm', 16, 2025, null, 11, 14, 2, 2],
  // 3 Mollusken (besucher)
  ['Weinbergschnecke', 'Großes, vollständig erhaltenes Gehäuse von einem Kalkmagerrasen.', '2025-05-24', 'available', '4.5 cm', 22, null, 'demo/schneckenhaus.webp', 5, 15, 3, 3],
  ['Hain-Bänderschnecke (gelb)', 'Gelbe Form mit fünf dunklen Bändern.', '2025-06-10', 'available', '2.3 cm', 2.5, null, null, 1, 16, 3, 3],
  ['Hain-Bänderschnecke (rosa)', 'Rosa Grundfarbe, bänderlos.', '2025-06-10', 'available', '2.2 cm', 2.4, null, null, 1, 16, 3, 3],
  ['Malermuschel', 'Doppelklappige Schale aus der Lahn.', '2024-08-30', 'available', '9 cm', 31, null, null, 4, 17, 3, 3],
  ['Spanische Wegschnecke', 'Alkoholpräparat eines adulten Tieres.', '2025-09-02', 'available', '11 cm', 14, 2025, null, 1, 18, 3, 3],
  // 4 Reptilien & Amphibien (feldforschung)
  ['Würfelnatter', 'Adultes Weibchen, Häutungsrest und Fotodokumentation von der Lahn.', '2024-07-19', 'available', '78 cm', 210, 2018, 'demo/schlange.webp', 4, 19, 4, 5],
  ['Zauneidechse ♂', 'Männchen in grüner Paarungsfärbung.', '2025-05-15', 'available', '19 cm', 14, 2022, null, 6, 20, 4, 5],
  ['Blindschleiche', 'Totfund auf einem Waldweg.', '2025-04-29', 'on loan', '38 cm', 22, 2020, null, 2, 21, 4, 5],
  ['Erdkröte', 'Laichwanderung, Verkehrsopfer an der Amphibienschutzanlage.', '2025-03-12', 'available', '9 cm', 55, 2021, null, 5, 22, 4, 5],
  ['Feuersalamander', 'Larve und adultes Tier als Alkoholpräparat.', '2024-10-05', 'available', '17 cm', 32, 2019, null, 1, 23, 4, 5],
  // 5 Kleinsäuger (kuratorin)
  ['Waldmaus', 'Adultes Tier, Ganzkörperpräparat.', '2025-02-14', 'available', '9 cm', 24, 2024, 'demo/maus.webp', 12, 24, 5, 2],
  ['Igel', 'Schädel und Stachelkleid eines Verkehrsopfers.', '2024-09-21', 'available', '26 cm', 900, 2021, null, 11, 25, 5, 2],
  ['Zwergfledermaus', 'Totfund unter einem Quartier, getrocknet.', '2025-08-11', 'available', '4.5 cm', 5, 2024, null, 13, 26, 5, 2],
  ['Eichhörnchen', 'Rotbraune Form, montiert auf Kiefernast.', '2023-12-03', 'available', '22 cm', 310, 2021, null, 11, 27, 5, 2],
  ['Hermelin (Winterkleid)', 'Weißes Winterfell mit schwarzer Schwanzspitze.', '2024-01-26', 'available', '25 cm', 210, 2022, null, 7, 28, 5, 2],
  // 6 Privat (besucher)
  ['Taubenschwänzchen', 'Schwärmer im Schwirrflug fotografiert, Totfund am Balkon – Bestimmung angefragt.', '2026-06-18', 'available', '4.5 cm', 0.3, 2026, null, 1, 30, 6, 3],
  ['Hirschkäfer ♀ (Fragment)', 'Elytren und Kopfkapsel, für Vergleichszwecke.', '2026-06-19', 'available', '4 cm', 1.5, null, null, 1, 1, 6, 3],
];

const specimens: DemoSpecimen[] = specimenRows.map(
  ([name, description, dateCollected, status, size, weight, birthYear, photoPath, locationId, taxonomyId, collectionId, addedBy], i) => ({
    id: i + 1, name, description, dateCollected, status, size, weight: weight === null ? null : weight / 1000, birthYear, photoPath,
    locationId, taxonomyId, collectionId, addedBy, createdAt: `${dateCollected}T12:00:00`,
  }),
);

/** Days relative to today, as yyyy-mm-dd – keeps "overdue" loans overdue whenever the demo is opened. */
function daysFromToday(days: number): string {
  const d = new Date();
  d.setDate(d.getDate() + days);
  return d.toISOString().split('T')[0];
}

function loans(): DemoLoan[] {
  return [
    { id: 1, specimenId: 2, loanedFrom: 3, loanedTo: 4, loanDate: daysFromToday(-20), returnDate: daysFromToday(25), status: 'active', notes: 'Sonderausstellung „Sommerfalter“.', fromCollection: 1 },
    { id: 2, specimenId: 12, loanedFrom: 2, loanedTo: 3, loanDate: daysFromToday(-40), returnDate: daysFromToday(-5), status: 'active', notes: 'Für eine Bestimmungsübung – Rückgabe überfällig.', fromCollection: 2 },
    { id: 3, specimenId: 23, loanedFrom: 5, loanedTo: 2, loanDate: daysFromToday(-10), returnDate: daysFromToday(14), status: 'active', notes: null, fromCollection: 4 },
    { id: 4, specimenId: 13, loanedFrom: 2, loanedTo: 4, loanDate: daysFromToday(-90), returnDate: daysFromToday(-60), status: 'returned', notes: 'Zurückgegeben in gutem Zustand.', fromCollection: 2 },
  ];
}

/** Fresh copy of the seed data (also used by "Demo zurücksetzen"). */
export function createSeed(): DemoDb {
  return structuredClone({
    users,
    taxonomies,
    locations,
    collections,
    specimens,
    loans: loans(),
    favorites: [
      { userId: 3, collectionId: 2 },
      { userId: 3, collectionId: 5 },
      { userId: 2, collectionId: 1 },
    ],
  });
}
