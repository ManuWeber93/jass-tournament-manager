# Jass Tournament Manager - Requirements & Context

## Overview
Web application for Jass tournament organizers to manage Schieber tournaments.

## Types of Users

### System Administrator (SYSADMIN)
- Can view and manage all tournaments from all organizers
- For support purposes
- Full access to all data and functions
- Can manage organizers and players

### Organizers
- Can register themselves
- Create and manage their own tournaments
- See only their own tournaments (logical separation, except SYSADMIN)
- Can register participants manually or invite via QR code
- Can set pairings manually or run automatic draws
- Have extended permissions (tournament management, configuration)
- Can run reports across all their tournaments
- Can import past tournament data via Excel

### Players
- Can log in
- Can enter scores for rounds
- Can view scores (if allowed by the organizer)
- See all tournaments they participated in
- Can view leaderboards for an organizer's tournaments
- Can join tournaments via code
- Planned: can select an existing user during first login/registration, so that they can reuse a user that was created during an import; this flow is not yet available in the app

## Jass Variant: Schieber

- Teams: 2 players per team (2 vs 2)
- Declarations: a team declares trump
- Declarations per player: typically 2 per game (configurable)

## Tournament Structure

### Hierarchy

Tournament
  └── Rounds (default: 5 rounds, configurable)
        └── Pairings (4 players per pairing: 2 vs 2)
                └── Games (default: 8 games per pairing per round, configurable)
              

### Tournaments
- Belong to an organizer
- Can be recurring events
- Reports across all tournaments of an organizer
- Number of rounds configurable (default: 5)
- Number of games per round configurable (default: 8)
- Code for participation can be generated (maybe QR code to scan)
- Match bonus configurable (on/off)

### Rounds
- A pairing of a round consists of multiple games (default: 8)
- New pairings can be defined per round

### Games
- A game = one match between two pairs (2 vs 2)
- Total 157 points per game
- Match bonus: +100 points when a team takes all points, if match bonus is enabled for the tournament
- Existing games follow the current tournament match-bonus setting when the tournament configuration is changed
- Only one team needs to enter points; the other is calculated as (157 - entered points)

### Pairings
- Normal mode: players play individually and pairings change per round
- Alternative: fixed teams for the whole tournament
- Input: manual assignment or automatic draw
- A pairing must have exactly 4 participants (2 per team)
- A pairing must have exactly the configured number of games before it can be completed
- A pairing can only be completed when all its games are completed

### Tournament Participants
- Organizers can register participants manually OR
- Players can add themselves via code
- Same players can participate in different tournaments
- Player identification via email address (for Excel export/matching)

## Features

### Authentication & Sessions
- Users can register with automatic sign-in, log in with an existing account, and log out.
- Detailed business rules, error cases and implemented versus deferred app behavior are maintained in [Authentication](features/authentication.md).
- Token handling, refresh and revocation are described in the [architecture documentation](architecture.md#app-authentication-and-session-handling).

### Pairing Management
- Organizers can assign pairings manually
- Organizers can run automatic pairing draws
- Players can confirm or enter their assigned partner themselves

### Score Management
- Players can enter scores
- Scores can be hidden during the tournament (for suspense)
- Score visibility can be toggled by the organizer

### Leaderboards
- Players can view leaderboards for tournaments
- Reports across all tournaments of an organizer
- Future version: Organizers can exclude selected tournaments from their organizer-wide all-time leaderboard ("ewige Rangliste") without removing those tournaments or their tournament-specific leaderboards.

### Excel Import
- Primary purpose: import historical tournament data
- Available for: Organizers (for their own tournaments)
- Import includes:
  - Tournament metadata
  - Participant data
  - Game results
  - Pairings
- Player matching via email address
- Details to be specified later
- Enables migration of historical data into the system

## Privacy & Isolation
- SYSADMIN users can see and manage all tournaments.
- Organizers can see tournaments they organize.
- Players can see tournaments in which they are registered as participants.
- A user can therefore see a tournament if they are either the organizer or a participant.
- Tournaments of different organizers are logically separated, except for SYSADMIN access.
- Refresh token plaintext values are not stored in the database; only token hashes are persisted.

## Configurable Parameters (per tournament)
- Number of rounds (default: 5)
- Number of games per round (default: 8)
- Match bonus enabled/disabled (default: enabled, +100 points; changes apply to existing games)
- Score visibility to players during the tournament
- Score visibility to players after the tournament
- Maybe in a future version: Pairing mode (rotating pairings vs fixed teams, default: rotating)

## Technical Requirements
- Code generation for tournament participation
- Excel import functionality for historical player and result data
- Manual pairing entry
- Automatic score calculation (157 - entered points)
- Match bonus calculation (+100 when applicable)
- Maybe in a future version: Automatic pairing draw for games
