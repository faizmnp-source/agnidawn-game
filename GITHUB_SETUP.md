# GitHub Setup Instructions for AGNIDAWN

Run these commands in your Windows terminal (PowerShell or Git Bash) from the project folder.

## Step 1: Install GitHub CLI
```powershell
winget install GitHub.cli
```
Or download from: https://cli.github.com

## Step 2: Authenticate
```bash
gh auth login
# Choose: GitHub.com → HTTPS → Login with browser
```

## Step 3: Initialize Local Git Repo
```bash
cd "C:\Users\Micro\30 Minute\30 Minutes\agnidawn-game"
git init
git checkout -b main
git add .
git commit -m "feat: initial project scaffold - Unity structure, GDD, CI/CD

Linear: FAI-5"
```

## Step 4: Create GitHub Repository
```bash
gh repo create agnidawn-game \
  --public \
  --description "Indian mythology roguelite survival game — Unity URP" \
  --source=. \
  --remote=origin \
  --push
```

## Step 5: Create develop branch
```bash
git checkout -b develop
git push --set-upstream origin develop
```

## Step 6: Set branch protection rules (via GitHub web)
Go to: https://github.com/YOUR_USERNAME/agnidawn-game/settings/branches
- Protect `main`: require PR, require status checks
- Protect `develop`: require PR from feature branches

## Step 7: Add Unity License secret for CI/CD
Go to: https://github.com/YOUR_USERNAME/agnidawn-game/settings/secrets/actions
- Add secret: `UNITY_LICENSE` (your Unity license XML)
- To get it: Unity Editor → Help → Manage License → Export License

## Branch Naming Convention
```
feature/FAI-XX-short-description   (new features)
fix/FAI-XX-bug-description         (bug fixes)
hotfix/critical-issue-name         (emergency fixes)
```

## Daily Workflow
```bash
# Start new feature
git checkout develop
git pull
git checkout -b feature/FAI-XX-feature-name

# Work, then commit
git add .
git commit -m "feat: description of change [FAI-XX]"

# Push and create PR
git push --set-upstream origin feature/FAI-XX-feature-name
gh pr create --base develop --title "feat: description" --body "Closes FAI-XX"
```
