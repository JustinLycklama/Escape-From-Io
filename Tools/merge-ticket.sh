#!/usr/bin/env bash
# Merge the current ticket branch into master WITHOUT rewriting files on disk, then delete the branch and push.
#
# Why: `git switch master` + `git merge` rewrites open scene files (e.g. GameScene.unity) twice, and the Unity editor
# then blocks on a modal "scene changed externally, reload?" prompt, which also blocks the Unity CLI.
# Since master hasn't moved while the branch was worked on, the merge result is exactly the branch's tree, so the
# merge commit is built with commit-tree and master is pointed at it; the final switch is a no-op on disk.
#
# Usage (from the ticket branch, everything committed):  bash Tools/merge-ticket.sh "Merge #16: HUD robot cards" [--no-push]
set -euo pipefail

msg="${1:?merge commit message required}"
push=true
[ "${2:-}" = "--no-push" ] && push=false

branch="$(git branch --show-current)"
[ "$branch" != "master" ] || { echo "already on master; run this from the ticket branch" >&2; exit 1; }

# Only tracked changes block the merge; deliberately-uncommitted .obj.meta deletions (see CLAUDE.md) are fine
if git status --porcelain --untracked-files=no | grep -v '\.obj\.meta"\?$' | grep -q .; then
    echo "commit or stash changes first:" >&2
    git status --short --untracked-files=no | grep -v '\.obj\.meta"\?$' >&2
    exit 1
fi

git merge-base --is-ancestor master HEAD || { echo "master has moved; rebase or merge master into $branch first" >&2; exit 1; }

merge_commit="$(git commit-tree "HEAD^{tree}" -p master -p HEAD -m "$msg")"
git update-ref refs/heads/master "$merge_commit"
git switch -q master             # same tree as the branch: nothing on disk changes
git branch -d "$branch" >/dev/null
echo "merged $branch -> master ($merge_commit)"

if $push; then
    git push -q origin master
    echo "pushed master"
fi
