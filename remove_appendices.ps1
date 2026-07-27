$file = 'docs\Cameo_Knowledge_Base_Manual.md'
$lines = Get-Content $file -Encoding UTF8

# 1. Remove appendix Q-Y content (lines 41039 to end, 0-indexed: 41038 to end)
$contentLines = $lines[0..41037]

# 2. Remove TOC entries for Q-Y (lines 83-91, 0-indexed: 82-90)
# 3. Remove master index entries for Q-Y (lines 425-433, 0-indexed: 424-432)
# We'll mark these indices for removal
$removeIndices = @()
for ($i = 82; $i -le 90; $i++) { $removeIndices += $i }  # TOC lines 83-91
for ($i = 424; $i -le 432; $i++) { $removeIndices += $i } # Master index lines 425-433

# 4. Replace cross-references to Appendix Q-Y with source document references
# Build new content
$newLines = @()
for ($i = 0; $i -lt $contentLines.Count; $i++) {
    if ($removeIndices -contains $i) { continue }
    
    $line = $contentLines[$i]
    
    # Replace Appendix Q references with AGENT_WORKSPACE.md
    $line = $line -replace '\[Appendix Q [^]]*\]\(#file-appendices-Appendix_Q_Project_Governance\)[^-]*', '`docs/AGENT_WORKSPACE.md`'
    $line = $line -replace '\[Appendix Q [^]]*\]\(#file-appendices-Appendix_Q_Project_Governance\)', '`docs/AGENT_WORKSPACE.md`'
    
    # Replace Appendix R references with DESIGN.md
    $line = $line -replace '\[Appendix R [^]]*\]\(#file-appendices-Appendix_R_Design_Rules\)[^`]*', '`docs/DESIGN.md`'
    $line = $line -replace '\[Appendix R [^]]*\]\(#file-appendices-Appendix_R_Design_Rules\)', '`docs/DESIGN.md`'
    
    # Replace Appendix S references with balance docs
    $line = $line -replace '\[Appendix S [^]]*\]\(#file-appendices-Appendix_S_Balance_System\)[^`]*', '`docs/design/FORMULA_V2.md` and `docs/design/ARMOR_SYSTEM.md`'
    $line = $line -replace '\[Appendix S [^]]*\]\(#file-appendices-Appendix_S_Balance_System\)', '`docs/design/FORMULA_V2.md` and `docs/design/ARMOR_SYSTEM.md`'
    
    # Replace Appendix T references with LESSONS_LEARNED.md
    $line = $line -replace '\[Appendix T [^]]*\]\(#file-appendices-Appendix_T_Lessons_Learned\)[^`]*', '`docs/LESSONS_LEARNED.md`'
    $line = $line -replace '\[Appendix T [^]]*\]\(#file-appendices-Appendix_T_Lessons_Learned\)', '`docs/LESSONS_LEARNED.md`'
    
    # Replace Appendix U references with MIGRATION.md
    $line = $line -replace '\[Appendix U [^]]*\]\(#file-appendices-Appendix_U_Migration\)[^`]*', '`docs/MIGRATION.md`'
    $line = $line -replace '\[Appendix U [^]]*\]\(#file-appendices-Appendix_U_Migration\)', '`docs/MIGRATION.md`'
    
    # Replace Appendix V references with audit/SUMMARY.md
    $line = $line -replace '\[Appendix V [^]]*\]\(#file-appendices-Appendix_V_Audit_Status\)[^`]*', '`docs/audit/SUMMARY.md`'
    $line = $line -replace '\[Appendix V [^]]*\]\(#file-appendices-Appendix_V_Audit_Status\)', '`docs/audit/SUMMARY.md`'
    
    # Replace Appendix W references with FACTIONS.md
    $line = $line -replace '\[Appendix W [^]]*\]\(#file-appendices-Appendix_W_Faction_Compendium\)[^`]*', '`docs/FACTIONS.md`'
    $line = $line -replace '\[Appendix W [^]]*\]\(#file-appendices-Appendix_W_Faction_Compendium\)', '`docs/FACTIONS.md`'
    
    # Replace Appendix X references with VISION.md
    $line = $line -replace '\[Appendix X [^]]*\]\(#file-appendices-Appendix_X_Vision\)[^`]*', '`docs/design/VISION.md`'
    $line = $line -replace '\[Appendix X [^]]*\]\(#file-appendices-Appendix_X_Vision\)', '`docs/design/VISION.md`'
    
    # Replace Appendix Y references with AI_AGENT_HANDOFF.md
    $line = $line -replace '\[Appendix Y [^]]*\]\(#file-appendices-Appendix_Y_Agent_Handoff\)[^`]*', '`docs/history/AI_AGENT_HANDOFF.md`'
    $line = $line -replace '\[Appendix Y [^]]*\]\(#file-appendices-Appendix_Y_Agent_Handoff\)', '`docs/history/AI_AGENT_HANDOFF.md`'
    
    $newLines += $line
}

# Write the file back
Set-Content $file -Value $newLines -Encoding UTF8

Write-Output "Done. New line count: $($newLines.Count)"
