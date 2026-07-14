# One-line installer for the AI Runtime on Windows (PowerShell).
#   irm https://raw.githubusercontent.com/linhquanggit/AGENTS/main/install.ps1 | iex
# Clones the runtime into %USERPROFILE%\.claude\ai-runtime, then wires ~/.claude
# (managed block + read scope + SessionStart hook). Re-running upgrades via git pull.
$ErrorActionPreference = 'Stop'

$Repo = 'https://github.com/linhquanggit/AGENTS.git'
$ClaudeDir = if ($env:CLAUDE_CONFIG_DIR) { $env:CLAUDE_CONFIG_DIR } else { Join-Path $env:USERPROFILE '.claude' }
$Dest = Join-Path $ClaudeDir 'ai-runtime'

function Say([string]$m) { Write-Host "==> $m" -ForegroundColor Cyan }

if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
  throw "git is required. Install it from https://git-scm.com/download/win first."
}

New-Item -ItemType Directory -Force -Path $ClaudeDir | Out-Null

if (Test-Path -LiteralPath (Join-Path $Dest '.git')) {
  Say "ai-runtime already installed — updating to latest…"
  git -C $Dest pull --ff-only
} elseif (Test-Path -LiteralPath $Dest) {
  throw "$Dest exists but is not a git checkout. Run 'ai-runtime.ps1 uninstall' or remove it, then retry."
} else {
  Say "Cloning runtime into $Dest…"
  git clone --depth 1 $Repo $Dest
}

$Ps1 = Join-Path $Dest 'bin\ai-runtime.ps1'
Say "Wiring ~/.claude…"
powershell -NoProfile -ExecutionPolicy Bypass -File $Ps1 install

Write-Host ""
Say "Done. Current state:"
powershell -NoProfile -ExecutionPolicy Bypass -File $Ps1 status
Write-Host ""
Say "Open a new agent session in any project — you should see [AI_RUNTIME_LOADED · global · <Profile>]."
