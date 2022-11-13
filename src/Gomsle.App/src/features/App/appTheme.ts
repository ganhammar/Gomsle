import { DefaultTheme } from 'styled-components';

export const appTheme: DefaultTheme = {
  borderRadius: '4px',
  palette: {
    common: {
      black: '#222222',
      white: '#f9f9f9',
    },
    primary: {
      main: '#eee',
      contrastText: '#222222',
    },
    secondary: {
      main: '#f9f9f9',
      contrastText: '#222222',
    },
    divider: {
      main: '#bbb',
      contrastText: '#222222',
    },
  },
  typography: {
    fontFamily: 'Roboto, "Helvetica Neue", sans-serif',
    fontSize: '18px',
    lineHeight: '1.6',
    h1: '4rem',
    h2: '2.5rem',
    h3: '2rem',
  },
};
