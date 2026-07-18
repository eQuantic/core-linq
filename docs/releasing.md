# Releasing (maintainers)

Releases are fully automated by [semantic-release](https://semantic-release.gitbook.io/) — nobody
edits version numbers or tags by hand.

## How a release happens

1. Commits land on `main` (house style: `emoji type: description`, e.g. `✨ feat: …` — the
   [release.config.mjs](../release.config.mjs) parser accepts the gitmoji prefix).
2. [release.yml](../.github/workflows/release.yml) runs the full test matrix
   (ubuntu/windows × net8.0/net10.0); only if green does the release job start, inside the
   `nuget` GitHub environment (add required reviewers there for a manual approval gate).
3. semantic-release analyzes the commits since the last `v*` tag:

   | Commits since last tag contain | Next version |
   |-------------------------------|--------------|
   | `✨ feat!:` or a `BREAKING CHANGE:` footer | major |
   | `✨ feat:` | minor |
   | `🐛 fix:` / `⚡ perf:` | patch |
   | only `docs`, `chore`, `ci`, `test`, `refactor`, `style` | **no release** |

4. If a release is due, the pipeline — in order — updates `CHANGELOG.md`, stamps the version into
   `src/Directory.Build.props`, packs the five packages with `-p:Version` +
   `ContinuousIntegrationBuild`, commits those two files back
   (`🔧 chore: release vX.Y.Z [skip ci]`), pushes to NuGet.org, tags `vX.Y.Z` and creates the
   GitHub release with the `.nupkg`/`.snupkg` files attached.

## Version 3 baseline

`v2.0.0` is an annotated **baseline marker** (the actual v2 implementation lives on the
`version2` branch): semantic-release only counts from *stable* tags on a release branch —
prerelease tags are ignored there, which is why the marker isn't `3.0.0-preview.x`. The commit
`✨ feat!: eQuantic.Linq v3` right after the marker declares the breaking rewrite, so the first
automated release is exactly **`3.0.0`**, and normal feat/fix flow continues from there
(`3.0.1`, `3.1.0`, …).

## Arming and gating

Without the `NUGET_KEY` secret (org-level in eQuantic, or set on the `nuget` environment),
releases are **disarmed**: [release-verify.sh](../.github/scripts/release-verify.sh) fails the
pipeline *before* anything is tagged, committed or published. With the key available, every
qualifying push to `main` releases — the manual control is the `nuget` environment's
*required reviewers* (Settings → Environments → `nuget`): with a reviewer configured, each
release job pauses until someone approves it in the Actions UI.

## Channels

- `main` → stable releases (`3.1.0`).
- `preview` branch → prerelease channel: pushing there releases `X.Y.Z-preview.N` versions.

## Day-to-day rules

- Never push tags by hand; never edit `<Version>` in `Directory.Build.props` expecting it to be
  released — the pipeline overwrites it.
- Mark breaking changes explicitly: `✨ feat!: …` and/or a `BREAKING CHANGE:` paragraph in the
  body. That's the only thing that produces a major bump.
- A push that only contains non-releasing types simply results in "no release" — that's normal.
- Secrets/config: `NUGET_KEY` is the eQuantic org secret; `GITHUB_TOKEN` is the workflow's own.
