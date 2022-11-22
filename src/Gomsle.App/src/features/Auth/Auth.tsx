import { ReactElement, useCallback, useEffect } from 'react';
import { Route, Routes, useLocation, useNavigate } from 'react-router-dom';
import { useRecoilState } from 'recoil';
import { Loader } from '../../components/Loader';
import { userAtom } from '../User';
import userManager from './userManager';

const DEFAULT_VIEW = '/dashboard';

interface AuthProps {
  children: ReactElement;
}

function RenderIfLoggedIn({ children }: AuthProps) {
  const [user, setUser] = useRecoilState(userAtom);
  const { pathname } = useLocation();
  const navigate = useNavigate();

  const login = useCallback(() => {
    new Promise((resolve, reject) => {
      if (!user || user.expired === true) {
        reject(new Error('login_required'));
      } else {
        userManager.signinSilent().then(resolve).catch(reject);
      }
    })
      .then(() => {
        if (pathname === '/login') {
          navigate(DEFAULT_VIEW);
        } else {
          navigate(pathname);
        }
      })
      .catch(({ error, message }) => {
        if (error === 'login_required' || message === 'login_required') {
          userManager.signinRedirect({
            state: { from: pathname || DEFAULT_VIEW },
          });
        } else {
          throw new Error(message);
        }
      });
  }, [navigate, pathname, user]);

  const verifyLogin = useCallback(
    () =>
      userManager.getUser().then((result) => {
        if (result && result.expired === false) {
          setUser(result);
        } else {
          login();
        }
      }),
    [login, setUser]
  );

  useEffect(() => {
    verifyLogin();
  }, [verifyLogin]);

  if (user && children) {
    return children;
  }

  return <Loader />;
}

function LoginCallback() {
  const navigate = useNavigate();

  useEffect(() => {
    userManager.signinRedirectCallback().then(({ state }) => {
      navigate((state && (state as any).from) ?? DEFAULT_VIEW);
    });
  }, [navigate]);

  return <Loader />;
}

function LogoutRedirect() {
  const navigate = useNavigate();

  useEffect(() => {
    userManager.getUser().then((result) => {
      if (result && result.expired === false) {
        userManager.signoutRedirect();
      } else {
        navigate('/');
      }
    });
  }, [navigate]);

  return <Loader />;
}

function LogoutCallbackRedirect() {
  const navigate = useNavigate();

  useEffect(() => {
    userManager.signoutRedirectCallback().then(() => {
      navigate('/');
    });
  }, [navigate]);

  return <Loader />;
}

export function Auth({ children }: AuthProps) {
  return (
    <Routes>
      <Route path="/login/callback" element={<LoginCallback />} />
      <Route path="/logout/callback" element={<LogoutCallbackRedirect />} />
      <Route path="/logout" element={<LogoutRedirect />} />
      <Route
        path="*"
        element={<RenderIfLoggedIn>{children}</RenderIfLoggedIn>}
      />
    </Routes>
  );
}
