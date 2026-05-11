# 001 — Agent tool `isolation: "worktree"` cross-contaminates parallel agents

- **Status:** Open, ready to file at https://github.com/anthropics/claude-code/issues
- **Filed:** 2026-05-11
- **Tool affected:** Claude Code `Agent` tool with `isolation: "worktree"` parameter
- **Severity:** Medium — silently bundles work across "isolated" agents; recoverable by post-hoc inspection but error-prone for batch workflows.

## Summary

Spawning five `general-purpose` agents in a single tool message with
`isolation: "worktree"` produced three observable cross-contamination
events: a third agent's worktree contained the partial work of a sibling
agent; a fourth agent's `git stash pop` absorbed a fifth agent's stashed
changes and committed them under the fourth agent's commit message; the
fifth agent reported "absorbed with sub-cluster D during stash pop from
shared worktree". Two of five agents (A and B) behaved as expected with
proper isolation; three of five (C, D, E) leaked.

Each agent was nominally given its own worktree path under
`.claude/worktrees/agent-<id>/` and its own branch
`worktree-agent-<id>`. All five worked simultaneously, with overlapping
execution windows, against the same parent repository's `.git/`.

## Expected behavior

With `isolation: "worktree"`, each spawned agent should see only its
own working directory and its own branch. Operations in one agent's
worktree — including `git stash`, `git checkout`, `git reset`, and
ephemeral file writes — should be invisible to peers.

## Observed behavior

- Agent C (worktree `agent-a3f7a0c11560ae822`) reported on first inspection:
  *"Worktree had pre-existing unstaged changes to FooterSection /
  PaymentSection / QrSection / TotalsSection / ThemeColors from another
  agent. Restored to HEAD via `git checkout --` where needed; those
  changes remained unstaged and are not in this commit."*
- Agent D (worktree `agent-ae691672456ed4be6`) reported:
  *"Test delta: 108 baseline → 122 pass + 2 skip (6 new T3cP tests;
  worktree already had FooterRhythmTests.cs + QrCompactionTests.cs
  from prior agent which are also now committed)."*
- Agent E (worktree `agent-a8e73afeaf1ad3e4c`) reported:
  *"3c-polish E changes landed in commit 906467c alongside sub-cluster D.
  ...
  Note: 3c-polish E changes landed in commit `906467c` (absorbed with
  sub-cluster D during stash pop from shared worktree). Content correct;
  commit message reflects D scope only."*
- Final result on the shared `main` branch:
  - Commit `906467c` (titled "Phase 3c-polish D") contains:
    - `TotalsSection.cs` + `PaymentSection.cs` + `ThemeColors.cs` (D scope, correct)
    - `FooterSection.cs` + `QrSection.cs` + `FooterRhythmTests.cs` +
      `QrCompactionTests.cs` (E scope, **incorrectly bundled into D's commit**)
  - Cluster E never produced a standalone commit; its message + history
    record were both lost.

## Hypothesis

The Agent harness places each worktree's working tree at a distinct path
but does not appear to isolate the **shared `.git` directory's stash
stack**. `git stash push` from any worktree writes to the same global
stash list; `git stash pop` from a peer worktree pulls the most recent
entry, which may be the peer's work in progress rather than the calling
agent's own stash. This matches the observed symptom in agent E's report
("absorbed with sub-cluster D during stash pop").

A secondary symptom — agent C observing peer changes as unstaged — is
consistent with one or more peers having checked out the same files
into `main` (the shared branch) before agent C's worktree's HEAD was
moved off `main` and onto its own `worktree-agent-<id>` branch. Race
condition between worktree creation and branch checkout.

## Reproduction

The contamination triggered with five concurrent agents. The minimum
reproducer probably does not need five — two parallel agents that both
touch the same file and either call `git stash` or share a transient
working state should suffice. Recommend testing with:

```text
Agent A: edit src/foo.cs, run `git stash`, modify other file, `git stash pop`.
Agent B: edit src/foo.cs differently, run `git stash`, modify other file, `git stash pop`.
```

Run both concurrently with `isolation: "worktree"`. Inspect each
agent's HEAD diff after they both finish.

## Workaround (used in this repo)

- Verify each agent's claimed commit independently after the parallel
  batch completes — read `git log --all`, inspect each commit's diff
  stat against the cluster scope before merging anything.
- When clusters share any potential edit target (a helper file,
  a shared CSS module, a manifest, a `.csproj`), **serialize them**
  instead of running in parallel.
- Document the actual scope of each landed commit in a project
  changelog even when commit messages are misleading.
- Avoid `git stash` inside agent prompts; use `git checkout -- <path>`
  to discard untracked-by-this-agent edits the harness has surfaced.

## Suggested fix

1. Use `git worktree add --isolate-locks` or an equivalent flag that
   gives each worktree its own stash namespace (`refs/stash` per
   worktree, not the shared global).
2. Or: have the Agent harness inspect each worktree for *unexpected*
   modifications before handing it to the agent, and surface a clear
   warning ("worktree appears to contain unstaged changes from another
   active agent — refusing to start").
3. Or: refuse to spawn a second worktree-isolated agent if any in-flight
   peer is touching overlapping path prefixes (heuristic on the
   prompts).

## Project trace

The full incident is logged in PROGRESS.md divergence #29
("Parallel cluster orchestrators leaked across worktrees — D's commit
silently bundled E's content"), with the corresponding commits
`80538f6`, `906467c`, `4c49d5e`, `a7bb14e` on the project's `main`
branch.

## Status

This document is the local record. Awaiting decision on whether to
mirror it as a GitHub issue at https://github.com/anthropics/claude-code/issues
(external action — held for explicit go).
