import { CompositeFiltering } from './CompositeFiltering.js';
import { IFiltering, parseComposite } from './base.js';

export class CompositeFilteringParser {
  public static parse<TData extends object = any>(exp: string, result: IFiltering<TData>[]): boolean {
    const composite = parseComposite(exp, result);
    if (composite === null) return false;
    result.push(new CompositeFiltering(composite.compositeOperator, composite.values));
    return true;
  }
}
