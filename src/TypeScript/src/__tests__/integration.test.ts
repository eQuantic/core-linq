import { Filtering } from '../filtering/Filtering';
import { FilteringCollection } from '../filtering/FilteringCollection';
import { Sorting } from '../sorting/Sorting';
import { SortingCollection } from '../sorting/SortingCollection';

interface User {
  id: number;
  name: string;
  email: string;
  age: number;
  isActive: boolean;
  department: string;
  salary: number;
  createdAt: Date;
}

describe('Integration Tests', () => {
  const sampleUsers: User[] = [
    { id: 1, name: 'John Doe', email: 'john@example.com', age: 25, isActive: true, department: 'IT', salary: 50000, createdAt: new Date('2023-01-15') },
    { id: 2, name: 'Jane Smith', email: 'jane@example.com', age: 30, isActive: false, department: 'HR', salary: 45000, createdAt: new Date('2023-02-20') },
    { id: 3, name: 'Bob Johnson', email: 'bob@example.com', age: 35, isActive: true, department: 'IT', salary: 60000, createdAt: new Date('2023-01-10') },
    { id: 4, name: 'Alice Brown', email: 'alice@example.com', age: 28, isActive: true, department: 'Finance', salary: 55000, createdAt: new Date('2023-03-05') }
  ];

  describe('Filtering + Sorting Integration', () => {
    it('should combine filtering and sorting for complex queries', () => {
      // Create filters
      const activeFilter = Filtering.eq<User, 'isActive'>('isActive', true);
      const ageFilter = Filtering.gte<User, 'age'>('age', 25);
      const departmentFilter = Filtering.neq<User, 'department'>('department', 'HR');
      
      const filters = new FilteringCollection<User>([activeFilter, ageFilter, departmentFilter]);
      
      // Create sorting
      const primarySort = Sorting.desc<User>('salary');
      const secondarySort = Sorting.asc<User>('name');
      
      const sorts = new SortingCollection<User>([primarySort, secondarySort]);
      
      // Simulate applying filters and sorts
      let filteredUsers = sampleUsers.filter(user => 
        user.isActive && 
        user.age >= 25 && 
        user.department !== 'HR'
      );
      
      filteredUsers = filteredUsers.sort((a, b) => {
        // Sort by salary DESC first
        if (a.salary !== b.salary) {
          return b.salary - a.salary;
        }
        // Then by name ASC
        return a.name.localeCompare(b.name);
      });
      
      expect(filteredUsers).toHaveLength(3);
      expect(filteredUsers[0].name).toBe('Bob Johnson'); // Highest salary
      expect(filteredUsers[1].name).toBe('Alice Brown'); // Second highest
      expect(filteredUsers[2].name).toBe('John Doe'); // Lowest salary
      
      // Verify filter structure
      expect(filters.length).toBe(3);
      expect(filters.toString()).toBe('isActive:eq(true),age:gte(25),department:neq(HR)');
      
      // Verify sort structure
      expect(sorts.length).toBe(2);
      expect(sorts.toString()).toBe('salary:desc,name:asc');
    });

    it('should handle complex nested property filtering and sorting', () => {
      interface UserWithProfile {
        id: number;
        profile: {
          name: {
            first: string;
            last: string;
          };
          contact: {
            email: string;
          };
        };
        metadata: {
          createdAt: Date;
          isActive: boolean;
        };
      }
      
      const users: UserWithProfile[] = [
        {
          id: 1,
          profile: { name: { first: 'John', last: 'Doe' }, contact: { email: 'john@example.com' } },
          metadata: { createdAt: new Date('2023-01-01'), isActive: true }
        },
        {
          id: 2,
          profile: { name: { first: 'Jane', last: 'Smith' }, contact: { email: 'jane@example.com' } },
          metadata: { createdAt: new Date('2023-02-01'), isActive: false }
        }
      ];
      
      // Filter by nested properties
      const nameFilter = Filtering.eq<UserWithProfile, 'profile.name.first'>('profile.name.first', 'John');
      const activeFilter = Filtering.eq<UserWithProfile, 'metadata.isActive'>('metadata.isActive', true);
      
      // Sort by nested properties
      const sort = Sorting.desc<UserWithProfile>('metadata.createdAt');
      
      expect(nameFilter.column).toBe('profile.name.first');
      expect(activeFilter.column).toBe('metadata.isActive');
      expect(sort.column).toBe('metadata.createdAt');
    });
  });

  describe('Manual Object Creation', () => {
    it('should create filters that would simulate query string parsing', () => {
      // Manually creating filters that would be the result of parsing query strings
      const filters = new FilteringCollection<User>([
        Filtering.ct<User, 'name'>('name', 'John'),
        Filtering.gte<User, 'age'>('age', 25),
        Filtering.eq<User, 'isActive'>('isActive', true),
        Filtering.neq<User, 'department'>('department', 'HR')
      ]);
      
      expect(filters.length).toBe(4);
      
      // Test that filters have correct structure
      const nameFilter = filters.find(f => f.column === 'name');
      const ageFilter = filters.find(f => f.column === 'age');
      const activeFilter = filters.find(f => f.column === 'isActive');
      const deptFilter = filters.find(f => f.column === 'department');
      
      expect(nameFilter?.operator).toBe('ct');
      expect(nameFilter?.value).toBe('John');
      
      expect(ageFilter?.operator).toBe('gte');
      expect(ageFilter?.value).toBe(25);
      
      expect(activeFilter?.operator).toBe('eq');
      expect(activeFilter?.value).toBe(true);
      
      expect(deptFilter?.operator).toBe('neq');
      expect(deptFilter?.value).toBe('HR');
    });

    it('should create sort objects manually', () => {
      const sorts = [
        Sorting.asc<User>('name'),
        Sorting.desc<User>('age'),
        Sorting.desc<User>('salary')
      ];
      
      expect(sorts.length).toBe(3);
      
      expect(sorts[0].column).toBe('name');
      expect(sorts[0].direction).toBe('asc');
      
      expect(sorts[1].column).toBe('age');
      expect(sorts[1].direction).toBe('desc');
      
      expect(sorts[2].column).toBe('salary');
      expect(sorts[2].direction).toBe('desc');
    });
  });

  describe('Manual Filter Building', () => {
    it('should build filters manually', () => {
      const filters = new FilteringCollection<User>([
        Filtering.eq<User, 'name'>('name', 'John'),
        Filtering.eq<User, 'isActive'>('isActive', true),
        Filtering.eq<User, 'age'>('age', 25)
      ]);
      
      expect(filters.length).toBe(3);
      
      // Verify each filter was created correctly
      const nameFilter = filters.find(f => f.column === 'name');
      const activeFilter = filters.find(f => f.column === 'isActive');
      const ageFilter = filters.find(f => f.column === 'age');
      
      expect(nameFilter?.value).toBe('John');
      expect(activeFilter?.value).toBe(true);
      expect(ageFilter?.value).toBe(25);
      
      // All should have 'eq' operator
      expect(nameFilter?.operator).toBe('eq');
      expect(activeFilter?.operator).toBe('eq');
      expect(ageFilter?.operator).toBe('eq');
    });

    it('should create complex nested property filters', () => {
      interface ComplexUser {
        user: {
          name: string;
          profile: {
            isActive: boolean;
          };
        };
        settings: {
          department: string;
        };
      }
      
      const filters = new FilteringCollection<ComplexUser>([
        Filtering.eq<ComplexUser, 'user.name'>('user.name', 'John'),
        Filtering.eq<ComplexUser, 'user.profile.isActive'>('user.profile.isActive', true),
        Filtering.eq<ComplexUser, 'settings.department'>('settings.department', 'IT')
      ]);
      
      expect(filters.length).toBe(3);
      
      expect(filters[0].column).toBe('user.name');
      expect(filters[1].column).toBe('user.profile.isActive');
      expect(filters[2].column).toBe('settings.department');
      
      expect(filters[0].value).toBe('John');
      expect(filters[1].value).toBe(true);
      expect(filters[2].value).toBe('IT');
    });
  });

  describe('Type Safety Integration', () => {
    it('should maintain type safety across filtering and sorting operations', () => {
      // This test mainly verifies compile-time type safety
      const filters = new FilteringCollection<User>([
        Filtering.eq<User, 'name'>('name', 'John'),
        Filtering.gte<User, 'age'>('age', 25),
        Filtering.ct<User, 'email'>('email', '@example.com')
      ]);
      
      const sorts = new SortingCollection<User>([
        Sorting.asc<User>('name'),
        Sorting.desc<User>('salary')
      ]);
      
      // Verify type information is preserved
      expect(filters.every(f => typeof f.column === 'string')).toBe(true);
      expect(sorts.every(s => typeof s.column === 'string')).toBe(true);
      expect(sorts.every(s => s.direction && ['asc', 'desc'].includes(s.direction))).toBe(true);
    });
  });

  describe('Performance and Edge Cases', () => {
    it('should handle empty collections gracefully', () => {
      const emptyFilters = new FilteringCollection<User>();
      const emptySorts = new SortingCollection<User>();
      
      expect(emptyFilters.length).toBe(0);
      expect(emptySorts.length).toBe(0);
      expect(emptyFilters.toString()).toBe('');
      expect(emptySorts.toString()).toBe('');
    });

    it('should handle large collections efficiently', () => {
      const manyFilters = Array.from({ length: 100 }, (_, i) => 
        Filtering.eq<User, 'name'>('name', `User${i}`)
      );
      
      const filterCollection = new FilteringCollection<User>(manyFilters);
      
      expect(filterCollection.length).toBe(100);
      
      // Test performance of operations
      const startTime = Date.now();
      const filtered = filterCollection.filter(f => String(f.value).includes('5'));
      const endTime = Date.now();
      
      expect(endTime - startTime).toBeLessThan(100); // Should be fast
      expect(filtered.length).toBeGreaterThan(0);
    });

    it('should handle special characters in values', () => {
      const specialFilters = [
        Filtering.eq<User, 'name'>('name', 'John "Johnny" O\'Connor'),
        Filtering.ct<User, 'email'>('email', '@company.co.uk'),
        Filtering.sw<User, 'department'>('department', 'R&D/Innovation')
      ];
      
      const collection = new FilteringCollection<User>(specialFilters);
      
      expect(collection.length).toBe(3);
      expect(collection[0].value).toBe('John "Johnny" O\'Connor');
      expect(collection[1].value).toBe('@company.co.uk');
      expect(collection[2].value).toBe('R&D/Innovation');
    });
  });
});