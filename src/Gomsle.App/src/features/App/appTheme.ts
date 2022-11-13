import { DefaultTheme } from 'styled-components';

export const appTheme: DefaultTheme = {
  borderRadius: '4px',
  palette: {
    common: {
      black: '#222222',
      white: '#f9f9f9',
    },
    background: {
      main: '#eee',
      contrastText: '#222222',
    },
    primary: {
      main: '#5e50a1',
      contrastText: '#f9f9f9',
    },
    secondary: {
      main: '#eb5187',
      contrastText: '#222222',
    },
    divider: {
      main: '#bbb',
      contrastText: '#222222',
    },
    warning: {
      main: '#e52129',
      contrastText: '#f9f9f9',
    },
    success: {
      main: '#41a949',
      contrastText: '#f9f9f9',
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
