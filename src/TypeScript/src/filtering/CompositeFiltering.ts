import { CompositeFilteringParser } from './CompositeFilteringParser';
import { CompositeOperator } from './CompositeOperator';
import { Filtering } from './Filtering';
import { IFiltering, parseExpression } from './base';

export class CompositeFiltering<TData extends object = any> extends Filtering<TData> {
  values: IFiltering<TData>[];
  compositeOperator: CompositeOperator;

  constructor(compositeOperator: CompositeOperator, values?: IFiltering<TData>[] | string[]);
  constructor(...args: any[]) {
    super();
    this.compositeOperator = args.length > 0 ? args[0] : 'and';
    this.values = [];
    if (args?.length > 1 && Array.isArray(args[1])) {
      args[1].forEach((value) => {
        if (typeof value === 'string') {
          if (!CompositeFilteringParser.parse(value, this.values)) {
            const parsed = parseExpression<TData>(value);
            const f = new Filtering<TData>(parsed.column, parsed.value, parsed.operator);
            this.values.push(f);
          }
        } else this.values.push(value);
      });
    }
  }

  addValue(filtering: IFiltering<TData>) {
    this.values.push(filtering);
  }

  toString(): string {
    if (this.values.length == 0) return super.toString();
    const args = this.values.map((v) => v.toString());
    return `${this.compositeOperator}(${args.join(',')})`;
  }

  public static and<TData extends object>(...values: IFiltering<TData>[] | string[]): CompositeFiltering<TData> {
    return new CompositeFiltering<TData>('and', values);
  }

  public static or<TData extends object>(...values: IFiltering<TData>[] | string[]): CompositeFiltering<TData> {
    return new CompositeFiltering<TData>('or', values);
  }
}

export const CF = CompositeFiltering;
