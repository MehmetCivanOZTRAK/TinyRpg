# Tiny RPG Jira automation

## Cloud Mode

`Tiny RPG Codex Runner` runs hourly on GitHub Actions or by manual dispatch. It selects the first unfinished existing `codex-task` issue in priority/key order, moves it to In Progress, asks Codex to implement that issue on a separate branch, runs the Jira queue self-test, protected path checks, and source file whitespace check, then opens a pull request. Unity generated `.prefab`, `.controller`, and `.meta` YAML is excluded from the whitespace check because Unity writes trailing spaces on empty scalar fields. It comments on the Jira issue with the PR, changed files, static check results, and an explicit **Unity NOT VERIFIED** status. The issue stays In Progress and the PR stays open. An unfinished issue pauses later tasks. A failed run labels its issue `blocked` and records the run URL in Jira.

Required GitHub Actions repository secrets:

- `OPENAI_API_KEY`
- `JIRA_EMAIL` and `JIRA_API_TOKEN` for an account allowed to read, comment, edit labels, and transition TINY issues

No Unity account, license, or Editor credentials are required in GitHub Actions. In GitHub Settings → Actions → General, allow Actions to create pull requests. The workflow requests scoped `contents: write` and `pull-requests: write` permissions for its own token. Do not grant the Codex implementation step access to Jira or GitHub credentials.

Cloud Mode does not run Unity. Its static checks cannot establish import, compilation, Animator, rig, prefab, scene, or Play Mode correctness. It never merges a task PR or marks the Jira issue Done.

## Unity MCP Mode

When the local computer, Unity Editor, and its MCP bridge are running, connect Codex to the **same TinyRpg project**. Inspect the open task PR and Jira acceptance criteria. Import the PR branch in Unity, inspect prefab/animation/rig/scene references, run available EditMode/PlayMode checks, and record actual observations and any fixes in the PR and Jira. A visual or Play Mode check counts as passed only when it was performed in the Editor. Merge only after all required checks pass, then mark Jira Done and continue to the next ready task. If a check fails, fix it on the task branch and rerun it; unresolved failures keep the PR open and the issue blocked or In Progress.

Two Unity MCP implementations are available. The existing Codex `unityMCP` entry runs `mcpforunityserver` over stdio and requires its matching Editor package. Unity's `com.unity.ai.assistant` package exposes a separate bridge through `%USERPROFILE%\.unity\relay\relay_win.exe --mcp --project-path <TinyRpg path>`. Configure that relay as a separate Codex MCP server; the two protocols are not interchangeable. Check the active project path before editing assets. The native relay was tested against the local TinyRpg Editor and returned Unity tools and project data.

The local Codex follow-up can run only while the computer and Editor are available. GitHub Actions remains independent of local Unity or the user's Unity license.
