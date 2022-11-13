import { RecoilRoot } from 'recoil';
import { BrowserRouter } from 'react-router-dom';
import { ThemeProvider } from 'styled-components';
import { AppRoutes } from './AppRoutes';
import { appTheme } from './appTheme';
import { Layout } from './Layout';

export function App() {
  return (
    <RecoilRoot>
      <BrowserRouter>
        <ThemeProvider theme={appTheme}>
          <Layout>
            <AppRoutes />
          </Layout>
        </ThemeProvider>
      </BrowserRouter>
    </RecoilRoot>
  );
}
