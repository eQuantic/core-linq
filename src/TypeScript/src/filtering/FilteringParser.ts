import QueryString from 'qs';
import {flatten} from 'flat';
import { Filtering } from './Filtering.js';
import { FilteringCollection } from './FilteringCollection.js';
import type { FieldPath, FieldPathValue } from '../base.js';
import { OrFiltering } from './OrFiltering.js';
import { CompositeFiltering } from './CompositeFiltering.js';

import type { IFiltering, IFilteringInfo } from './base.js';
import { parseExpression } from './base.js';
import { CompositeFilteringParser } from './CompositeFilteringParser.js';

// Type utilities for safe conversion between source and destination types
type SafeFieldConversion<TSource extends object, TDestination extends object, TSourceColumn extends FieldPath<TSource>> = 
  TSourceColumn extends FieldPath<TDestination> 
    ? TSourceColumn 
    : FieldPath<TDestination>;

type SafeValueConversion<
  TSource extends object, 
  TDestination extends object,
  TSourceColumn extends FieldPath<TSource>,
  TDestinationColumn extends FieldPath<TDestination>
> = FieldPathValue<TDestination, TDestinationColumn>;

export interface IFilteringParserOptions<TData extends object = any, TDestination extends object = TData> {
  consideringNullValues?: boolean;
  consideringEmptyValues?: boolean;
  parseProperty?: (prop: FieldPath<TData>) => IFilteringInfo<TDestination>;
}

export interface IFilteringConverterOptions<TSource extends object = any, TDestination extends object = TSource> {
  parseProperty: (prop: FieldPath<TSource>) => FieldPath<TDestination>;
}

export class FilteringParser {
  public static parseQueryString<TData extends object = any>(
    queryFilter?: string | string[] | QueryString.ParsedQs | QueryString.ParsedQs[],
  ): FilteringCollection<TData> {
    const result = new FilteringCollection<TData>();

    if (!queryFilter) return result;

    if (!Array.isArray(queryFilter)) queryFilter = [queryFilter.toString()];

    for (const k in queryFilter) {
      if (!k) continue;

      const f = queryFilter[k];
      const exp = f.toString();

      if (CompositeFilteringParser.parse(exp, result)) continue;

      FilteringParser.parseExpression(exp, result);
    }
    return result;
  }

  public static parseObject<TData extends object = any, TDestination extends object = TData>(
    data: TData,
    options?: IFilteringParserOptions<TData, TDestination>,
  ): FilteringCollection<TDestination> {
    const collection = new FilteringCollection<TDestination>();
    if (!data) return collection;
    const flattenObj: any = flatten(data, { safe: true });
    for (const key in flattenObj) {
      if (!key) continue;

      let value = flattenObj[key];
      const info: IFilteringInfo<TDestination> = options?.parseProperty?.(key as unknown as FieldPath<TData>) || {
        column: key as unknown as FieldPath<TDestination>,
        operator: 'eq',
      };

      if (Array.isArray(value)) {
        if (value.length > 1) {
          const composite = new OrFiltering<TDestination>();
          for (const idx in value) {
            if (FilteringParser.validateValue(value[idx], options))
              composite.addValue(new Filtering<TDestination, FieldPath<TDestination>>(info.column, value[idx], info.operator));
          }
          collection.push(composite);
          continue;
        }
        value = value[0];
      }
      if (FilteringParser.validateValue(value, options))
        collection.push(new Filtering<TDestination, FieldPath<TDestination>>(info.column, value, info.operator));
    }

    return collection;
  }

  public static parse<
    TSource extends object, 
    TDestination extends object,
    TSourceColumn extends FieldPath<TSource> = FieldPath<TSource>
  >(
    source: IFiltering<TSource, TSourceColumn>,
    options?: IFilteringConverterOptions<TSource, TDestination>,
  ): IFiltering<TDestination, FieldPath<TDestination>> {
    if (source instanceof CompositeFiltering) {
      return new CompositeFiltering<TDestination>(
        source.compositeOperator,
        source.values.map((value) => FilteringParser.parse(value as IFiltering<TSource, FieldPath<TSource>>, options)),
      );
    }
    
    const convertedColumn = options?.parseProperty(source.column) || (source.column as unknown as FieldPath<TDestination>);
    const convertedValue = source.value as unknown as FieldPathValue<TDestination, typeof convertedColumn>;
    
    return new Filtering<TDestination, typeof convertedColumn>(
      convertedColumn, 
      convertedValue, 
      source.operator
    );
  }

  public static parseCollection<TSource extends object, TDestination extends object>(
    source: FilteringCollection<TSource>,
    options?: IFilteringConverterOptions<TSource, TDestination>,
  ): FilteringCollection<TDestination> {
    return new FilteringCollection<TDestination>(source.map((s) => FilteringParser.parse(s, options)));
  }

  public static parseExpression<TData extends object = any>(exp: string, result: IFiltering<TData>[]): void {
    const f = parseExpression<TData>(exp);
    const filtering = new Filtering<TData>(f.column, f.value, f.operator);
    result.push(filtering);
  }

  public static toQueryString<TSource extends object>(filtering: IFiltering<TSource>[]): string | undefined {
    if (!filtering || filtering.length === 0) return undefined;

    const newFiltering = FilteringParser.parseCollection<TSource, TSource>(new FilteringCollection<TSource>(filtering));

    return newFiltering.length > 1
      ? new CompositeFiltering<TSource>('and', newFiltering).toString()
      : newFiltering[0].toString();
  }

  private static validateValue(value: any, options?: IFilteringParserOptions) {
    if (value == null) return options?.consideringNullValues ? true : false;
    if (value == '') return options?.consideringEmptyValues ? true : false;
    return true;
  }
}
