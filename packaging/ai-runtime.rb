# Homebrew formula for the AI Runtime.
# Place this in your tap repo (e.g. linhquanggit/homebrew-tap) as Formula/ai-runtime.rb,
# after filling in the release tag + sha256 (see INSTALL.md).
class AiRuntime < Formula
  desc "Multi-profile AI coding runtime for Claude Code and other agents"
  homepage "https://github.com/linhquanggit/AGENTS"
  url "https://github.com/linhquanggit/AGENTS/archive/refs/tags/v0.1.0.tar.gz"
  sha256 "c3a48f5655f99807cff937dd31dedb434f0190182ef5d2f14d207943fda48172"
  # license "MIT"  # add a LICENSE file to the repo, then enable this

  def install
    # Install the whole runtime (AGENTS.md selector + AI/ profiles + bin/) under libexec.
    libexec.install Dir["*"]
    # Thin wrapper on PATH that points the CLI at the installed content (stable opt path).
    (bin/"ai-runtime").write <<~SH
      #!/usr/bin/env bash
      export AI_RUNTIME_HOME="#{opt_libexec}"
      exec "#{opt_libexec}/bin/ai-runtime" "$@"
    SH
  end

  def caveats
    <<~EOS
      Wire the runtime into your global agent config (once):
        ai-runtime install

      This links ~/.claude/ai-runtime to this formula and adds a managed block to
      ~/.claude/CLAUDE.md so every Claude Code / agent session routes through it.

      Status:   ai-runtime status
      Remove:   ai-runtime uninstall
      Update:   brew upgrade ai-runtime
    EOS
  end

  test do
    ENV["AI_RUNTIME_HOME"] = libexec
    assert_match "runtime home", shell_output("#{bin}/ai-runtime status")
  end
end
