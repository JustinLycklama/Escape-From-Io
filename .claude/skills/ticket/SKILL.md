---
name: ticket
description: Work with the Escape From Io backlog in GitHub Issues. Use it to start work on an issue number, file a new bug or feature ticket, record findings on a ticket, or review what's open. Use whenever the user mentions an issue/ticket, asks what to work on, or wants an idea captured for later.
---

# Tickets (GitHub Issues via `gh`)

Repo: `JustinLycklama/Escape-From-Io`. Labels: a type (`bug`, `feature`, `tech-debt`, `tooling`, `docs`), one or more `area:*`
(`tasks`, `pathfinding`, `map`, `units-buildings`, `ui`, `art-assets`), and `needs-repro` for unconfirmed bugs.

## What's open
`gh issue list --limit 50`. Group by type/area and suggest what to pick next (bugs blocking play first, then the feature the user is excited about).

## Start a ticket `<n>`
1. `gh issue view <n> --comments`. Read the whole thread; earlier sessions leave findings there.
2. Read the relevant ARCHITECTURE.md section.
3. `git switch master && git pull`, then `git switch -c <n>-short-name`.
4. Do the work, verify with the `play-test` skill, and commit with `Fixes #<n>` (or `Refs #<n>` if partial).
5. Comment on the issue with what changed, how it was verified, and anything left over. Then merge to `master` or open a PR, as the user prefers.

## File a new ticket
Use the template fields as headings (`.github/ISSUE_TEMPLATE/bug.yml` / `feature.yml`):
`gh issue create --title "..." --label bug,area:tasks --body-file <tmp.md>`
- Bugs: what happens, how to reproduce (or `needs-repro`), evidence with `path/File.cs:line`, suspected cause.
- Features: goal, design notes and open questions, "done when" criteria that can be checked in the editor, systems touched.
Keep the user's wording for ideas they dictate. Don't invent scope. The repo is **public**, so never put secrets or personal info in tickets.

## Record findings
When an investigation ends without a fix, `gh issue comment <n>` with what was tried, the evidence, and the next step, so the next session doesn't redo it.
