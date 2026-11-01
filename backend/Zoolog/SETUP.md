# Zoolog Backend Setup

Diese Datei erklärt kurz, wie man das Backend lokal einrichtet, nachdem man das Repository geklont oder gepullt hat.

## 1. Lokale Datenbank-Konfiguration

Zugangsdaten stehen **nie** in Git. `appsettings.Development.json` enthält nur einen leeren Connection String.
Setze die echte Verbindung lokal mit user-secrets (im Ordner `backend/Zoolog`):

```
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=DEIN_SERVER;Database=DEINE_DATENBANK;User Id=DEIN_USER;Password=DEIN_PASSWORT;TrustServerCertificate=True;"
```

Auf einem Server alternativ als Umgebungsvariable: `ConnectionStrings__DefaultConnection`.

## 2. JWT-Konfiguration

Für den Login benutzen wir JWT-Tokens.

Der echte JWT-Key wird lokal mit user-secrets gespeichert und darf nicht in Git stehen.

Gehe zuerst in den Backend-Ordner:

```
cd backend/Zoolog
```

Dann user-secrets einmal initialisieren:

```
dotnet user-secrets init
```

Danach die JWT-Werte setzen:

```
dotnet user-secrets set "Jwt:Key" "ersetze-das-mit-einem-langen-lokalen-development-key"
dotnet user-secrets set "Jwt:Issuer" "Zoolog"
dotnet user-secrets set "Jwt:Audience" "ZoologClient"
```

Jedes Teammitglied kann lokal einen eigenen Key benutzen. Ohne Key erzeugt das Backend im Development-Modus bei jedem Start einen zufälligen Key (Logins verfallen dann beim Neustart). In Produktion ist `Jwt__Key` als Umgebungsvariable Pflicht.

Wichtig ist nur, dass der Key lang genug ist.

## 3. Backend bauen

Im Ordner backend/Zoolog:

```
dotnet restore
dotnet build
```

## 4. Backend starten

Im Ordner backend/Zoolog:

```
dotnet run --urls "http://localhost:5227"
```

Das Backend läuft dann hier:

```
http://localhost:5227
```

## 5. Login testen

Wenn die Demo-Daten in der Datenbank existieren, kann man den Login so testen.

Für Git Bash:

```
curl -X POST http://localhost:5227/api/auth/login -H 'Content-Type: application/json' -d '{"usernameOrEmail":"demo_user","password":"Demo123!"}'
```

Wichtig: In Git Bash muss die JSON mit einfachen Anführungszeichen geschrieben werden, weil das Ausrufezeichen in Demo123! sonst als Bash-History interpretiert werden kann.

Wenn alles funktioniert, sollte die Antwort einen JWT-Token enthalten.

## 6. Gemeinsame Uni-Datenbank und Demo-Daten

Wir benutzen im Team die gemeinsame Uni-Datenbank Group6.

Die Demo-Daten wurden schon einmal in dieser gemeinsamen Datenbank angelegt. Deshalb müssen andere Teammitglieder die Demo-Daten normalerweise nicht nochmal einfügen.

Wenn man mit der gleichen Uni-Datenbank verbunden ist, sollten diese Testdaten schon sichtbar sein:

```
demo_admin
demo_moderator
demo_user
Demo Public Collection
Demo Monarch Butterfly
```

Der wichtigste Login-Testnutzer ist:

```
Benutzer: demo_user
Passwort: Demo123!
```

Wichtig: Die Demo-User wurden über die API erstellt, damit die Passwörter richtig mit BCrypt gehasht werden.

Die SQL-Datei für die Demo-Daten liegt trotzdem im Repository, damit nachvollziehbar ist, welche Testdaten eingefügt wurden:

```
backend/Zoolog/Sql/2026-06-17_step2_seed_demo_data.sql
```

Diese Datei ist eher zur Dokumentation und für den Fall, dass man später eine neue oder lokale Datenbank aufsetzen muss.

Für die normale Arbeit mit der gemeinsamen Uni-Datenbank muss man sie nicht nochmal ausführen.
