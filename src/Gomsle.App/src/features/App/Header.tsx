import { Link } from 'react-router-dom';
import styled from 'styled-components';

const Wrapper = styled.header`
  text-align: center;
`;
const Title = styled.h1`
  font-family: Pacifico;
`;
const Nav = styled.nav`
  margin: 2rem 0;
  padding: 0.5rem 0;
  border-top: 1px solid ${({ theme }) => theme.palette.divider.main};
  border-bottom: 1px solid ${({ theme }) => theme.palette.divider.main};
  position: relative;
`;

export function Header() {
  return (
    <Wrapper>
      <Title>Gömsle</Title>
      <Nav>
        <Link to="/">Home</Link>
        <Link to="/register">Register</Link>
        <Link to="/login">Login</Link>
      </Nav>
    </Wrapper>
  );
}
