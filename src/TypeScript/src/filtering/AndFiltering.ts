import { CompositeFiltering } from './CompositeFiltering.js';
import { IFiltering } from './base.js';

export class AndFiltering<TData extends object = any> extends CompositeFiltering<TData> {
  constructor(values?: IFiltering<TData>[] | string[]);
  constructor(...args: any[]) {
    super('and', args?.length > 0 ? args[0] : []);
  }
}
