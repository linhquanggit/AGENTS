#!/usr/bin/env pwsh
# ai-runtime.ps1 — wire the shared AI Runtime into your global agent config (~/.claude) on Windows.
# Mirrors bin/ai-runtime (bash). No Homebrew on Windows: runs from a git checkout, updates via git pull.
[CmdletBinding()]
param([Parameter(Position = 0)][string]$Command)

$ErrorActionPreference = 'Stop'

$ClaudeDir = if ($env:CLAUDE_CONFIG_DIR) { $env:CLAUDE_CONFIG_DIR } else { Join-Path $env:USERPROFILE '.claude' }
$GlobalMd  = Join-Path $ClaudeDir 'CLAUDE.md'
$Link      = Join-Path $ClaudeDir 'ai-runtime'
$Settings  = Join-Path $ClaudeDir 'settings.json'
$HookFile  = Join-Path $ClaudeDir 'ai-runtime-sessionstart.json'
$Begin = '# >>> ai-runtime >>>'
$End   = '# <<< ai-runtime <<<'

# Runtime home: $AI_RUNTIME_HOME if set, else the repo root relative to this script (bin/..).
$HomeDir = if ($env:AI_RUNTIME_HOME) { $env:AI_RUNTIME_HOME } else { Split-Path -Parent (Split-Path -Parent $PSCommandPath) }

# SessionStart hook: the hook command just prints a small JSON file (written on install) so the
# agent loads the runtime every session. Keeping the JSON in a file avoids Windows shell-quoting
# hell and preserves the unicode marker. Dedup/removal key on the "ai-runtime-sessionstart" path.
$HookCmd = "powershell -NoProfile -Command `"Get-Content -Raw -Encoding UTF8 -LiteralPath '$HookFile'`""

function Get-Directive {
  # Path uses backslashes; the model reads either separator. No ' or " so it embeds cleanly.
  "ACTIVE DIRECTIVE - do this BEFORE the first task: read $Link\AGENTS.md (profile selector) and follow it. " +
  "Pick the profile (Unity if the project has Assets/ + ProjectSettings/, else Common), load that profile context/ files, " +
  "then route via its Workflows.md. Prepend your first response of the session with [AI_RUNTIME_LOADED " +
  [char]0x00B7 + " global " + [char]0x00B7 + " <Profile>]. This is not optional background context."
}

function Get-Block {
  # Single-quoted here-string keeps backticks literal; __LINK__ is substituted after.
  $tpl = @'
# >>> ai-runtime >>>
# Managed by `ai-runtime`. Do not edit between these markers.
For **every task in any project**, use the shared AI Runtime at `__LINK__`:
1. Read `__LINK__\AGENTS.md` (the profile selector) and follow it.
2. Unity project (has `Assets/` + `ProjectSettings/`) -> `AI/Unity/`, else `AI/Common/`.
3. Load that profile's `context/`, then route to a skill per its `Workflows.md`.
4. Prepend your first response of the session with `[AI_RUNTIME_LOADED · global · <Profile>]` (`<Profile>` = Unity or Common) — confirms the global runtime was read. Verification only.
# <<< ai-runtime <<<
'@
  return $tpl.Replace('__LINK__', $Link)
}

function Remove-Block([string]$text) {
  if (-not $text) { return '' }
  $out = New-Object System.Collections.Generic.List[string]
  $skip = $false
  foreach ($ln in ($text -split "`r?`n")) {
    if ($ln -eq $Begin) { $skip = $true }
    if (-not $skip) { $out.Add($ln) }
    if ($ln -eq $End)   { $skip = $false }
  }
  return ($out -join "`n")
}

function Read-Settings {
  if (Test-Path -LiteralPath $Settings) {
    try { return (Get-Content -Raw -LiteralPath $Settings | ConvertFrom-Json) } catch { }
  }
  return (New-Object PSObject)
}

function Ensure-Prop($obj, [string]$name, $default) {
  if (-not ($obj.PSObject.Properties.Name -contains $name)) {
    $obj | Add-Member -NotePropertyName $name -NotePropertyValue $default
  }
  return $obj.$name
}

function Save-Settings($s) {
  ($s | ConvertTo-Json -Depth 30) | Set-Content -LiteralPath $Settings -Encoding UTF8
}

function Perms-Wire([string]$op) {
  $s = Read-Settings
  Ensure-Prop $s 'permissions' (New-Object PSObject) | Out-Null
  Ensure-Prop $s.permissions 'additionalDirectories' @() | Out-Null
  $ad = @($s.permissions.additionalDirectories)
  if ($op -eq 'add') {
    foreach ($x in @($Link, $HomeDir)) { if ($ad -notcontains $x) { $ad += $x } }
  } else {
    $ad = @($ad | Where-Object { $_ -ne $Link -and $_ -ne $HomeDir })
  }
  $s.permissions.additionalDirectories = @($ad)
  Save-Settings $s
}

function Hook-Wire([string]$op) {
  $s = Read-Settings
  Ensure-Prop $s 'hooks' (New-Object PSObject) | Out-Null
  Ensure-Prop $s.hooks 'SessionStart' @() | Out-Null
  $ss = @($s.hooks.SessionStart)
  $marked = { param($g) @($g.hooks) | Where-Object { "$($_.command)" -like '*ai-runtime-sessionstart*' } }
  if ($op -eq 'add') {
    $has = $false
    foreach ($g in $ss) { if (& $marked $g) { $has = $true } }
    if (-not $has) {
      $grp = [PSCustomObject]@{ hooks = @([PSCustomObject]@{ type = 'command'; command = $HookCmd }) }
      $ss += $grp
    }
  } else {
    $ss = @($ss | Where-Object { -not (& $marked $_) })
  }
  $s.hooks.SessionStart = @($ss)
  Save-Settings $s
}

