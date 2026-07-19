# Changelog

## [3.6.0](https://github.com/eQuantic/core-linq/compare/v3.5.0...v3.6.0) (2026-07-19)

### Features

* fluent and/or clause folding in QueryFilterBuilder ([39b301a](https://github.com/eQuantic/core-linq/commit/39b301ac43b2271e9359fb43c161eb0ab909ba72))

## [3.5.0](https://github.com/eQuantic/core-linq/compare/v3.4.0...v3.5.0) (2026-07-19)

### Features

* revive eQuantic.Linq as the pure-core meta-package ([33e4f47](https://github.com/eQuantic/core-linq/commit/33e4f4724db18717f32a7d3a22c800bf58e855d8))

## [3.4.0](https://github.com/eQuantic/core-linq/compare/v3.3.0...v3.4.0) (2026-07-19)

### Features

* string-path overloads for the query builders ([041480d](https://github.com/eQuantic/core-linq/commit/041480d3e4cbde0a0dee30aa92e799f87ce8ca36))

## [3.3.0](https://github.com/eQuantic/core-linq/compare/v3.2.1...v3.3.0) (2026-07-19)

### Features

* code-first typed query-string builders ([27ecfe2](https://github.com/eQuantic/core-linq/commit/27ecfe2c2e412d9eca136f6ee16ec2c64c434991))

## [3.2.1](https://github.com/eQuantic/core-linq/compare/v3.2.0...v3.2.1) (2026-07-18)

### Bug Fixes

* query collections belong to the pure web package ([d913e45](https://github.com/eQuantic/core-linq/commit/d913e45700abe392dd47510ad16b56fd6b8dd6c0))

## [3.2.0](https://github.com/eQuantic/core-linq/compare/v3.1.0...v3.2.0) (2026-07-18)

### Features

* typed query collections for request envelopes ([a122822](https://github.com/eQuantic/core-linq/commit/a122822f75342c009568de4da72b379264831e8a))

## [3.1.0](https://github.com/eQuantic/core-linq/compare/v3.0.0...v3.1.0) (2026-07-18)

### Features

* OpenAPI documentation for the entity-query surface ([8d678b5](https://github.com/eQuantic/core-linq/commit/8d678b5b219232cf7f4349cb468855ac3462c3fc))

### Bug Fixes

* netstandard2.0 build of the documentation catalog ([d7cd8a1](https://github.com/eQuantic/core-linq/commit/d7cd8a19cb3a0738625f02ce25d83cfddd0bcc88))

## [3.0.0](https://github.com/eQuantic/core-linq/compare/v2.0.0...v3.0.0) (2026-07-18)

### ⚠ BREAKING CHANGES

* the v2 API surface (Filtering, Sorting, EntityFilter,
ExpressionBuilder) is replaced by the eQuantic.Linq.Expressions engine and
its satellite packages. The previous implementation is preserved on the
version2 branch; the new surface is documented in docs/.

### Features

* eQuantic.Linq v3 ([2a5c5c2](https://github.com/eQuantic/core-linq/commit/2a5c5c260cb379fec13d7dd6f951413d81c8ae92))

## [3.0.0-preview.1] - 2026-07-18

### Added

- Full-fidelity serialization of any .NET expression tree to a structured node model and JSON, and
  exact reconstruction back (84/85 `ExpressionType` values; `Dynamic` is unsupported by design).
- Portable type references: well-known aliases, core-library assembly omission, structural
  generics/arrays/by-ref and anonymous-type shapes re-materialized via `Reflection.Emit`.
- Partial evaluation (closure folding) with structural preservation of object creation, lambdas,
  quotes, statement trees and `Queryable` pipelines.
- `IQueryable` root detection and re-binding through `QueryRootProvider` — whole-query transport.
- Root-anchored typed model `ExpressionModel<TRoot>` with top-down type inference
  (`TypeInfoMode.Minimal`): lean, hand-writable payloads; case-insensitive member/method matching;
  instance-style extension method binding by generic unification.
- Security: `TypeResolutionOptions` with strict mode, known-type registration and
  assembly/namespace allow-lists.
- `ExpressionEqualityComparer`: structural expression equality with parameter/label mapping.
- Process-wide reflection caches, interpreter-based closure evaluation and compiled anonymous-type
  accessors.
- Complete `Queryable` surface (all 63 operators on .NET 10, including `LeftJoin`/`RightJoin`,
  `CountBy`, `AggregateBy`, `Shuffle`, `UnionBy`/`IntersectBy`/`ExceptBy`, `Order`/`OrderDescending`)
  with a mechanical acknowledgement test that fails when future .NET versions add new operators.
- Comparer preservation: `StringComparer.*`, `EqualityComparer<T>.Default` and `Comparer<T>.Default`
  serialize as portable static member accesses; `Index`/`Range` constants have dedicated JSON
  converters; user-defined conversion operators, by-ref parameter calls and full `DebugInfo`
  document metadata round-trip.
- 194 tests (net10.0) / 190 tests (net8.0) covering every `ExpressionType`, every `Queryable`
  operator as a full pipeline, closures, anonymous types, statement trees, JSON payload files and
  the inference layer.
