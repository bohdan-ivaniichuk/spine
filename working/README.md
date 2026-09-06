# working/ — personal scratch space

Everything in this folder is **git-ignored** (only this `README.md` is tracked). Use it for your own
notes, draft Jira tickets, throwaway scripts, REPL snippets, exported reports — anything personal that
should live inside the workspace but **must not** be pushed to the shared repo.

Nothing here is shared with the team and nothing here is reviewed. Don't put anything the team needs in
here — promote it to `docs/`, `specs/` (Spec Kit), or the relevant product repo instead.

## For AI agents (Claude Code)

- Treat `working/` as the user's private scratch. **Do not** stage, commit, or push anything from here.
- It's fine to read/create/edit files here when the user is drafting something personal (e.g. a Jira
  draft before it's created via API), but keep it out of git.
- The git-ignore rule lives in the root `.gitignore` (`/working/*` with `!/working/README.md`).
