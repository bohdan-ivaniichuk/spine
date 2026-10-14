# web — Spine Angular shell

Angular 22 (zoneless, standalone, signals), Tailwind 4, Vitest. Served by nginx in Docker with `/api`
proxied to the API.

Before any command here run `nvm use 22` (Angular 22 CLI needs Node >= 22.22.3) — see
`../.claude/skills/web-toolchain/SKILL.md` for the full toolchain notes.

```bash
npm ci
npm start                    # http://localhost:4200, /api proxied to http://localhost:8080
npm test -- --watch=false    # Vitest
npm run build                # dist/web/browser
```

Feature folders: `src/app/knowledge` (003), `src/app/review` (002), `src/app/grading` (004),
`src/app/shared` (cross-cutting: API types, status indicator).
