# ShaderStripMod 1.31.1 — release verification

Date: 2026-11-07  
Status: testing pre-release; full live gameplay validation has not been performed.

## Repository

- Annotated tag `v1.31.1` points at commit `f0cb1b3` on `main`; the source, tests and release documents are committed.
- Remote: `https://github.com/flyzeen-hk/ShaderStripMod.git`.
- GitHub release: `https://github.com/flyzeen-hk/ShaderStripMod/releases/tag/v1.31.1`, marked as a pre-release.
- Assets: `ShaderStripMod-1.31.1.zip`, its `.sha256.txt`, `RELEASE_NOTES_1.31.1.md` and this document.
- `v1.29.30` remains the normal release marked Latest.

## Artifacts

SHA-256:

- `ShaderStripMod-1.31.1.zip`: `29abe6ccc3694fb70ee302525fdeb7b1fde2c8dda1fe64dbc554aab3a2e8ee67`
- `ShaderStripMod.dll`: `da741bd04dadc322a9d4857b50279b86b9799b5967113412bdbcc9f7df205183`
- `ShaderStripMod.pdb` (not included in the ZIP): `60ac225e60a90a2162f55dffe938056e4516b70fc4a7a4476610d57bb91987d4`

ProductVersion: 1.31.1; AssemblyVersion/FileVersion: 1.31.1.0.

The ZIP was checked before publishing: it contains exactly `ShaderStripMod.dll`, `README.md`, `CHANGELOG.md` and `LICENSE`, with no settings, logs, media, game assets or backups. The build and installed DLL hashes matched at installation time.

## Build and automatic checks

Results from the 2026-11-02 preparation build:

- Release: 0 warnings, 0 errors.
- Eight projects: 159,612 assertions:
  - BrowserInputGateChecks: 39
  - CameraPinChecks: 249
  - MenuBackgroundDecorationsChecks: 23
  - MenuMediaChecks: 146,058
  - MenuResourcesChecks: 25
  - PersistenceChecks: 24
  - RegressionChecks: 4,570
  - TexturePreviewChecks: 8,624

The tests were not rerun when the Git history and release were created. Most assertions are pixel comparisons; they do not replace a live gameplay test.

## Backups

- Source and installed 1.31.0: `backups-history/backup_20261102_000946_v1.31.0_pre_1.31.1_audit_fixes/` and `backups-history/backup_20261102_000946_v1.31.0_installed_pre_1.31.1/`.
- Pre-install DLL/PDB: `ShaderStripMod.dll.bak_before_v1.31.1_20261102_002459` and `ShaderStripMod.pdb.bak_before_v1.31.1_20261102_002459`.

## Limitations

The first launch completed without serious errors, but many checks were not performed. Camera pin, room transitions, death, cutscenes, capture-mode switching, preset apply/backup failure handling, narrow F9, collage rendering and resizing, audio and composition autosave remain unverified in a real gameplay session. Until those pass, 1.31.1 is a testing pre-release.
