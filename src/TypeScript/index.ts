/**
 * Typed builder for the eQuantic.Linq query-string syntax.
 *
 *   f.and(f.gt('total', 100), f.any('items', f.gt('price', 50)))
 *   → "and(total:gt(100),items:any(price:gt(50)))"
 */
export type Filter = string & { readonly __filter?: unique symbol };

const quote = (v: unknown): string => {
  if (v === null || v === undefined) return 'null';
  const s = String(v);
  return /[,()|']/.test(s) ? `'${s.replace(/'/g, "''")}'` : s;
};

const op = (path: string, name: string, v: unknown): Filter => `${path}:${name}(${quote(v)})` as Filter;

export const f = {
  eq: (p: string, v: unknown) => op(p, 'eq', v),
  neq: (p: string, v: unknown) => op(p, 'neq', v),
  gt: (p: string, v: unknown) => op(p, 'gt', v),
  gte: (p: string, v: unknown) => op(p, 'gte', v),
  lt: (p: string, v: unknown) => op(p, 'lt', v),
  lte: (p: string, v: unknown) => op(p, 'lte', v),
  ct: (p: string, v: unknown) => op(p, 'ct', v),
  nct: (p: string, v: unknown) => op(p, 'nct', v),
  sw: (p: string, v: unknown) => op(p, 'sw', v),
  ew: (p: string, v: unknown) => op(p, 'ew', v),
  isNull: (p: string) => op(p, 'eq', null),
  notNull: (p: string) => op(p, 'neq', null),
  in: (p: string, vs: unknown[]) => `${p}:in(${vs.map(quote).join('|')})` as Filter,
  nin: (p: string, vs: unknown[]) => `${p}:nin(${vs.map(quote).join('|')})` as Filter,
  and: (...fs: Filter[]) => `and(${fs.join(',')})` as Filter,
  or: (...fs: Filter[]) => `or(${fs.join(',')})` as Filter,
  not: (...fs: Filter[]) => `not(${fs.join(',')})` as Filter,
  any: (p: string, ...fs: Filter[]) => `${p}:any(${fs.join(',')})` as Filter,
  all: (p: string, ...fs: Filter[]) => `${p}:all(${fs.join(',')})` as Filter,
};

export interface QueryOptions {
  filter?: Filter | Filter[];
  orderBy?: string | string[];
  skip?: number;
  take?: number;
  select?: string | string[];
}

/** Builds the full query string: buildQuery({ filter: f.gt('total', 100), orderBy: 'total:desc', take: 10 }) */
export const buildQuery = (q: QueryOptions): string => {
  const parts: string[] = [];
  const add = (key: string, value: string) => parts.push(`${key}=${encodeURIComponent(value)}`);
  for (const fl of [q.filter ?? []].flat()) add('filter', fl);
  const order = [q.orderBy ?? []].flat().join(',');
  if (order) add('orderBy', order);
  if (q.skip !== undefined) add('skip', String(q.skip));
  if (q.take !== undefined) add('take', String(q.take));
  const select = [q.select ?? []].flat().join(',');
  if (select) add('select', select);
  return parts.join('&');
};
