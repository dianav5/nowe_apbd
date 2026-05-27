# Hospital API

Projekt Web API wykonany w ramach zadania APBD.

Aplikacja została przygotowana w ASP.NET Core Web API z użyciem Entity Framework Core oraz podejścia Database First. Baza danych została utworzona na podstawie pliku `create.sql`, a modele oraz `HospitalDbContext` zostały wygenerowane przez scaffold.
## Technologie

- .NET 8
- ASP.NET Core Web API
- Entity Framework Core 8
- SQL Server / LocalDB
- Swagger

## Endpointy

## GET /api/patients

Zwraca listę pacjentów wraz z informacjami o przyjęciach oraz przypisanych łóżkach.

Opcjonalny parametr:

search

Przykład:

GET /api/patients?search=an

Filtrowanie odbywa się po polach `FirstName` oraz `LastName`.


## POST /api/patients/{pesel}/bedassignments

Przypisuje pacjentowi dostępne łóżko danego typu na wskazanym oddziale w podanym przedziale czasu.

Przykładowe body:

{
  "from": "2026-06-01T10:00:00",
  "to": "2026-06-10T10:00:00",
  "bedType": "Standard",
  "ward": "Kardiologia"
}


## Uruchomienie

1. Utworzyć bazę danych `HospitalDb`.
2. Uruchomić skrypt `create.sql`.
3. Sprawdzić connection string w `appsettings.json`.
4. Uruchomić projekt komendą:

dotnet run

Swagger dostępny jest pod adresem:

http://localhost:5088/swagger


## Informacje dodatkowe

Projekt korzysta z lokalnej bazy danych SQL Server LocalDB.

Przykładowy connection string:

Server=(localdb)\MSSQLLocalDB;Database=HospitalDb;Trusted_Connection=True;TrustServerCertificate=True

Foldery `bin` i `obj` nie są dodawane do repozytorium, ponieważ są generowane automatycznie podczas budowania projektu.
