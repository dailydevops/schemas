# Development workflow

This page states how work is done in this repository. It applies to every contributor and every agent. Commit messages follow [Conventional Commits 1.0.0](https://www.conventionalcommits.org/en/v1.0.0/). Code and documentation are written in American English.

## Test-first workflow

A failing test exists before the implementation.

1. Write the test with TUnit. It states the behavior the work package promises.
2. Run it and confirm that it fails for the expected reason. Commit it on its own (`test(<scope>): ...`).
3. Implement until the test is green. Commit the implementation separately (`feat(<scope>): ...`).

No implementation is merged without a confirming test. Where no test is possible, the issue names the evidence to deliver instead. A test must run green on every leg: `net9.0` (runs the `netstandard2.0` build), `net10.0`, and `net11.0`.

```shell
dotnet test --solution Schemas.slnx -c Release -f net9.0
```

## Bug handling

A defect found on the way is not fixed inside the pull request that found it.

1. Open a separate bug issue and label it `type:bug` and `state:investigate`.
2. Commit a red test that reproduces the defect, as its own commit (`test(<scope>): ...`). The test fails on the unfixed code.
3. Commit the targeted fix after it (`fix(<scope>): ...`). The fix is as narrow as the cause allows and turns the red test green.

The commit order is always red test first, then fix. The fix pull request closes the bug issue.

## Known-failure baselines of the conformance runners

A conformance run compares its results against a committed baseline: a plain text file with one case id per line in ascending ordinal order, one `<test set>.txt` file per test set. Because every test set has its own file, parallel work packages edit different files.

The comparison reports two things separately, and both break the build:

- a regression: a case that fails and is not in the baseline;
- an unexpected pass: a case that passes but is in the baseline.

A baseline file may be edited only for one of these reasons:

- an unexpected pass is reported: remove the id of the case, because the baseline only shrinks when behavior improves;
- a case fails for a known cause that is tracked in an issue, and the exit rule allows the exclusion: add the id and state the reason and the issue in the pull request.

A baseline is never edited to make a regression disappear. Fix the regression instead. Do not edit the file by hand when a regenerated file is available: regenerate it with the baseline writer, which produces the stable sort order. The maintainer approves every baseline edit in the pull request review. The edit is its own commit so that the review sees it in isolation.

## One pull request per work package

Every work package is delivered in one small pull request.

- The pull request title equals the issue title and follows Conventional Commits. The body starts with `Closes #<number>` and lists evidence for each acceptance criterion.
- Shared files are changed only when the work package cannot be done otherwise. The change is minimal and named in the pull request body.
- Work left out on purpose becomes its own issue labeled `state:onhold` and `state:help wanted`. Only existing labels are used.
- Open questions are clarified with the maintainer at once, with research and answer options. Nothing is implemented on an assumption.

### Owned paths

Each work package lists its owned paths in the issue. Only these paths may be changed by the pull request. Work packages of the same wave never share a path, which is what lets them run in parallel without merge conflicts. A change outside the owned paths needs a follow-up issue that carries the path, or a named exception in the pull request body.

## Where design lives

Design is an approved proposal in the design issue (type `type:architectural runway`), not a file in the repository. The proposal lists its approval points with options, consequences, and a recommendation. The maintainer approves it in the issue, and the decision is recorded there in a comment. Dependent work starts after the approval.

Do not add ADR files, a `docs/adr` directory, or a `decisions` directory. A deviation from an approved decision is raised in the design issue, not decided in a pull request.

## Target framework leg of the conformance suites

A conformance result does not depend on the target framework, so the conformance workflow runs each suite on one leg, `net10.0`, and not on every leg of the test projects. The suites are explicit (see below), so no other leg runs them, and the rate table appears once per package in the job summary.

The conformance workflow runs on pull requests that target `main` and on demand (`workflow_dispatch`). It does not run per push to `main`, so the suites run once per change. The conformance of `main` is checked through the pull request runs, or by starting the workflow manually with `workflow_dispatch` on `main`.

- To compare another leg locally, run the category filter with `--framework`, for example `--framework net9.0`.
- `net9.0` runs the `netstandard2.0` build of the libraries. A run on `net9.0` passes every baseline gate of QT3, XSTS, XML Conformance and Schematron, so the failure sets equal those of `net10.0`. A second leg is added only when a conformance result is shown to differ between builds.

### Explicit conformance suites

The suite classes also carry `[Explicit]` and `[Category(ConformanceCategory.Name)]` (category `Conformance`, defined once in `tests/NetEvolve.Schemas.Conformance`). TUnit excludes explicit tests from a run that selects any non-explicit test, so the main CI test run, which takes no filter, executes none of the suites, and `conformance.yml` is the only place that runs them. See <https://tunit.dev/docs/writing-tests/explicit/#combining-explicit-with-category>.

- The conformance workflow selects the suites by category: `--treenode-filter "/*/*/*/*[Category=Conformance]"`. A filter such as `/*/*Conformance*/*/*` also selects non-explicit tests, so TUnit would drop the explicit ones from that run. Do not use a namespace or class-name filter for the suites.
- Because a conformance result does not depend on the target framework, the workflow matrix runs the `net10.0` leg only.
- A new suite class carries `[Explicit]` and `[Category(ConformanceCategory.Name)]`.

## Benchmarks run after the merge

The Benchmarks workflow (`.github/workflows/benchmarks.yml`) does not run on pull requests. A run takes about 50 minutes, and with many open pull requests it saturates the hosted CI runners and delays every other check. The benchmark gate is not a required status check of the `main` ruleset, so it does not need to finish before the merge.

- It runs on every push to `main` that touches `benchmarks/**`, `src/**`, or the workflow file.
- It runs nightly (`schedule`) and on demand (`workflow_dispatch`).
- To benchmark a pull request that changes benchmarks or hot paths, start the workflow manually with `workflow_dispatch` on the branch of the pull request. There is no label for this.
- The concurrency group is per ref, so a newer run on `main` cancels an older one, and runs of other refs are not affected.
- A red run on `main` stays visible as a failed workflow run. A follow-up pull request fixes it.
