import { splitArguments } from '..';

test('split arguments: and(a:eq(1),b:eq(2)), c:eq(3), d:eq(4)', () => {
  expect(splitArguments('and(a:eq(1),b:eq(2)),c:eq(3),d:eq(4)')).toStrictEqual([
    'and(a:eq(1),b:eq(2))',
    'c:eq(3)',
    'd:eq(4)',
  ]);
});
