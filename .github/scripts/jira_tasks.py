"""Select and report one Jira task for the scheduled Codex workflow."""

import base64
import json
import os
import re
import subprocess
import sys
import urllib.parse
import urllib.request
from pathlib import Path


BASE = "https://mehmetciwan02-1793014553432.atlassian.net"


def request(method, path, data=None, github=False):
    if github:
        url = "https://api.github.com" + path
        auth = "Bearer " + os.environ["GH_TOKEN"]
        headers = {"Authorization": auth, "Accept": "application/vnd.github+json"}
    else:
        url = BASE + path
        pair = (os.environ["JIRA_EMAIL"] + ":" + os.environ["JIRA_API_TOKEN"]).encode()
        headers = {
            "Authorization": "Basic " + base64.b64encode(pair).decode(),
            "Accept": "application/json",
        }
    if data is not None:
        headers["Content-Type"] = "application/json"
    body = json.dumps(data).encode() if data is not None else None
    with urllib.request.urlopen(
        urllib.request.Request(url, data=body, headers=headers, method=method), timeout=30
    ) as response:
        raw = response.read()
        return json.loads(raw) if raw else {}


def jira(method, path, data=None):
    return request(method, path, data)


def adf_text(node):
    if isinstance(node, str):
        return node
    if isinstance(node, list):
        return "\n".join(filter(None, (adf_text(item) for item in node)))
    if isinstance(node, dict):
        return node.get("text", "") + adf_text(node.get("content", []))
    return ""


def pending_issue(issues):
    # shortcut: Jira tasks currently have no dependency links; use one ordered queue until links are maintained.
    for issue in issues:
        if issue["fields"]["status"]["statusCategory"]["key"] != "done":
            return issue
    return None


def transition(key, category):
    choices = jira("GET", f"/rest/api/3/issue/{key}/transitions")["transitions"]
    match = next(
        (choice for choice in choices if choice["to"]["statusCategory"]["key"] == category),
        None,
    )
    if match is None:
        raise RuntimeError(f"No Jira transition to {category} for {key}")
    jira("POST", f"/rest/api/3/issue/{key}/transitions", {"transition": {"id": match["id"]}})


def comment(key, message):
    body = {"body": {"type": "doc", "version": 1, "content": [
        {"type": "paragraph", "content": [{"type": "text", "text": message}]}
    ]}}
    jira("POST", f"/rest/api/3/issue/{key}/comment", body)


def prepare():
    explicit = os.getenv("ISSUE_INPUT", "").strip().upper()
    if explicit and not re.fullmatch(r"TINY-\d+", explicit):
        raise ValueError("Invalid Jira issue key")
    if explicit:
        issue = jira("GET", f"/rest/api/3/issue/{explicit}?fields=summary,description,status,labels")
    else:
        query = urllib.parse.urlencode({
            "jql": "project = TINY AND labels = codex-task ORDER BY priority DESC, key ASC",
            "fields": "summary,description,status,labels,priority",
            "maxResults": "100",
        })
        issue = pending_issue(jira("GET", "/rest/api/3/search/jql?" + query)["issues"])
    if issue is None:
        print("No outstanding Jira task")
        return
    key = issue["key"]
    fields = issue["fields"]
    if "codex-task" not in fields.get("labels", []):
        raise ValueError(f"{key} is not an existing Codex task")
    if "blocked" in fields.get("labels", []) or fields["status"]["statusCategory"]["key"] != "new":
        print(f"Queue paused at {key}: task is not ready")
        return
    repo = os.environ["GITHUB_REPOSITORY"]
    pulls = request("GET", f"/repos/{repo}/pulls?state=open&per_page=100", github=True)
    if any(pr["title"].startswith(key + ":") for pr in pulls):
        print(f"Queue paused at {key}: an open PR already exists")
        return
    transition(key, "indeterminate")
    prompt = (
        f"Complete only Jira task {key}: {fields['summary']}\n"
        "Read Assets/GDD.md.docx and the existing project first. Preserve scenes, prefabs, "
        "meta GUIDs and existing controls. Single player controls the whole squad; "
        "there is no character switching. Multiplayer is out of scope. "
        "Do not edit .github, secrets, workflow files, or Git configuration. "
        "Run available checks and never claim visual Unity validation unless performed.\n\n"
        "Jira description and acceptance criteria:\n"
        + adf_text(fields.get("description"))[:16000]
    )
    Path(os.environ["RUNNER_TEMP"], "tiny-task.txt").write_text(prompt, encoding="utf-8")
    with open(os.environ["GITHUB_OUTPUT"], "a", encoding="utf-8") as output:
        output.write("issue=" + key + "\n")
    print("Started", key)


def report_cloud():
    key = os.environ["ISSUE_KEY"]
    pr = os.getenv("PR_URL") or "not created"
    run = os.getenv("RUN_URL") or "unavailable"
    files = subprocess.check_output(
        ["gh", "pr", "diff", pr, "--name-only"], text=True
    ).strip() or "none"
    comment(key, (
        f"Cloud Mode PR opened.\nPR: {pr}\nActions run: {run}\n"
        f"Changed files:\n{files[:6000]}\n"
        "Static checks: Jira queue self-test, protected path check, source file whitespace check passed. "
        "Unity import, compilation, prefab, animation, rig, scene, and Play Mode: NOT VERIFIED. "
        "Task remains In Progress; Unity MCP Mode review is required before merge."
    ))


def report_blocked():
    key = os.environ["ISSUE_KEY"]
    pr = os.getenv("PR_URL") or "not created"
    run = os.getenv("RUN_URL") or "unavailable"
    issue = jira("GET", f"/rest/api/3/issue/{key}?fields=labels")
    labels = sorted(set(issue["fields"].get("labels", []) + ["blocked"]))
    jira("PUT", f"/rest/api/3/issue/{key}", {"fields": {"labels": labels}})
    comment(key, f"Automation blocked after a failed step.\nPR: {pr}\nActions run: {run}\nUnity validation: NOT VERIFIED. Check the run before retrying; dependent work is paused.")


def selftest():
    issues = [
        {"key": "TINY-7", "fields": {"status": {"statusCategory": {"key": "done"}}}},
        {"key": "TINY-8", "fields": {"status": {"statusCategory": {"key": "new"}}}},
        {"key": "TINY-9", "fields": {"status": {"statusCategory": {"key": "new"}}}},
    ]
    assert pending_issue(issues)["key"] == "TINY-8"
    issues[1]["fields"]["status"]["statusCategory"]["key"] = "indeterminate"
    assert pending_issue(issues)["key"] == "TINY-8"
    assert adf_text({"content": [{"text": "criterion"}]}) == "criterion"
    print("Jira queue self-test passed")


if __name__ == "__main__":
    {"prepare": prepare, "cloud_report": report_cloud, "blocked": report_blocked, "selftest": selftest}[sys.argv[1]]()
