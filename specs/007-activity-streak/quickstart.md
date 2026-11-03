# Quickstart: Activity and streak

Prerequisites: the stack is running (`docker compose up --build -d`) with content imported.

1. **SC-001, API in two zones**
   `curl 'http://localhost:8080/api/review/activity?tz=Europe/Kyiv'` and `…?tz=America/Los_Angeles`:
   14 days ending at each zone's today; streaks match a hand count over `attempts.answered_at`.
2. **Container zone data (R2)**: `curl -i 'http://localhost:8080/api/review/activity?tz=Europe/Kyiv'`
   returns 200, not 400 — `tzdata` is in the image. `…?tz=Mars/Base` returns 400 problem details; no `tz`
   returns `"timeZone": "UTC"`.
3. **US1**: open `http://localhost`; the overview card shows the current and longest streak; with no answer
   today the "answer today to keep it" hint shows; answer one question and reopen — `activeToday` and the
   hint update.
4. **US2**: the card shows 14 bars, oldest left, today right; a screen reader reads the period total and
   each day's count.
5. **SC-002/SC-003**: card visible within 1 s; keyboard and screen-reader pass; no horizontal scroll at
   375 px in EN/УК and light/dark.
6. **Failure**: stop the api container and reload — the card shows an error, the rest of the overview
   renders its own error states as before.
