export function splitArguments(s: string, separator: string = ','): string[] {
    const result: string[] = [];
    const str = s + separator;
    let root = 0;
    let curr = 0;
    for (let i = 0; i < str.length; i++) {
      if (str[i] === '(') root++;
      if (str[i] === ')') root--;
  
      if (str[i] === separator && root == 0) {
        result.push(str.substring(curr, i));
        curr = i + 1;
      }
    }
    return result;
  }
  
  export function randomInteger(min: number, max: number) {
    return Math.floor(Math.random() * (max - min + 1)) + min;
  }