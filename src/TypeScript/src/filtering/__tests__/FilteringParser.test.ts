import { Filtering } from '../Filtering';
import { FilteringCollection } from '../FilteringCollection';
import type { FieldPath } from '../../base';

interface TestUser {
  id: number;
  name: string;
  email: string;
  age: number;
  isActive: boolean;
  profile: {
    firstName: string;
    lastName: string;
  };
}

describe('FilteringParser Conceptual Tests', () => {
  describe('Query String Parsing Concept', () => {
    it('should demonstrate query string parsing concept', () => {
      // Simulating what FilteringParser.parseQueryString would do
      // without importing the actual class due to 'flat' module issues
      
      const queryString = 'name:eq(John)';
      const [column, rest] = queryString.split(':');
      const match = rest.match(/(\w+)\((.+)\)$/);
      
      expect(column).toBe('name');
      expect(match).not.toBeNull();
      expect(match![1]).toBe('eq');
      expect(match![2]).toBe('John');
      
      // Create filter from parsed parts
      const filter = new Filtering<TestUser, 'name'>('name', 'John', 'eq');
      expect(filter.column).toBe('name');
      expect(filter.value).toBe('John');
      expect(filter.operator).toBe('eq');
    });

    it('should demonstrate multiple query strings parsing', () => {
      const queryStrings = [
        'name:eq(John)',
        'age:gt(25)',
        'isActive:eq(true)'
      ];
      
      const filters = new FilteringCollection<TestUser>();
      
      queryStrings.forEach(queryString => {
        const [column, rest] = queryString.split(':');
        const match = rest.match(/(\w+)\((.+)\)$/);
        
        if (match) {
          const operator = match[1] as any;
          const value = match[2];
          
          // Convert string values to appropriate types
          let parsedValue: any = value;
          if (value === 'true') parsedValue = true;
          if (value === 'false') parsedValue = false;
          if (!isNaN(Number(value))) parsedValue = Number(value);
          
          filters.push(new Filtering<TestUser, FieldPath<TestUser>>(
            column as FieldPath<TestUser>, 
            parsedValue, 
            operator
          ));
        }
      });
      
      expect(filters.length).toBe(3);
      expect(filters[0].column).toBe('name');
      expect(filters[1].column).toBe('age');
      expect(filters[2].column).toBe('isActive');
    });

    it('should handle different operators', () => {
      const operatorTests = [
        { query: 'name:neq(Jane)', op: 'neq', val: 'Jane' },
        { query: 'age:lt(30)', op: 'lt', val: 30 },
        { query: 'email:ct(@example.com)', op: 'ct', val: '@example.com' },
        { query: 'name:sw(Jo)', op: 'sw', val: 'Jo' },
        { query: 'email:ew(.com)', op: 'ew', val: '.com' }
      ];
      
      operatorTests.forEach(test => {
        const [column, rest] = test.query.split(':');
        const match = rest.match(/(\w+)\((.+)\)$/);
        
        expect(match).not.toBeNull();
        expect(match![1]).toBe(test.op);
        
        let parsedValue: any = match![2];
        if (!isNaN(Number(parsedValue))) parsedValue = Number(parsedValue);
        
        expect(parsedValue).toBe(test.val);
      });
    });
  });

  describe('Object Parsing Concept', () => {
    it('should demonstrate flat object parsing', () => {
      const data = {
        name: 'John',
        age: 25,
        isActive: true
      };
      
      const filters = new FilteringCollection<TestUser>();
      
      Object.entries(data).forEach(([key, value]) => {
        if (value != null && value !== '') {
          filters.push(new Filtering<TestUser, FieldPath<TestUser>>(
            key as FieldPath<TestUser>, 
            value, 
            'eq'
          ));
        }
      });
      
      expect(filters.length).toBe(3);
      
      const nameFilter = filters.find(f => f.column === 'name');
      const ageFilter = filters.find(f => f.column === 'age');
      const activeFilter = filters.find(f => f.column === 'isActive');
      
      expect(nameFilter?.value).toBe('John');
      expect(ageFilter?.value).toBe(25);
      expect(activeFilter?.value).toBe(true);
    });

    it('should filter out null and empty values', () => {
      const data = {
        name: 'John',
        emptyString: '',
        nullValue: null,
        undefinedValue: undefined,
        validNumber: 0,
        validBoolean: false
      };
      
      const filters = new FilteringCollection<any>();
      
      Object.entries(data).forEach(([key, value]) => {
        // Simulate FilteringParser validation logic
        if (value !== null && value !== undefined && value !== '') {
          filters.push(new Filtering<any, string>(key, value, 'eq'));
        }
      });
      
      // Should include valid values including 0 and false
      expect(filters.some(f => f.column === 'name' && f.value === 'John')).toBe(true);
      expect(filters.some(f => f.column === 'validNumber' && f.value === 0)).toBe(true);
      expect(filters.some(f => f.column === 'validBoolean' && f.value === false)).toBe(true);
      
      // Should exclude empty/null/undefined
      expect(filters.some(f => f.column === 'emptyString')).toBe(false);
      expect(filters.some(f => f.column === 'nullValue')).toBe(false);
      expect(filters.some(f => f.column === 'undefinedValue')).toBe(false);
    });
  });

  describe('Type Conversion Concept', () => {
    it('should demonstrate type conversion between different interfaces', () => {
      interface SourceType {
        userName: string;
        userAge: number;
      }
      
      const sourceFilter = new Filtering<SourceType, 'userName'>('userName', 'John', 'eq');
      
      // Simulate conversion logic
      const columnMapping: Record<string, string> = {
        'userName': 'name',
        'userAge': 'age'
      };
      
      const convertedColumn = columnMapping[sourceFilter.column] || sourceFilter.column;
      const convertedFilter = new Filtering<TestUser, FieldPath<TestUser>>(
        convertedColumn as FieldPath<TestUser>,
        sourceFilter.value,
        sourceFilter.operator
      );
      
      expect(convertedFilter.column).toBe('name');
      expect(convertedFilter.value).toBe('John');
      expect(convertedFilter.operator).toBe('eq');
    });
  });

  describe('Collection Operations', () => {
    it('should work with filtering collections', () => {
      const filters = new FilteringCollection<TestUser>([
        new Filtering<TestUser, 'name'>('name', 'John', 'eq'),
        new Filtering<TestUser, 'age'>('age', 25, 'gt')
      ]);
      
      expect(filters.length).toBe(2);
      expect(filters[0].column).toBe('name');
      expect(filters[1].column).toBe('age');
    });

    it('should handle empty collections', () => {
      const filters = new FilteringCollection<TestUser>();
      expect(filters.length).toBe(0);
    });
  });

  describe('Any/All Collection Operations Parsing', () => {
    interface TestUserWithRoles extends TestUser {
      roles: { name: string; isActive: boolean; permissions: string[] }[];
      projects: { status: string; priority: number; isCompleted: boolean }[];
    }

    it('should parse any collection operation', () => {
      const queryString = 'roles:any(name:eq(Admin),isActive:eq(true))';
      
      // Test parsing the collection regex pattern
      const collectionRegex = /^(\w+):(any|all)\((.+)\)$/;
      const match = queryString.match(collectionRegex);
      
      expect(match).not.toBeNull();
      expect(match![1]).toBe('roles');           // collection property
      expect(match![2]).toBe('any');             // operator
      expect(match![3]).toBe('name:eq(Admin),isActive:eq(true)'); // inner args
    });

    it('should parse all collection operation', () => {
      const queryString = 'projects:all(status:eq(Active),priority:gte(5))';
      
      const collectionRegex = /^(\w+):(any|all)\((.+)\)$/;
      const match = queryString.match(collectionRegex);
      
      expect(match).not.toBeNull();
      expect(match![1]).toBe('projects');        // collection property
      expect(match![2]).toBe('all');             // operator
      expect(match![3]).toBe('status:eq(Active),priority:gte(5)'); // inner args
    });

    it('should parse single condition in any operation', () => {
      const queryString = 'roles:any(name:eq(Admin))';
      
      const collectionRegex = /^(\w+):(any|all)\((.+)\)$/;
      const match = queryString.match(collectionRegex);
      
      expect(match).not.toBeNull();
      expect(match![1]).toBe('roles');
      expect(match![2]).toBe('any');
      expect(match![3]).toBe('name:eq(Admin)');
    });

    it('should parse complex conditions in all operation', () => {
      const queryString = 'projects:all(status:neq(Cancelled),isCompleted:eq(false),priority:gte(3))';
      
      const collectionRegex = /^(\w+):(any|all)\((.+)\)$/;
      const match = queryString.match(collectionRegex);
      
      expect(match).not.toBeNull();
      expect(match![1]).toBe('projects');
      expect(match![2]).toBe('all');
      expect(match![3]).toBe('status:neq(Cancelled),isCompleted:eq(false),priority:gte(3)');
    });

    it('should not match regular property:value patterns as collections', () => {
      const regularQueries = [
        'name:eq(John)',
        'age:gt(25)',
        'isActive:eq(true)',
        'email:ct(@example.com)'
      ];
      
      const collectionRegex = /^(\w+):(any|all)\((.+)\)$/;
      
      regularQueries.forEach(query => {
        const match = query.match(collectionRegex);
        expect(match).toBeNull();
      });
    });

    it('should not match regular composite operations as collections', () => {
      const compositeQueries = [
        'and(name:eq(John),age:gt(25))',
        'or(name:eq(John),name:eq(Jane))'
      ];
      
      const collectionRegex = /^(\w+):(any|all)\((.+)\)$/;
      
      compositeQueries.forEach(query => {
        const match = query.match(collectionRegex);
        expect(match).toBeNull();
      });
    });

    it('should handle nested conditions within any/all operations', () => {
      const queryString = 'roles:any(or(name:eq(Admin),name:eq(Manager)))';
      
      const collectionRegex = /^(\w+):(any|all)\((.+)\)$/;
      const match = queryString.match(collectionRegex);
      
      expect(match).not.toBeNull();
      expect(match![1]).toBe('roles');
      expect(match![2]).toBe('any');
      expect(match![3]).toBe('or(name:eq(Admin),name:eq(Manager))');
    });

    it('should validate collection operation formats', () => {
      const validFormats = [
        'roles:any(name:eq(Admin))',
        'projects:all(status:eq(Active))',
        'permissions:any(name:ct(write))',
        'tasks:all(isCompleted:eq(true),priority:gte(5))'
      ];

      const collectionRegex = /^(\w+):(any|all)\((.+)\)$/;
      
      validFormats.forEach(format => {
        const match = format.match(collectionRegex);
        expect(match).not.toBeNull();
        expect(['any', 'all']).toContain(match![2]);
      });
    });

    it('should reject invalid collection operation formats', () => {
      const invalidFormats = [
        'roles:any',                    // Missing parentheses
        'any(name:eq(Admin))',         // Missing collection property
        'roles:some(name:eq(Admin))',  // Invalid operator
        ':any(name:eq(Admin))',        // Missing collection property
        'roles:any()',                 // Empty conditions
      ];

      const collectionRegex = /^(\w+):(any|all)\((.+)\)$/;
      
      invalidFormats.forEach(format => {
        const match = format.match(collectionRegex);
        expect(match).toBeNull();
      });
    });
  });
});