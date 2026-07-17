# Development Log

## 2026-07-16

**Task:** Diagnose ACP connection issue with Claude.
**Done:**
- Confirmed ACP refers to Agent Client Protocol; Claude integration is typically via `claude-agent-acp` / `claude-code-acp` or inside Devin Desktop/Windsurf/Zed/JetBrains.
- Checked Cameo-mod repo: no ACP/Claude config present.
- Checked local environment: `node`, `npm`, `devin`, `claude`, and `claude-agent-acp` are not on PATH for this shell; no Windsurf ACP registry (`~/.windsurf/acp/registry.json`) or Windsurf logs found.
**Diagnosis (after user logs):** Devin Desktop/Windsurf is trying to spawn `npx -y @agentclientprotocol/claude-agent-acp@0.59.0`, but `npx` is not found in the IDE's PATH (`spawn npx ENOENT`). The ACP client needs Node.js installed (>=20.19 for this package) and available to the IDE process.
**Fix applied:**
- Downloaded and extracted Node.js v24.18.0 LTS to `%LOCALAPPDATA%\Programs\nodejs\node-v24.18.0-win-x64`.
- Added the Node `bin` directory to the user `PATH`.
- Set PowerShell execution policy to `RemoteSigned` for the current user so `*.ps1` scripts (including `npx.ps1`) can run.
- Installed `@agentclientprotocol/claude-agent-acp@0.59.0` globally via `npm`.
- Verified `node -v`, `npx -v`, `claude-agent-acp --version`, and `npx -y @agentclientprotocol/claude-agent-acp@0.59.0 --version` all work.
**Next:** Restart Devin Desktop/Windsurf so the IDE process picks up the updated `PATH`, then enable the Claude agent again.
