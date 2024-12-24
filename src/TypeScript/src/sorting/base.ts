import { FieldPath } from '../base';

export type SortingDirection = 'asc' | 'desc';

export interface ISorting<T extends object> {
  column: FieldPath<T>;
  direction?: SortingDirection;
}
