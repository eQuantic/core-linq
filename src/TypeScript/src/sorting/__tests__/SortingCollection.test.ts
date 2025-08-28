import { SortingCollection } from '../SortingCollection';
import { Sorting } from '../Sorting';
import { ISorting } from '../base';

interface TestUser {
  id: number;
  name: string;
  age: number;
  createdAt: Date;
}

describe('SortingCollection', () => {
  describe('Constructor', () => {
    it('should create empty collection', () => {
      const collection = new SortingCollection<TestUser>();
      
      expect(collection.length).toBe(0);
      expect([...collection]).toEqual([]);
    });

    it('should create collection with initial items', () => {
      const sorts = [
        new Sorting<TestUser>('name', 'asc'),
        new Sorting<TestUser>('age', 'desc')
      ];
      
      const collection = new SortingCollection<TestUser>(sorts);
      
      expect(collection.length).toBe(2);
      expect([...collection]).toEqual(sorts);
    });
  });

  describe('Array-like behavior', () => {
    let collection: SortingCollection<TestUser>;
    
    beforeEach(() => {
      collection = new SortingCollection<TestUser>();
    });

    it('should support push', () => {
      const sort = new Sorting<TestUser>('name', 'asc');
      collection.push(sort);
      
      expect(collection.length).toBe(1);
      expect(collection[0]).toBe(sort);
    });

    it('should support indexing', () => {
      const sort1 = new Sorting<TestUser>('name', 'asc');
      const sort2 = new Sorting<TestUser>('age', 'desc');
      
      collection.push(sort1, sort2);
      
      expect(collection[0]).toBe(sort1);
      expect(collection[1]).toBe(sort2);
    });

    it('should support forEach', () => {
      const sorts = [
        new Sorting<TestUser>('name', 'asc'),
        new Sorting<TestUser>('age', 'desc')
      ];
      
      collection.push(...sorts);
      
      const visited: ISorting<TestUser>[] = [];
      collection.forEach(sort => visited.push(sort));
      
      expect(visited).toEqual(sorts);
    });

    it('should support map', () => {
      const sorts = [
        new Sorting<TestUser>('name', 'asc'),
        new Sorting<TestUser>('age', 'desc')
      ];
      
      collection.push(...sorts);
      
      const columns = collection.map(sort => sort.column);
      
      expect(columns).toEqual(['name', 'age']);
    });

    it('should support filter', () => {
      const sorts = [
        new Sorting<TestUser>('name', 'asc'),
        new Sorting<TestUser>('age', 'desc'),
        new Sorting<TestUser>('createdAt', 'asc')
      ];
      
      collection.push(...sorts);
      
      const ascSorts = collection.filter(sort => sort.direction === 'asc');
      
      expect(ascSorts.length).toBe(2);
      expect(ascSorts[0].column).toBe('name');
      expect(ascSorts[1].column).toBe('createdAt');
    });

    it('should support find', () => {
      const nameSort = new Sorting<TestUser>('name', 'asc');
      const ageSort = new Sorting<TestUser>('age', 'desc');
      
      collection.push(nameSort, ageSort);
      
      const found = collection.find(sort => sort.column === 'age');
      
      expect(found).toBe(ageSort);
    });

    it('should support some', () => {
      const sorts = [
        new Sorting<TestUser>('name', 'asc'),
        new Sorting<TestUser>('age', 'desc')
      ];
      
      collection.push(...sorts);
      
      expect(collection.some(sort => sort.direction === 'desc')).toBe(true);
      expect(collection.some(sort => sort.column === ('notexistent' as any))).toBe(false);
    });

    it('should support every', () => {
      const sorts = [
        new Sorting<TestUser>('name', 'asc'),
        new Sorting<TestUser>('age', 'asc')
      ];
      
      collection.push(...sorts);
      
      expect(collection.every(sort => sort.direction === 'asc')).toBe(true);
      
      collection.push(new Sorting<TestUser>('createdAt', 'desc'));
      
      expect(collection.every(sort => sort.direction === 'asc')).toBe(false);
    });
  });

  describe('toString', () => {
    it('should format empty collection', () => {
      const collection = new SortingCollection<TestUser>();
      
      expect(collection.toString()).toBe('');
    });

    it('should format single sort', () => {
      const collection = new SortingCollection<TestUser>([
        new Sorting<TestUser>('name', 'asc')
      ]);
      
      expect(collection.toString()).toBe('name:asc');
    });

    it('should format multiple sorts', () => {
      const collection = new SortingCollection<TestUser>([
        new Sorting<TestUser>('name', 'asc'),
        new Sorting<TestUser>('age', 'desc'),
        new Sorting<TestUser>('createdAt', 'asc')
      ]);
      
      expect(collection.toString()).toBe('name:asc,age:desc,createdAt:asc');
    });
  });

  describe('Chaining Operations', () => {
    it('should support method chaining', () => {
      const collection = new SortingCollection<TestUser>([
        new Sorting<TestUser>('name', 'asc'),
        new Sorting<TestUser>('age', 'desc'),
        new Sorting<TestUser>('createdAt', 'asc')
      ]);
      
      const result = collection
        .filter(sort => sort.direction === 'asc')
        .map(sort => sort.column);
      
      expect(result).toEqual(['name', 'createdAt']);
    });
  });

  describe('Type Safety', () => {
    it('should maintain type safety throughout operations', () => {
      const collection = new SortingCollection<TestUser>();
      
      // These should all compile without errors
      collection.push(new Sorting<TestUser>('name', 'asc'));
      collection.push(new Sorting<TestUser>('age', 'desc'));
      
      const nameColumns = collection
        .filter(sort => sort.column === 'name')
        .map(sort => sort.column);
      
      expect(nameColumns).toEqual(['name']);
    });
  });
});