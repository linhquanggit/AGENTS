# Install

The AI Runtime installs once per machine and every agent session routes through it.

## For users — install via Homebrew

Quickest (no tap needed) — install straight from the formula URL:
```sh
brew install https://raw.githubusercontent.com/linhquanggit/AGENTS/main/packaging/ai-runtime.rb
ai-runtime install                          # wire ~/.claude (symlink + managed block + read-scope permission)
```

Or, once a tap exists (nicer, supports `brew upgrade`):
```sh
brew install linhquanggit/tap/ai-runtime
ai-runtime install
```
- `ai-runtime status` — show what's wired.
- `ai-runtime uninstall` — remove the link + managed block.
- `brew upgrade ai-runtime` — update the runtime for all projects at once.

After `ai-runtime install`, open Claude Code in any project: it auto-loads `~/.claude/CLAUDE.md`, which routes through the runtime and selects a profile (Unity if `Assets/`+`ProjectSettings/`, else Common).

## For developers — run from a git checkout (no Homebrew)
```sh
git clone https://github.com/linhquanggit/AGENTS.git
cd AGENTS
./bin/ai-runtime install      # links ~/.claude/ai-runtime -> this checkout
./bin/ai-runtime update       # git pull
```

## For the maintainer — cut a release + publish the formula
1. Tag and push a release:
   ```sh
   git tag v0.1.0 && git push origin v0.1.0
   ```
2. Get the tarball sha256:
   ```sh
   curl -sL https://github.com/linhquanggit/AGENTS/archive/refs/tags/v0.1.0.tar.gz | shasum -a 256
   ```
3. Create a tap repo named `homebrew-tap` (→ `github.com/linhquanggit/homebrew-tap`).
   Copy `packaging/ai-runtime.rb` into it as `Formula/ai-runtime.rb`, then set the `url`
   tag and paste the `sha256`.
4. Users can now `brew install linhquanggit/tap/ai-runtime`.

To release a new version: bump the tag, recompute the sha256, update the formula.
