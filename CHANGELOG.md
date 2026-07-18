# Changelog

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
