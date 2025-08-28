import { FieldPath } from '../base.js';
import { ISorting, SortingDirection } from './base.js';

export class Sorting<TData extends object> implements ISorting<TData> {
  constructor(public column: FieldPath<TData>, public direction: SortingDirection = 'asc') {}
  toString(): string {
    return `${this.column.toString()}:${this.direction}`;
  }

  public static asc<TData extends object>(column: FieldPath<TData>): ISorting<TData> {
    return new Sorting<TData>(column, 'asc');
  }

  public static desc<TData extends object>(column: FieldPath<TData>): ISorting<TData> {
    return new Sorting<TData>(column, 'desc');
  }
}

export const S = Sorting;