function Set-Junction {
  if ($HomeDir -eq $Link) { return }   # runtime already lives at the link path (clone-in-place)
  if (Test-Path -LiteralPath $Link) {
    $item = Get-Item -LiteralPath $Link -Force
    if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) {
      cmd /c rmdir "$Link" | Out-Null            # remove the junction only, never the target
    } else {
      throw "$Link exists and is not a junction; refusing to overwrite. Remove it manually first."
    }
  }
  New-Item -ItemType Junction -Path $Link -Target $HomeDir | Out-Null
}

function Cmd-Install {
  if (-not (Test-Path -LiteralPath (Join-Path $HomeDir 'AGENTS.md'))) { throw "runtime not found at $HomeDir" }
  New-Item -ItemType Directory -Force -Path $ClaudeDir | Out-Null
  Set-Junction

  $body = ''
  if (Test-Path -LiteralPath $GlobalMd) { $body = (Remove-Block (Get-Content -Raw -LiteralPath $GlobalMd)).TrimEnd() }
  $content = if ($body) { "$body`n`n$(Get-Block)" } else { Get-Block }
  Set-Content -LiteralPath $GlobalMd -Value $content -Encoding UTF8

  # Write the SessionStart directive file the hook prints.
  $hookObj  = [PSCustomObject]@{ hookSpecificOutput = [PSCustomObject]@{ hookEventName = 'SessionStart'; additionalContext = (Get-Directive) } }
  ($hookObj | ConvertTo-Json -Compress -Depth 10) | Set-Content -LiteralPath $HookFile -Encoding UTF8 -NoNewline

  Perms-Wire 'add'
  Hook-Wire  'add'

  Write-Host "installed: $Link -> $HomeDir"
  Write-Host "updated:   $GlobalMd (managed block)"
  Write-Host "updated:   $Settings (read scope + SessionStart hook)"
  Write-Host "wrote:     $HookFile"
}

function Cmd-Uninstall {
  if (Test-Path -LiteralPath $Link) {
    $item = Get-Item -LiteralPath $Link -Force
    if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) {
      cmd /c rmdir "$Link" | Out-Null
      Write-Host "removed link $Link"
    }
  }
  if (Test-Path -LiteralPath $GlobalMd) {
    (Remove-Block (Get-Content -Raw -LiteralPath $GlobalMd)).TrimEnd() | Set-Content -LiteralPath $GlobalMd -Encoding UTF8
    Write-Host "removed managed block from $GlobalMd"
  }
  if (Test-Path -LiteralPath $Settings) { Perms-Wire 'remove'; Hook-Wire 'remove'; Write-Host "removed read scope + hook from $Settings" }
  if (Test-Path -LiteralPath $HookFile) { Remove-Item -LiteralPath $HookFile -Force; Write-Host "removed $HookFile" }
}

function Cmd-Status {
  $target = '(none)'
  if (Test-Path -LiteralPath $Link) {
    $it = Get-Item -LiteralPath $Link -Force
    if ($it.Attributes -band [IO.FileAttributes]::ReparsePoint) { $target = "$($it.Target)" } else { $target = '(not a junction)' }
  }
  $mdWired = (Test-Path -LiteralPath $GlobalMd) -and ((Get-Content -Raw -LiteralPath $GlobalMd) -like "*$Begin*")
  # Parse settings.json so backslashes compare literally (raw JSON escapes \ as \\).
  $readWired = $false; $hookWired = $false
  $s = Read-Settings
  if (($s.PSObject.Properties.Name -contains 'permissions') -and ($s.permissions.PSObject.Properties.Name -contains 'additionalDirectories')) {
    $ad = @($s.permissions.additionalDirectories)
    if (($ad -contains $HomeDir) -or ($ad -contains $Link)) { $readWired = $true }
  }
  if (($s.PSObject.Properties.Name -contains 'hooks') -and ($s.hooks.PSObject.Properties.Name -contains 'SessionStart')) {
    foreach ($g in @($s.hooks.SessionStart)) { foreach ($h in @($g.hooks)) { if ("$($h.command)" -like '*ai-runtime-sessionstart*') { $hookWired = $true } } }
  }
  Write-Host "runtime home : $HomeDir"
  Write-Host "link         : $Link -> $target"
  Write-Host "global md    : $(if ($mdWired) { 'wired' } else { 'not wired' })"
  Write-Host "read scope   : $(if ($readWired) { 'wired' } else { 'not wired' })"
  Write-Host "session hook : $(if ($hookWired) { 'wired' } else { 'not wired' })"
}

function Cmd-Update {
  if (Test-Path -LiteralPath (Join-Path $HomeDir '.git')) {
    # Hard reset — managed checkout; avoids abort when autocrlf dirties a tracked file.
    git -C $HomeDir fetch origin main
    git -C $HomeDir reset --hard FETCH_HEAD
    Write-Host "updated (reset to origin/main)"
  } else {
    Write-Host "no git checkout at $HomeDir — reinstall from the repo to update"
  }
}

switch ($Command) {
  'install'   { Cmd-Install }
  'uninstall' { Cmd-Uninstall }
  'status'    { Cmd-Status }
  'update'    { Cmd-Update }
  default     { Write-Error "usage: ai-runtime.ps1 {install|uninstall|status|update}"; exit 2 }
}
