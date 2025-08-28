import { Filtering } from '../Filtering';

interface TestUser {
  id: number;
  name: string;
  email: string;
  age: number;
  isActive: boolean;
  createdAt: Date;
}

describe('Filtering', () => {
  describe('Constructor', () => {
    it('should create filtering with all parameters', () => {
      const filter = new Filtering<TestUser, 'name'>('name', 'John', 'eq');
      
      expect(filter.column).toBe('name');
      expect(filter.value).toBe('John');
      expect(filter.operator).toBe('eq');
    });

    it('should create filtering with default operator', () => {
      const filter = new Filtering<TestUser, 'age'>('age', 25);
      
      expect(filter.column).toBe('age');
      expect(filter.value).toBe(25);
      expect(filter.operator).toBe('eq');
    });

    it('should create empty filtering', () => {
      const filter = new Filtering<TestUser>();
      
      expect(filter.column).toBeNull();
      expect(filter.value).toBeNull();
      expect(filter.operator).toBe('eq');
    });
  });

  describe('toString', () => {
    it('should format string correctly', () => {
      const filter = new Filtering<TestUser, 'name'>('name', 'John', 'eq');
      
      expect(filter.toString()).toBe('name:eq(John)');
    });

    it('should format with different operators', () => {
      const filter = new Filtering<TestUser, 'age'>('age', 25, 'gt');
      
      expect(filter.toString()).toBe('age:gt(25)');
    });
  });

  describe('Static Factory Methods', () => {
    describe('eq', () => {
      it('should create equality filter for string', () => {
        const filter = Filtering.eq<TestUser, 'name'>('name', 'John');
        
        expect(filter.column).toBe('name');
        expect(filter.value).toBe('John');
        expect(filter.operator).toBe('eq');
      });

      it('should create equality filter for number', () => {
        const filter = Filtering.eq<TestUser, 'age'>('age', 25);
        
        expect(filter.column).toBe('age');
        expect(filter.value).toBe(25);
        expect(filter.operator).toBe('eq');
      });

      it('should create equality filter for boolean', () => {
        const filter = Filtering.eq<TestUser, 'isActive'>('isActive', true);
        
        expect(filter.column).toBe('isActive');
        expect(filter.value).toBe(true);
        expect(filter.operator).toBe('eq');
      });
    });

    describe('neq', () => {
      it('should create inequality filter', () => {
        const filter = Filtering.neq<TestUser, 'name'>('name', 'John');
        
        expect(filter.column).toBe('name');
        expect(filter.value).toBe('John');
        expect(filter.operator).toBe('neq');
      });
    });

    describe('gt', () => {
      it('should create greater than filter for number', () => {
        const filter = Filtering.gt<TestUser, 'age'>('age', 25);
        
        expect(filter.column).toBe('age');
        expect(filter.value).toBe(25);
        expect(filter.operator).toBe('gt');
      });

      it('should create greater than filter for date', () => {
        const date = new Date('2023-01-01');
        const filter = Filtering.gt<TestUser, 'createdAt'>('createdAt', date);
        
        expect(filter.column).toBe('createdAt');
        expect(filter.value).toBe(date);
        expect(filter.operator).toBe('gt');
      });
    });

    describe('lt', () => {
      it('should create less than filter for number', () => {
        const filter = Filtering.lt<TestUser, 'age'>('age', 30);
        
        expect(filter.column).toBe('age');
        expect(filter.value).toBe(30);
        expect(filter.operator).toBe('lt');
      });
    });

    describe('gte', () => {
      it('should create greater than or equal filter', () => {
        const filter = Filtering.gte<TestUser, 'age'>('age', 21);
        
        expect(filter.column).toBe('age');
        expect(filter.value).toBe(21);
        expect(filter.operator).toBe('gte');
      });
    });

    describe('lte', () => {
      it('should create less than or equal filter', () => {
        const filter = Filtering.lte<TestUser, 'age'>('age', 65);
        
        expect(filter.column).toBe('age');
        expect(filter.value).toBe(65);
        expect(filter.operator).toBe('lte');
      });
    });

    describe('ct', () => {
      it('should create contains filter for string', () => {
        const filter = Filtering.ct<TestUser, 'name'>('name', 'John');
        
        expect(filter.column).toBe('name');
        expect(filter.value).toBe('John');
        expect(filter.operator).toBe('ct');
      });
    });

    describe('sw', () => {
      it('should create starts with filter for string', () => {
        const filter = Filtering.sw<TestUser, 'name'>('name', 'Jo');
        
        expect(filter.column).toBe('name');
        expect(filter.value).toBe('Jo');
        expect(filter.operator).toBe('sw');
      });
    });

    describe('ew', () => {
      it('should create ends with filter for string', () => {
        const filter = Filtering.ew<TestUser, 'email'>('email', '@example.com');
        
        expect(filter.column).toBe('email');
        expect(filter.value).toBe('@example.com');
        expect(filter.operator).toBe('ew');
      });
    });

    describe('in', () => {
      it('should create in filter with multiple values', () => {
        const filter = Filtering.in<TestUser, 'age'>('age', 25, 30, 35);
        
        expect(filter.column).toBe('age');
        expect(filter.value).toEqual([25, 30, 35]);
        expect(filter.operator).toBe('in');
      });

      it('should create in filter with single value', () => {
        const filter = Filtering.in<TestUser, 'name'>('name', 'John');
        
        expect(filter.column).toBe('name');
        expect(filter.value).toEqual(['John']);
        expect(filter.operator).toBe('in');
      });
    });
  });

  describe('Type Safety', () => {
    it('should preserve type information through generics', () => {
      // This test verifies compile-time type safety
      const nameFilter = Filtering.eq<TestUser, 'name'>('name', 'John');
      const ageFilter = Filtering.gt<TestUser, 'age'>('age', 25);
      const activeFilter = Filtering.eq<TestUser, 'isActive'>('isActive', true);
      
      // These should compile without errors and maintain type safety
      expect(nameFilter.column).toBe('name');
      expect(ageFilter.column).toBe('age');
      expect(activeFilter.column).toBe('isActive');
    });
  });
});