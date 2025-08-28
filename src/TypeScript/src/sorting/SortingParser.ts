import { FieldPath } from '../base.js';
import { ISorting, SortingDirection } from './base.js';
import { Sorting } from './Sorting.js';
import { SortingCollection } from './SortingCollection.js';

export class SortingParser {
  public static parse<TData extends object = any>(querySorter?: string | string[]): SortingCollection<TData> {
    const result = new SortingCollection<TData>();

    if (!querySorter) {
      return result;
    }

    if (!Array.isArray(querySorter)) {
      querySorter = [querySorter];
    }

    for (const s of querySorter) {
      if (!s) {
        continue;
      }

      const arr = s.split(':');
      const dir: SortingDirection = arr.length === 1 ? 'asc' : (arr[1].toLowerCase() as SortingDirection);

      result.push(new Sorting<TData>(arr[0] as FieldPath<TData>, dir));
    }
    return result;
  }

  public static toQueryString<TSource extends object>(sorting: ISorting<TSource>[]): string | undefined {
    if (!sorting || sorting.length === 0) return undefined;

    const newSorting = new SortingCollection<TSource>(sorting.map((s) => new Sorting<TSource>(s.column, s.direction)));

    return newSorting.join(',');
  }
}
