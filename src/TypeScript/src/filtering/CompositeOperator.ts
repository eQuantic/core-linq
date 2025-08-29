/**
 * Defines the composite operators available for combining multiple filtering conditions.
 * 
 * - `and`: Combines conditions using logical AND operation. All conditions must be true.
 * - `or`: Combines conditions using logical OR operation. At least one condition must be true.
 * - `any`: Applies to collection properties. Returns true if any item in the collection matches the specified conditions.
 * - `all`: Applies to collection properties. Returns true if all items in the collection match the specified conditions.
 */
export type CompositeOperator = 'and' | 'or' | 'any' | 'all';
