import { CompositeFiltering } from './CompositeFiltering';
import type { IFiltering } from './base';

export class OrFiltering<TData extends object = any> extends CompositeFiltering<TData> {
  constructor(values?: IFiltering<TData>[] | string[]);
  constructor(...args: any[]) {
    super('or', args?.length > 0 ? args[0] : []);
  }
}
