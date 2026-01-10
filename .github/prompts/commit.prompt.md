---
agent: agent
model: Claude Haiku 4.5 (copilot)
tools: ['vscode', 'execute', 'read', 'edit', 'search', 'web', 'agent', 'todo']
description: This prompt is used to commit changes to a git repository following specific rules.
---

# Commit Changes

Commit all staged and unstaged changes following these rules:

## Rules
1. **Never commit directly to main** - Always create or use a private branch
2. **Use concise commit messages** - Summarize the change in a single line
3. **Ask before pushing** - After committing, ask if the user wants to push

## Steps

1. Check the current branch:
   - If on `main`, create a new branch with a descriptive name based on the changes
   - If on a feature branch, continue on that branch

2. Stage all changes:
   ```bash
   git add -A
   ```

3. Review what will be committed:
   ```bash
   git status
   ```

4. Create a concise commit message that summarizes the changes

5. Present the commit message to the user and ask for approval:
   > "Proposed commit message: `<commit message>`
   > 
   > Would you like me to proceed with this commit message, or would you prefer a different one?"

6. Once approved, commit the changes:
   ```bash
   git commit -m "<approved commit message>"
   ```

7. After successful commit, ask the user:
   > "Changes committed successfully. Would you like me to push these changes to the remote repository?"

8. If user confirms, push the branch:
   ```bash
   git push -u origin <branch-name>
   ```