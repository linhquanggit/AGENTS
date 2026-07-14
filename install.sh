#!/usr/bin/env bash
# One-line installer for the AI Runtime.
#   curl -fsSL https://raw.githubusercontent.com/linhquanggit/AGENTS/main/install.sh | bash
# Installs the Homebrew formula, then wires ~/.claude (symlink + managed block +
# read scope + SessionStart hook). Re-running upgrades to the latest and re-wires.
set -euo pipefail

FORMULA_URL="https://raw.githubusercontent.com/linhquanggit/AGENTS/main/packaging/ai-runtime.rb"

say() { printf '\033[1;36m==>\033[0m %s\n' "$1"; }
die() { printf '\033[1;31mError:\033[0m %s\n' "$1" >&2; exit 1; }

command -v brew >/dev/null 2>&1 || die \
  "Homebrew is required (macOS/Linux). Install it from https://brew.sh first. Windows is not supported yet."

if brew list ai-runtime >/dev/null 2>&1; then
  say "ai-runtime already installed — upgrading to latest…"
  brew reinstall "$FORMULA_URL"
else
  say "Installing ai-runtime via Homebrew…"
  brew install "$FORMULA_URL"
fi

# brew may link into a prefix not yet on this shell's PATH — resolve explicitly.
AR="$(command -v ai-runtime || true)"
[ -n "$AR" ] || AR="$(brew --prefix)/bin/ai-runtime"
[ -x "$AR" ] || die "ai-runtime not found after install (looked at $AR)."

say "Wiring ~/.claude…"
"$AR" install

echo
say "Done. Current state:"
"$AR" status
echo
say "Open a new agent session in any project — you should see [AI_RUNTIME_LOADED · global · <Profile>]."
