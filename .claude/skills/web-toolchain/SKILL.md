---
name: web-toolchain
description: >-
  Use before ANY command inside web/ (ng, npm, npx, vitest, prettier) or when a subagent must build or
  test the Angular app. Selects the right Node via nvm (Angular 22 CLI needs Node >= 22.22.3; the machine
  default is older), works around the npm 10 arborist "edgesOut" install crash, and lists the verified
  build/test/format commands and flags. Triggers: "npm start", "ng serve", "run web tests", "build the
  Angular app", "ERR node version", "Cannot read properties of null (reading 'edgesOut')".
---

# Web toolchain (Angular 22 in `web/`)

## Always first

```bash
source ~/.nvm/nvm.sh && nvm use 22 >/dev/null && node --version   # expect v22.23.x (>= 22.22.3)
cd "$(git rev-parse --show-toplevel)/web"
```

The system default `node` (22.19.0 / brew 19.x) fails Angular CLI's engine check with a misleading
error. Every `ng`, `npm`, `npx` invocation in `web/` must run in a shell where `nvm use 22` ran.
When delegating to a subagent, put the two lines above at the top of its prompt.

## Install

- Prefer `npm ci` (lockfile) — it works with npm 10.
- Fresh install / adding packages: npm 10's arborist can crash with
  `Cannot read properties of null (reading 'edgesOut')` on the jsdom → optional `canvas` peer.
  Use `npx npm@11 install <pkg>` instead of `npm install`.
- Never commit `node_modules`, `dist`, `.angular` (the CLI `.gitignore` covers them).

## Verified commands

| Task | Command | Notes |
|---|---|---|
| Dev server | `npm start` | `ng serve` with `proxy.conf.json` (`/api` → `localhost:8080`, `/api/health` → `/health/ready`) |
| Build | `npm run build` | output `dist/web/browser` |
| Unit tests | `npm test -- --watch=false` | Vitest 4 + jsdom (CLI default for v22); zoneless |
| Format check | `npx prettier --check .` | CLI scaffolds prettier; `web-format.sh` hook runs it on save |
| Lint | `npx ng lint --fix` only if `lint` exists in `package.json` | not scaffolded by default |
| Add Tailwind | `ng add tailwindcss` (v4) | then `@use 'tailwindcss';` in `src/styles.scss`, delete `src/tailwind.css` |
| Generate component | `ng g c shared/foo --skip-tests=false` | v22 does NOT emit `changeDetection: OnPush` — add it by hand |

## Testing `httpResource` in zoneless components

```ts
const fixture = TestBed.createComponent(ApiStatus);
TestBed.tick();                        // runs the resource effect → request issued
httpTesting.expectOne('/api/health').flush({ status: 'Healthy' });
await fixture.whenStable();
expect(fixture.nativeElement.textContent).toContain('API: OK');
```

Fake timers: `vi.useFakeTimers({ toFake: ['setInterval', 'clearInterval'] })`.

## Docker

`web/Dockerfile` builds with `node:22-alpine` (`npm ci && npm run build`) and serves with
`nginx:alpine`; healthcheck must target `127.0.0.1` (alpine resolves `localhost` to IPv6 first).
