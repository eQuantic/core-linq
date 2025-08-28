import { Sorting } from '../Sorting';
import { SortingDirection } from '../base';

interface TestUser {
  id: number;
  name: string;
  email: string;
  age: number;
  isActive: boolean;
  createdAt: Date;
}

describe('Sorting', () => {
  describe('Constructor', () => {
    it('should create sorting with all parameters', () => {
      const sort = new Sorting<TestUser>('name', 'asc');
      
      expect(sort.column).toBe('name');
      expect(sort.direction).toBe('asc');
    });

    it('should create sorting with default direction', () => {
      const sort = new Sorting<TestUser>('age');
      
      expect(sort.column).toBe('age');
      expect(sort.direction).toBe('asc');
    });

    it('should create sorting with null column', () => {
      const sort = new Sorting<TestUser>(null as any);
      
      expect(sort.column).toBeNull();
      expect(sort.direction).toBe('asc');
    });
  });

  describe('toString', () => {
    it('should format ascending sort correctly', () => {
      const sort = new Sorting<TestUser>('name', 'asc');
      
      expect(sort.toString()).toBe('name:asc');
    });

    it('should format descending sort correctly', () => {
      const sort = new Sorting<TestUser>('age', 'desc');
      
      expect(sort.toString()).toBe('age:desc');
    });
  });

  describe('Static Factory Methods', () => {
    describe('asc', () => {
      it('should create ascending sort for string column', () => {
        const sort = Sorting.asc<TestUser>('name');
        
        expect(sort.column).toBe('name');
        expect(sort.direction).toBe('asc');
      });

      it('should create ascending sort for number column', () => {
        const sort = Sorting.asc<TestUser>('age');
        
        expect(sort.column).toBe('age');
        expect(sort.direction).toBe('asc');
      });

      it('should create ascending sort for date column', () => {
        const sort = Sorting.asc<TestUser>('createdAt');
        
        expect(sort.column).toBe('createdAt');
        expect(sort.direction).toBe('asc');
      });
    });

    describe('desc', () => {
      it('should create descending sort for string column', () => {
        const sort = Sorting.desc<TestUser>('name');
        
        expect(sort.column).toBe('name');
        expect(sort.direction).toBe('desc');
      });

      it('should create descending sort for number column', () => {
        const sort = Sorting.desc<TestUser>('age');
        
        expect(sort.column).toBe('age');
        expect(sort.direction).toBe('desc');
      });

      it('should create descending sort for boolean column', () => {
        const sort = Sorting.desc<TestUser>('isActive');
        
        expect(sort.column).toBe('isActive');
        expect(sort.direction).toBe('desc');
      });
    });
  });

  describe('Type Safety', () => {
    it('should preserve type information through generics', () => {
      // This test verifies compile-time type safety
      const nameSort = Sorting.asc<TestUser>('name');
      const ageSort = Sorting.desc<TestUser>('age');
      const activeSort = Sorting.asc<TestUser>('isActive');
      
      // These should compile without errors and maintain type safety
      expect(nameSort.column).toBe('name');
      expect(ageSort.column).toBe('age');
      expect(activeSort.column).toBe('isActive');
      
      expect(nameSort.direction).toBe('asc');
      expect(ageSort.direction).toBe('desc');
      expect(activeSort.direction).toBe('asc');
    });
  });

  describe('Direction Validation', () => {
    it('should accept valid sort directions', () => {
      const ascSort = new Sorting<TestUser>('name', 'asc');
      const descSort = new Sorting<TestUser>('name', 'desc');
      
      expect(ascSort.direction).toBe('asc');
      expect(descSort.direction).toBe('desc');
    });
  });

  describe('Nested Property Sorting', () => {
    interface TestUserWithProfile {
      id: number;
      profile: {
        firstName: string;
        lastName: string;
        settings: {
          theme: string;
        };
      };
    }

    it('should handle nested property sorting', () => {
      const sort = Sorting.asc<TestUserWithProfile>('profile.firstName');
      
      expect(sort.column).toBe('profile.firstName');
      expect(sort.direction).toBe('asc');
    });

    it('should handle deeply nested property sorting', () => {
      const sort = Sorting.desc<TestUserWithProfile>('profile.settings.theme');
      
      expect(sort.column).toBe('profile.settings.theme');
      expect(sort.direction).toBe('desc');
    });
  });
});