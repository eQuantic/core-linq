import type {
  AbstractType,
  Constructor,
  Primitive,
  BrowserNativeObject,
  FieldValues,
  FieldPath,
  IsEqual,
  IsTuple,
  IsAny,
  ArrayKey,
  TupleKeys,
  Path,
  ArrayPath,
  FieldArrayPath,
  PathValue,
  FieldPathValue,
  FieldArrayPathValue,
  FieldPathValues,
  FieldPathByValue,
  IndexedObject,
  Serializable,
} from './base.js';

import { splitArguments } from './funcs/index.js';

import { AndFiltering } from './filtering/AndFiltering.js';
import { CF, CompositeFiltering } from './filtering/CompositeFiltering.js';
import { CompositeFilteringParser } from './filtering/CompositeFilteringParser.js';
import { F, Filtering } from './filtering/Filtering.js';
import { FilteringCollection } from './filtering/FilteringCollection.js';
import { FilteringParser } from './filtering/FilteringParser.js';
import { OrFiltering } from './filtering/OrFiltering.js';

import type { CompositeOperator } from './filtering/CompositeOperator.js';
import type { FilterOperator, IFiltering, IFilteringInfo } from './filtering/base.js';
import type { IFilteringConverterOptions, IFilteringParserOptions } from './filtering/FilteringParser.js';

import type { SortingDirection, ISorting } from './sorting/base.js';
import { S, Sorting } from './sorting/Sorting.js';
import { SortingCollection } from './sorting/SortingCollection.js';
import { SortingParser } from './sorting/SortingParser.js';

// types and interfaces
export type {
  AbstractType,
  Constructor,
  Primitive,
  BrowserNativeObject,
  FieldValues,
  FieldPath,
  IsEqual,
  IsTuple,
  IsAny,
  ArrayKey,
  TupleKeys,
  Path,
  ArrayPath,
  FieldArrayPath,
  PathValue,
  FieldPathValue,
  FieldArrayPathValue,
  FieldPathValues,
  FieldPathByValue,
  IndexedObject,
  Serializable,
};

// functions
export { splitArguments };

// filtering
export type {
  CompositeOperator,
  FilterOperator,
  IFiltering,
  IFilteringConverterOptions,
  IFilteringInfo,
  IFilteringParserOptions,
};

export {
  AndFiltering,
  CF,
  CompositeFiltering,
  CompositeFilteringParser,
  F,
  Filtering,
  FilteringCollection,
  FilteringParser,
  OrFiltering,
};

// sorting
export type { SortingDirection, ISorting };
export { S, Sorting, SortingCollection, SortingParser };
