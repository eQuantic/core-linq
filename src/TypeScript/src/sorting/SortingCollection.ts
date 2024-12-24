import { ISorting } from './base';

export class SortingCollection<TData extends object = any> extends Array<ISorting<TData>> {
  constructor(items: ISorting<TData>[] = []) {
    super();
    if (items.length > 0) {
      items.forEach((item) => this.push(item));
    }
  }
  public replace(filter: ISorting<TData>) {
    let idx = -1;
    for (let i = 0; i < this.length; i++) {
      if (this[i].column === filter.column) {
        idx = i;
        break;
      }
    }
    if (idx < 0) this.push(filter);
    else this[idx] = filter;
  }
}
