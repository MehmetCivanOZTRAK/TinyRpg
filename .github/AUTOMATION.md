# Tiny RPG Jira automation

The `Tiny RPG Codex Runner` workflow runs hourly on GitHub Actions and can also be started manually. It reads existing `codex-task` issues in Jira priority/key order. Only one task runs at a time. An unfinished or blocked issue pauses the queue, so later dependent work cannot pass it.

Configure these **GitHub Actions repository secrets** before enabling the runner:

- `OPENAI_API_KEY`
- `JIRA_EMAIL` and `JIRA_API_TOKEN` for an account that can read, comment, edit labels, and transition TINY issues
- `UNITY_EMAIL`, `UNITY_PASSWORD`, and either `UNITY_LICENSE` (Personal) or `UNITY_SERIAL` (Pro)

In GitHub Settings → Actions → General, allow the workflow token read/write access and allow Actions to create pull requests. Branch protection or review rules must also permit the bot to merge after checks; otherwise its PR stays open and Jira is marked `blocked`.

The workflow gives Jira and GitHub credentials only to the selection and reporting steps. The Codex step receives only `OPENAI_API_KEY`. The Unity test step receives license credentials. A failed Codex, PR, Unity, or merge step labels the Jira issue `blocked` and stops the queue. The run log and PR remain available for diagnosis. Remove the label and return the task to To Do only after resolving the cause.

Unity Test Runner covers import, compilation, and available automated EditMode/PlayMode tests. Visual rig, sword, animation appearance, and VFX checks require a separate Unity Editor review; the workflow reports them as unverified.
