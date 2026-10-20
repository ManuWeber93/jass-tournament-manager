# Tournament Creation

## Implemented backend behavior

- An authenticated user can create a tournament. The organizer is always the current user and must exist in persistence; clients cannot assign a different organizer.
- The request supplies a name, optional location, date, and explicit tournament configuration. Existing tournament templates can supply these values in a future client, but creating a tournament does not modify or remain linked to a template.
- The domain factory creates all configured rounds, numbered consecutively starting at one, with `Pending` status. The tournament uses the existing `Active` default status.
- The organizer is automatically registered as a participant with role `Organizer`, registration method `ByOrganizer`, and `IsPlaying = true`.
- Pairings, other participants, and games are not created. Games belong to pairings and should be created when those pairings are established.
- Tournament creation persists the tournament, organizer participation, and rounds together through one successful unit-of-work save.
- The backend generates a participation code. It checks existing codes and retries database unique-index conflicts, including concurrent allocation, with at most five candidates. Exhaustion returns a conflict response; unrelated persistence failures are not disguised as code collisions.

## API

### `POST /api/tournaments`

Requires authentication. Request properties:

- `name`: required, nonblank, maximum 200 characters; trimmed by the domain.
- `location`: optional, maximum 200 characters; trimmed by the domain.
- `date`: nondefault `DateOnly` value. Historical tournaments are permitted.
- `config`: required, using the shared `TournamentConfigDto`. Round counts must be between 1 and 50 inclusive, game counts must be positive, and score visibility must be a defined enum value.

Returns `201 Created` with a `Location` header referencing `GET /api/tournaments/{id}`. The response includes tournament details, participation code, configuration, status, ordered round summaries, and timestamps.

### `GET /api/tournaments/{id}`

Requires authentication. Access is allowed to the organizer, registered tournament participants, and system administrators. Other users receive `403 Forbidden`; unknown tournament IDs receive `404 Not Found`.

## Deferred

- MAUI creation form, template prepopulation, and navigation.
- Tournament listing and updates.
- Participant registration, table allocation, pairing creation/draws, and game initialization.