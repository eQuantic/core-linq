import { CompositeFiltering } from '../CompositeFiltering';
import { Filtering } from '../Filtering';

interface TestUser {
  id: number;
  name: string;
  email: string;
  age: number;
  isActive: boolean;
  roles: TestRole[];
  projects: TestProject[];
}

interface TestRole {
  name: string;
  isActive: boolean;
  permissions: string[];
}

interface TestProject {
  name: string;
  status: string;
  priority: number;
  isCompleted: boolean;
}

describe('CompositeFiltering', () => {
  describe('Constructor', () => {
    it('should create and filtering with multiple string values', () => {
      const composite = new CompositeFiltering<TestUser>('and', ['name:eq(John)', 'age:gt(25)']);
      
      expect(composite.compositeOperator).toBe('and');
      expect(composite.values).toHaveLength(2);
    });

    it('should create or filtering with string values', () => {
      const composite = new CompositeFiltering<TestUser>('or', ['name:eq(John)', 'age:gt(25)']);
      
      expect(composite.compositeOperator).toBe('or');
      expect(composite.values).toHaveLength(2);
    });

    it('should create any filtering with collection syntax', () => {
      const composite = new CompositeFiltering<TestUser>('any', 'roles' as any, ['name:eq(Admin)', 'isActive:eq(true)']);
      
      expect(composite.compositeOperator).toBe('any');
      expect(composite.values).toHaveLength(2);
    });

    it('should create all filtering with collection syntax', () => {
      const composite = new CompositeFiltering<TestUser>('all', 'projects' as any, ['status:eq(Active)', 'priority:gte(5)']);
      
      expect(composite.compositeOperator).toBe('all');
      expect(composite.values).toHaveLength(2);
    });

    it('should create filtering with explicit column', () => {
      const composite = new CompositeFiltering<TestUser>('any', 'roles' as any, ['name:eq(Admin)', 'isActive:eq(true)']);
      
      expect(composite.compositeOperator).toBe('any');
      expect(composite.column).toBe('roles');
      expect(composite.values).toHaveLength(2);
    });
  });

  describe('Static Factory Methods', () => {
    describe('and', () => {
      it('should create and composite filtering', () => {
        const composite = CompositeFiltering.and<TestUser>('name:eq(John)', 'age:gt(25)');
        
        expect(composite.compositeOperator).toBe('and');
        expect(composite.values).toHaveLength(2);
      });
    });

    describe('or', () => {
      it('should create or composite filtering', () => {
        const composite = CompositeFiltering.or<TestUser>('name:eq(John)', 'name:eq(Jane)');
        
        expect(composite.compositeOperator).toBe('or');
        expect(composite.values).toHaveLength(2);
      });
    });

    describe('any', () => {
      it('should create any composite filtering with explicit column', () => {
        const composite = new CompositeFiltering<TestUser>('any', 'roles' as any, ['name:eq(Admin)', 'isActive:eq(true)']);
        
        expect(composite.compositeOperator).toBe('any');
        expect(composite.values).toHaveLength(2);
      });
    });

    describe('all', () => {
      it('should create all composite filtering with explicit column', () => {
        const composite = new CompositeFiltering<TestUser>('all', 'projects' as any, ['status:eq(Active)', 'priority:gte(5)']);
        
        expect(composite.compositeOperator).toBe('all');
        expect(composite.values).toHaveLength(2);
      });
    });
  });

  describe('toString', () => {
    it('should format and operation correctly', () => {
      const composite = CompositeFiltering.and<TestUser>('name:eq(John)', 'age:gt(25)');
      
      expect(composite.toString()).toBe('and(name:eq(John),age:gt(25))');
    });

    it('should format or operation correctly', () => {
      const composite = CompositeFiltering.or<TestUser>('name:eq(John)', 'name:eq(Jane)');
      
      expect(composite.toString()).toBe('or(name:eq(John),name:eq(Jane))');
    });

    it('should format any collection operation correctly', () => {
      const composite = new CompositeFiltering<TestUser>('any', 'roles' as any, ['name:eq(Admin)', 'isActive:eq(true)']);
      
      expect(composite.toString()).toBe('roles:any(name:eq(Admin),isActive:eq(true))');
    });

    it('should format all collection operation correctly', () => {
      const composite = new CompositeFiltering<TestUser>('all', 'projects' as any, ['status:eq(Active)', 'priority:gte(5)']);
      
      expect(composite.toString()).toBe('projects:all(status:eq(Active),priority:gte(5))');
    });

    it('should format any operation with explicit column', () => {
      const composite = new CompositeFiltering<TestUser>('any', 'roles' as any, ['name:eq(Admin)', 'isActive:eq(true)']);
      
      expect(composite.toString()).toBe('roles:any(name:eq(Admin),isActive:eq(true))');
    });

    it('should format all operation with explicit column', () => {
      const composite = new CompositeFiltering<TestUser>('all', 'projects' as any, ['status:eq(Active)', 'priority:gte(5)']);
      
      expect(composite.toString()).toBe('projects:all(status:eq(Active),priority:gte(5))');
    });

    it('should handle complex any operation with multiple conditions', () => {
      const composite = new CompositeFiltering<TestUser>(
        'any', 
        'roles' as any, 
        ['name:eq(Admin)', 'isActive:eq(true)', 'permissions:ct(write)']
      );
      
      expect(composite.toString()).toBe('roles:any(name:eq(Admin),isActive:eq(true),permissions:ct(write))');
    });

    it('should handle complex all operation with multiple conditions', () => {
      const composite = new CompositeFiltering<TestUser>(
        'all', 
        'projects' as any, 
        ['status:eq(Active)', 'isCompleted:eq(false)', 'priority:gte(3)']
      );
      
      expect(composite.toString()).toBe('projects:all(status:eq(Active),isCompleted:eq(false),priority:gte(3))');
    });
  });

  describe('Business Scenarios', () => {
    it('should handle users with any admin or manager roles', () => {
      const composite = new CompositeFiltering<TestUser>('any', 'roles' as any, ['name:eq(Admin)', 'name:eq(Manager)']);
      
      expect(composite.toString()).toBe('roles:any(name:eq(Admin),name:eq(Manager))');
    });

    it('should handle users with all active and high-priority projects', () => {
      const composite = new CompositeFiltering<TestUser>('all', 'projects' as any, ['status:eq(Active)', 'priority:gte(8)']);
      
      expect(composite.toString()).toBe('projects:all(status:eq(Active),priority:gte(8))');
    });

    it('should handle users with any roles that have write permissions', () => {
      const composite = new CompositeFiltering<TestUser>('any', 'roles' as any, ['permissions:ct(write)', 'name:eq(Admin)']);
      
      expect(composite.toString()).toBe('roles:any(permissions:ct(write),name:eq(Admin))');
    });

    it('should handle users with all completed projects', () => {
      const composite = new CompositeFiltering<TestUser>('all', 'projects' as any, ['isCompleted:eq(true)', 'status:eq(Finished)']);
      
      expect(composite.toString()).toBe('projects:all(isCompleted:eq(true),status:eq(Finished))');
    });
  });

  describe('Type Safety', () => {
    it('should maintain type information for collection operations', () => {
      const anyComposite = new CompositeFiltering<TestUser>('any', 'roles' as any, ['name:eq(Admin)']);
      const allComposite = new CompositeFiltering<TestUser>('all', 'projects' as any, ['status:eq(Active)']);
      
      expect(anyComposite.compositeOperator).toBe('any');
      expect(allComposite.compositeOperator).toBe('all');
    });
  });

  describe('addValue method', () => {
    it('should add filtering values', () => {
      const composite = CompositeFiltering.and<TestUser>();
      const filter = new Filtering<TestUser>('name' as any, 'John', 'eq');
      
      composite.addValue(filter);
      
      expect(composite.values).toHaveLength(1);
      expect(composite.values[0]).toBe(filter);
    });
  });
});