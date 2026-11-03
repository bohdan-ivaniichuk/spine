# Research: Activity and streak

## R1 — Where to convert answers to local dates

- **Decision**: in .NET, with `TimeZoneInfo.FindSystemTimeZoneById(ianaId)` and
  `TimeZoneInfo.ConvertTime(answeredAt, zone)`, then `DateOnly.FromDateTime(local.DateTime)`.
- **Rationale**: the validator and the conversion use the same zone database, so a zone that passes
  validation always converts; the calculator stays a pure function of dates; ~7k timestamps a year are
  trivial to convert.
- **Alternatives**: `GROUP BY (answered_at AT TIME ZONE @tz)::date` in PostgreSQL — fewer rows, but a second
  zone database (PostgreSQL's) that can disagree with .NET's validation, and the math moves into SQL where
  it is only integration-testable.

## R2 — Time-zone data in the API container

- **Finding**: the runtime image is `mcr.microsoft.com/dotnet/aspnet:10.0-alpine`; `/usr/share/zoneinfo`
  does not exist there (checked in the running `api` container on 2026-11-03). On Linux .NET reads IANA
  zones from that directory, so every zone but UTC would be "unknown" in Docker. macOS and the ubuntu
  GitHub runner have the data, so tests would not catch it.
- **Decision**: `apk add --no-cache tzdata` in the runtime stage of `src/Api/Dockerfile`; the quickstart
  checks `?tz=Europe/Kyiv` against the running container.
- **Note**: `InvariantGlobalization=true` does not affect `TimeZoneInfo`, which reads tzdata files directly.

## R3 — Streak rules

- **Decision**: active day = a local date with ≥ 1 answer. Current streak = length of the run of active
  days ending today if today is active, else ending yesterday if yesterday is active, else 0. Longest =
  longest run over all active days (≥ current). Days after today are ignored (clock skew, D5).
- **Rationale**: matches the spec scenarios and the common "don't break the streak before the day is over"
  convention.

## R4 — Browser time zone

- **Decision**: `Intl.DateTimeFormat().resolvedOptions().timeZone`, sent as `?tz=`. Browsers report IANA ids
  (sometimes legacy aliases such as `Europe/Kiev`); tzdata keeps the aliases as links, so both resolve.
- **Fallback**: if the browser returns nothing, the client omits `tz` and the API uses UTC.
