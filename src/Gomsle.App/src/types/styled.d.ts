// styled.d.ts
import 'styled-components';

interface IPalette {
  main: string;
  contrastText: string;
}

declare module 'styled-components' {
  export interface DefaultTheme {
    borderRadius: string;
    palette: {
      common: {
        black: string;
        white: string;
      };
      primary: IPalette;
      secondary: IPalette;
      divider: IPalette;
    };
    typography: {
      fontFamily: string;
      fontSize: string;
      lineHeight: string;
      h1: string;
      h2: string;
      h3: string;
    }
  }
}
