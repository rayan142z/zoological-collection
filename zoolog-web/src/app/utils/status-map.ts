// Das Backend speichert Status-Werte auf Englisch
// Die Oberfläche zeigt weiterhin Deutsch - diese Maps übersetzen
// an der Grenze zwischen Angular und der API, ohne Datenbank oder UI zu ändern.
export const STATUS_DE_TO_EN: Record<string, string> = {
  'verfügbar': 'available',
  'ausgeliehen': 'on loan',
  'verloren': 'lost',
  'zerstört': 'destroyed',
};

export const STATUS_EN_TO_DE: Record<string, string> = {
  'available': 'verfügbar',
  'on loan': 'ausgeliehen',
  'lost': 'verloren',
  'destroyed': 'zerstört',
};